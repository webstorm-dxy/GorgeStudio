using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GorgeStudio.AppCore.DependencyInjection;
using GorgeStudio.AppCore.Models.Results;
using GorgeStudio.AppCore.Services;
using GorgeStudio.AppCore.Sessions;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;
using GorgeStudio.Models.Timeline;
using GorgeStudio.Services;
using GorgeStudio.Services.ChartService;

namespace GorgeStudio.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();

        if (command == "shell")
            return await RunInteractiveShellAsync();

        return await RunOneShotAsync(command, args.Skip(1).ToArray());
    }

    private static void PrintUsage()
    {
        Console.WriteLine("GorgeStudio.Cli - Gorge Studio 命令行工具");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  GorgeStudio.Cli.exe shell                       交互式 Shell 模式");
        Console.WriteLine("  GorgeStudio.Cli.exe compile --input <path>       编译谱面包");
        Console.WriteLine("  GorgeStudio.Cli.exe save --input <path> [--output <path>]  保存谱面");
        Console.WriteLine("  GorgeStudio.Cli.exe launch --input <path>         启动 Godot 并加载谱面");
        Console.WriteLine("  GorgeStudio.Cli.exe inspect classes --input <path>  查看类结构");
        Console.WriteLine("  GorgeStudio.Cli.exe inspect elements --input <path>  查看元素列表");
        Console.WriteLine("  GorgeStudio.Cli.exe forms list                    列出可用 Forms");
    }

    #region One-shot commands

    static async Task<int> RunOneShotAsync(string command, string[] args)
    {
        var services = new ServiceCollection();
        services.AddGorgeStudioAppCore();
        var sp = services.BuildServiceProvider();

        return command switch
        {
            "compile" => await CompileCommand(sp, ParseArgs(args)),
            "save" => await SaveCommand(sp, ParseArgs(args)),
            "launch" => await LaunchCommand(sp, ParseArgs(args)),
            "inspect" => await InspectCommand(sp, ParseArgs(args)),
            "forms" => await FormsCommand(sp, ParseArgs(args)),
            _ => HandleUnknownCommand(command)
        };
    }

    static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var dict = new Dictionary<string, string?>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i][2..];
                var value = (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ? args[++i] : null;
                dict[key] = value;
            }
            else
            {
                dict[args[i]] = null;
            }
        }
        return dict;
    }

    static async Task<int> CompileCommand(ServiceProvider sp, Dictionary<string, string?> args)
    {
        var input = args.GetValueOrDefault("input");
        if (input == null)
        {
            Console.Error.WriteLine("错误：需要 --input <path>");
            return 1;
        }

        var fileService = sp.GetRequiredService<IFileService>();
        Console.WriteLine($"编译: {input}");
        var result = await fileService.LoadAndCompileZipAsync(input);

        if (result.Success && result.Project != null)
        {
            Console.WriteLine($"成功: {result.Project.Classes.Count} 个类, {result.Project.Enums.Count} 个枚举, 耗时 {result.CompileTime.TotalMilliseconds:F0}ms");
            return 0;
        }

        Console.Error.WriteLine($"编译失败: {result.ErrorMessage}");
        return 1;
    }

    static async Task<int> SaveCommand(ServiceProvider sp, Dictionary<string, string?> args)
    {
        var input = args.GetValueOrDefault("input");
        var output = args.GetValueOrDefault("output") ?? input;

        if (input == null)
        {
            Console.Error.WriteLine("错误：需要 --input <path>");
            return 1;
        }

        var workspace = sp.GetRequiredService<IChartWorkspaceService>();
        workspace.StatusChanged += msg => Console.WriteLine($"  {msg}");

        Console.WriteLine($"加载: {input}");
        var loadResult = await workspace.LoadFromZipAsync(input);

        if (!loadResult.Success || loadResult.Score == null)
        {
            Console.Error.WriteLine($"加载失败: {loadResult.ErrorMessage}");
            return 1;
        }

        var session = new ChartSession
        {
            CurrentScore = loadResult.Score,
            CurrentProject = loadResult.Project,
            Settings = loadResult.Project != null ? new ProjectSettings() : new ProjectSettings()
        };
        if (loadResult.LoadedForms != null)
            session.LoadedForms.AddRange(loadResult.LoadedForms);

        Console.WriteLine($"保存: {output}");
        var saveResult = await workspace.SaveAsync(session, output);

        if (saveResult.Success)
        {
            Console.WriteLine($"保存成功: {saveResult.FilePath}");
            return 0;
        }

        Console.Error.WriteLine($"保存失败: {saveResult.ErrorMessage}");
        return 1;
    }

    static async Task<int> LaunchCommand(ServiceProvider sp, Dictionary<string, string?> args)
    {
        var input = args.GetValueOrDefault("input");
        if (input == null)
        {
            Console.Error.WriteLine("错误：需要 --input <path>");
            return 1;
        }

        var workspace = sp.GetRequiredService<IChartWorkspaceService>();
        var launchWorkflow = sp.GetRequiredService<IGodotLaunchWorkflow>();
        workspace.StatusChanged += msg => Console.WriteLine($"  {msg}");
        launchWorkflow.StatusChanged += msg => Console.WriteLine($"  {msg}");

        Console.WriteLine($"加载: {input}");
        var loadResult = await workspace.LoadFromZipAsync(input);

        if (!loadResult.Success || loadResult.Score == null)
        {
            Console.Error.WriteLine($"加载失败: {loadResult.ErrorMessage}");
            return 1;
        }

        var session = new ChartSession
        {
            CurrentFilePath = input,
            CurrentScore = loadResult.Score,
            CurrentProject = loadResult.Project,
            Settings = new ProjectSettings()
        };
        if (loadResult.LoadedForms != null)
            session.LoadedForms.AddRange(loadResult.LoadedForms);

        // Save first, then launch
        Console.WriteLine("正在保存并启动 Godot...");
        var saveResult = await workspace.SaveAsync(session);
        if (!saveResult.Success || saveResult.FilePath == null)
        {
            Console.Error.WriteLine($"保存失败: {saveResult.ErrorMessage}");
            return 1;
        }

        // Launch Godot and load via UDP
        var launchResult = await launchWorkflow.SaveLaunchAndLoadAsync(session);
        if (launchResult.Success)
        {
            Console.WriteLine($"Godot 已启动，谱面已加载，时长 {launchResult.DurationSeconds:F1}s");
            return 0;
        }

        Console.Error.WriteLine($"启动失败: {launchResult.ErrorMessage}");
        return 1;
    }

    static async Task<int> InspectCommand(ServiceProvider sp, Dictionary<string, string?> args)
    {
        var subCommand = args.Keys.FirstOrDefault(k => k is "classes" or "elements");
        if (subCommand == null)
        {
            Console.Error.WriteLine("用法: inspect classes --input <path>  或  inspect elements --input <path>");
            return 1;
        }

        var input = args.GetValueOrDefault("input");
        if (input == null)
        {
            Console.Error.WriteLine("错误：需要 --input <path>");
            return 1;
        }

        var fileService = sp.GetRequiredService<IFileService>();
        Console.WriteLine($"加载: {input}");
        var result = await fileService.LoadAndCompileZipAsync(input);

        if (!result.Success || result.Project == null)
        {
            Console.Error.WriteLine($"编译失败: {result.ErrorMessage}");
            return 1;
        }

        if (subCommand == "classes")
        {
            Console.WriteLine($"类 ({result.Project.Classes.Count}):");
            foreach (var cls in result.Project.Classes)
                Console.WriteLine($"  {cls.FullName} (Chart: {cls.IsChartCode}, Native: {cls.IsNative})");

            Console.WriteLine($"枚举 ({result.Project.Enums.Count}):");
            foreach (var e in result.Project.Enums)
                Console.WriteLine($"  {e.FullName}: [{string.Join(", ", e.Values)}]");
        }
        else if (subCommand == "elements")
        {
            var chartService = sp.GetRequiredService<GorgeStudio.Services.ChartService.IChartService>();
            var score = await chartService.BuildChartDocumentAsync(result);

            Console.WriteLine($"谱表 ({score.Stave.Count}):");
            foreach (var staff in score.Stave)
            {
                Console.WriteLine($"  [{staff.ClassName}] {staff.DisplayName} ({staff.Periods.Count()} periods)");
                foreach (var period in staff.Periods)
                {
                    Console.WriteLine($"    {period.MethodName} @ {period.TimeOffset:F2}s");
                    if (period is ElementPeriod ep)
                    {
                        foreach (var elem in ep.Elements)
                            Console.WriteLine($"      - {elem.InjectedClassDeclaration.Name}");
                    }
                }
            }
        }

        return 0;
    }

    static async Task<int> FormsCommand(ServiceProvider sp, Dictionary<string, string?> args)
    {
        var formsService = sp.GetRequiredService<IFormsCatalogService>();
        var forms = await formsService.DiscoverFormsAsync();

        Console.WriteLine($"可用 Forms ({forms.Count}):");
        foreach (var form in forms)
            Console.WriteLine($"  {form.Name} v{form.Version} ({form.DirectoryName})");

        return 0;
    }

    static int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"未知命令: {command}");
        PrintUsage();
        return 1;
    }

    #endregion

    #region Interactive Shell

    static async Task<int> RunInteractiveShellAsync()
    {
        var services = new ServiceCollection();
        services.AddGorgeStudioAppCore();
        var sp = services.BuildServiceProvider();

        var workspace = sp.GetRequiredService<IChartWorkspaceService>();
        var formsService = sp.GetRequiredService<IFormsCatalogService>();
        var launchWorkflow = sp.GetRequiredService<IGodotLaunchWorkflow>();
        var timelineWorkflow = sp.GetRequiredService<ITimelineEditingWorkflow>();
        var inspectionService = sp.GetRequiredService<IPropertyInspectionService>();

        workspace.StatusChanged += msg => Console.WriteLine($"  [{msg}]");

        var session = new ChartSession();
        Console.WriteLine("GorgeStudio CLI Shell - 输入 'help' 查看命令, 'exit' 退出");
        Console.WriteLine();

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line == null) break;

            var parts = ParseLine(line);
            if (parts.Count == 0) continue;

            var cmd = parts[0].ToLowerInvariant();
            var cmdArgs = ParseArgs(parts.Skip(1).ToArray());

            try
            {
                switch (cmd)
                {
                    case "exit":
                    case "quit":
                        return 0;

                    case "help":
                        PrintShellHelp();
                        break;

                    case "load":
                        await ShellLoad(workspace, session, cmdArgs);
                        break;

                    case "compile":
                        await ShellCompile(workspace, session, cmdArgs);
                        break;

                    case "save":
                        await ShellSave(workspace, session, cmdArgs);
                        break;

                    case "save-as":
                        await ShellSaveAs(workspace, session, cmdArgs);
                        break;

                    case "launch":
                        await ShellLaunch(workspace, launchWorkflow, session);
                        break;

                    case "forms":
                        await ShellForms(formsService, cmdArgs);
                        break;

                    case "tracks":
                        ShellTracks(session);
                        break;

                    case "track":
                        await ShellTrackEdit(timelineWorkflow, session, cmdArgs);
                        break;

                    case "period":
                        ShellPeriodEdit(timelineWorkflow, session, cmdArgs);
                        break;

                    case "inspect":
                        ShellInspect(inspectionService, session, cmdArgs);
                        break;

                    case "settings":
                        ShellSettings(session, cmdArgs);
                        break;

                    case "snap":
                        ShellSnap(cmdArgs);
                        break;

                    default:
                        Console.WriteLine($"未知命令: {cmd}. 输入 'help' 查看帮助.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"错误: {ex.Message}");
            }
        }

        return 0;
    }

    static List<string> ParseLine(string line)
    {
        var parts = new List<string>();
        var current = "";
        bool inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0) { parts.Add(current); current = ""; }
            }
            else
            {
                current += c;
            }
        }
        if (current.Length > 0) parts.Add(current);
        return parts;
    }

    static void PrintShellHelp()
    {
        Console.WriteLine("命令列表:");
        Console.WriteLine("  load <path>                           加载谱面包");
        Console.WriteLine("  compile [--input <path>]              编译当前或指定谱面");
        Console.WriteLine("  save                                   保存当前谱面");
        Console.WriteLine("  save-as <path>                         另存为");
        Console.WriteLine("  launch                                 保存并启动 Godot 加载谱面");
        Console.WriteLine("  forms list                             列出可用 Forms");
        Console.WriteLine("  forms load --names <n1,n2>             加载指定 Forms");
        Console.WriteLine("  tracks list                            列出谱表");
        Console.WriteLine("  track add [--type ElementStaff|AudioStaff] [--form <name>]  添加谱表");
        Console.WriteLine("  track delete --index <n>               删除谱表");
        Console.WriteLine("  track copy --index <n>                 复制谱表");
        Console.WriteLine("  track rename --index <n> --name <text> 重命名谱表");
        Console.WriteLine("  period add --track <n> --time <s>      添加乐段");
        Console.WriteLine("  period delete --track <n> --index <n>  删除乐段");
        Console.WriteLine("  period copy --track <n> --index <n>    复制乐段");
        Console.WriteLine("  period set-time --track <n> --index <n> --time <s>    设置乐段时间");
        Console.WriteLine("  period set-min-length --track <n> --index <n> --length <s>  设置乐段最小长度");
        Console.WriteLine("  inspect classes                        查看类结构");
        Console.WriteLine("  inspect elements                       查看元素列表");
        Console.WriteLine("  inspect object --path <selector>       查看对象属性");
        Console.WriteLine("  settings get                           查看项目设置");
        Console.WriteLine("  settings set --key <key> --value <v>   修改项目设置");
        Console.WriteLine("  snap set --mode off|bar|beat|sub       设置吸附模式");
        Console.WriteLine("  exit                                   退出");
    }

    static async Task ShellLoad(IChartWorkspaceService workspace, ChartSession session, Dictionary<string, string?> args)
    {
        var path = args.GetValueOrDefault("input") ?? args.Keys.FirstOrDefault();
        if (path == null) { Console.Error.WriteLine("用法: load <path>"); return; }

        var result = await workspace.LoadFromZipAsync(path);
        if (result.Success)
        {
            session.CurrentFilePath = path;
            session.CurrentProject = result.Project;
            session.CurrentScore = result.Score;
            if (result.LoadedForms != null)
            {
                session.LoadedForms.Clear();
                session.LoadedForms.AddRange(result.LoadedForms);
            }
            Console.WriteLine($"加载成功: {result.Project?.Classes.Count ?? 0} 个类, 耗时 {result.CompileTime.TotalMilliseconds:F0}ms");
        }
        else
        {
            Console.Error.WriteLine($"加载失败: {result.ErrorMessage}");
        }
    }

    static async Task ShellCompile(IChartWorkspaceService workspace, ChartSession session, Dictionary<string, string?> args)
    {
        var input = args.GetValueOrDefault("input");
        if (input != null)
        {
            var result = await workspace.LoadFromZipAsync(input);
            if (result.Success) Console.WriteLine($"编译成功: {result.Project?.Classes.Count ?? 0} 个类");
            else Console.Error.WriteLine($"编译失败: {result.ErrorMessage}");
        }
        else if (session.CurrentProject != null)
        {
            Console.WriteLine($"当前谱面: {session.CurrentProject.Classes.Count} 个类已加载");
        }
        else
        {
            Console.Error.WriteLine("无已加载的谱面。使用 'load <path>' 加载。");
        }
    }

    static async Task ShellSave(IChartWorkspaceService workspace, ChartSession session, Dictionary<string, string?> args)
    {
        var output = args.GetValueOrDefault("output");
        if (output != null) session.CurrentFilePath = output;

        if (session.CurrentFilePath == null)
        {
            Console.Error.WriteLine("未指定保存路径。使用 'save-as <path>' 或先 'load <path>'.");
            return;
        }

        var result = await workspace.SaveAsync(session);
        if (result.Success) Console.WriteLine($"保存成功: {result.FilePath}");
        else Console.Error.WriteLine($"保存失败: {result.ErrorMessage}");
    }

    static async Task ShellSaveAs(IChartWorkspaceService workspace, ChartSession session, Dictionary<string, string?> args)
    {
        var path = args.Keys.FirstOrDefault();
        if (path == null) { Console.Error.WriteLine("用法: save-as <path>"); return; }
        session.CurrentFilePath = path;
        var result = await workspace.SaveAsync(session);
        if (result.Success) Console.WriteLine($"保存成功: {result.FilePath}");
        else Console.Error.WriteLine($"保存失败: {result.ErrorMessage}");
    }

    static async Task ShellLaunch(IChartWorkspaceService workspace, IGodotLaunchWorkflow launchWorkflow, ChartSession session)
    {
        if (session.CurrentScore == null) { Console.Error.WriteLine("无已加载的谱面。"); return; }

        launchWorkflow.StatusChanged += msg => Console.WriteLine($"  {msg}");
        var result = await launchWorkflow.SaveLaunchAndLoadAsync(session);
        if (result.Success) Console.WriteLine($"启动成功，时长 {result.DurationSeconds:F1}s");
        else Console.Error.WriteLine($"启动失败: {result.ErrorMessage}");
    }

    static async Task ShellForms(IFormsCatalogService formsService, Dictionary<string, string?> args)
    {
        var subCmd = args.Keys.FirstOrDefault() ?? "list";
        if (subCmd == "list")
        {
            var forms = await formsService.DiscoverFormsAsync();
            Console.WriteLine($"可用 Forms ({forms.Count}):");
            foreach (var f in forms) Console.WriteLine($"  {f.Name} v{f.Version} ({f.DirectoryName})");
        }
        else
        {
            Console.Error.WriteLine($"未知子命令: {subCmd}. 可用: list");
        }
    }

    static void ShellTracks(ChartSession session)
    {
        if (session.CurrentScore == null) { Console.Error.WriteLine("无已加载的谱面。"); return; }

        Console.WriteLine($"谱表 ({session.CurrentScore.Stave.Count}):");
        for (int i = 0; i < session.CurrentScore.Stave.Count; i++)
        {
            var s = session.CurrentScore.Stave[i];
            Console.WriteLine($"  [{i}] {s.ClassName} \"{s.DisplayName}\" ({s.Periods.Count()} periods)");
        }
    }

    static async Task ShellTrackEdit(ITimelineEditingWorkflow timeline, ChartSession session, Dictionary<string, string?> args)
    {
        var score = session.CurrentScore;
        if (score == null) { Console.Error.WriteLine("无已加载的谱面。"); return; }

        var subCmd = args.Keys.FirstOrDefault() ?? "list";
        switch (subCmd)
        {
            case "add":
                var type = args.GetValueOrDefault("--type") ?? "ElementStaff";
                var form = args.GetValueOrDefault("--form");
                var prefix = type == "AudioStaff" ? "AudioStaff" : "ElementStaff";
                var className = timeline.GetNextClassName(score, prefix);
                var displayName = $"{prefix}谱表";
                var staff = timeline.CreateStaff(className, true, displayName, form);
                timeline.AddStaff(score, staff);
                Console.WriteLine($"添加谱表: {className}");
                break;

            case "delete":
                if (int.TryParse(args.GetValueOrDefault("--index"), out var delIdx))
                {
                    timeline.RemoveStaff(score, delIdx);
                    Console.WriteLine($"删除谱表: [{delIdx}]");
                }
                else Console.Error.WriteLine("用法: track delete --index <n>");
                break;

            case "copy":
                if (int.TryParse(args.GetValueOrDefault("--index"), out var copyIdx))
                {
                    timeline.CopyStaff(score, copyIdx);
                    Console.WriteLine($"复制谱表: [{copyIdx}] -> [{copyIdx + 1}]");
                }
                else Console.Error.WriteLine("用法: track copy --index <n>");
                break;

            case "rename":
                if (int.TryParse(args.GetValueOrDefault("--index"), out var renIdx) &&
                    args.TryGetValue("--name", out var newName) && newName != null)
                {
                    timeline.RenameStaffDisplayName(score, renIdx, newName);
                    Console.WriteLine($"重命名谱表 [{renIdx}]: {newName}");
                }
                else Console.Error.WriteLine("用法: track rename --index <n> --name <text>");
                break;

            default:
                ShellTracks(session);
                break;
        }
    }

    static void ShellPeriodEdit(ITimelineEditingWorkflow timeline, ChartSession session, Dictionary<string, string?> args)
    {
        var score = session.CurrentScore;
        if (score == null) { Console.Error.WriteLine("无已加载的谱面。"); return; }

        var subCmd = args.Keys.FirstOrDefault() ?? "list";
        if (!int.TryParse(args.GetValueOrDefault("--track"), out var trackIdx))
        {
            if (subCmd != "list") { Console.Error.WriteLine("需要 --track <n>"); return; }
        }

        if (trackIdx < 0 || trackIdx >= score.Stave.Count)
        {
            Console.Error.WriteLine($"无效的 track 索引: {trackIdx}");
            return;
        }

        var staff = score.Stave[trackIdx];

        switch (subCmd)
        {
            case "add":
                if (float.TryParse(args.GetValueOrDefault("--time"), out var time))
                {
                    var period = timeline.CreatePeriod(staff, score, time);
                    timeline.InsertPeriod(staff, period);
                    Console.WriteLine($"添加乐段: {period.MethodName} @ {period.TimeOffset:F2}s");
                }
                else Console.Error.WriteLine("用法: period add --track <n> --time <s>");
                break;

            case "delete":
                if (int.TryParse(args.GetValueOrDefault("--index"), out var delIdx) &&
                    delIdx >= 0 && delIdx < staff.Periods.Count())
                {
                    var period = staff.Periods.ElementAt(delIdx);
                    timeline.RemovePeriod(staff, period);
                    Console.WriteLine($"删除乐段 [{delIdx}]");
                }
                else Console.Error.WriteLine("用法: period delete --track <n> --index <n>");
                break;

            case "copy":
                if (int.TryParse(args.GetValueOrDefault("--index"), out var copyIdx) &&
                    copyIdx >= 0 && copyIdx < staff.Periods.Count())
                {
                    var period = staff.Periods.ElementAt(copyIdx);
                    timeline.DuplicatePeriod(staff, period);
                    Console.WriteLine($"复制乐段 [{copyIdx}]");
                }
                else Console.Error.WriteLine("用法: period copy --track <n> --index <n>");
                break;

            case "set-time":
                if (int.TryParse(args.GetValueOrDefault("--index"), out var stIdx) &&
                    float.TryParse(args.GetValueOrDefault("--time"), out var newTime) &&
                    stIdx >= 0 && stIdx < staff.Periods.Count())
                {
                    timeline.UpdatePeriodTimeOffset(staff.Periods.ElementAt(stIdx), newTime);
                    Console.WriteLine($"乐段 [{stIdx}] 时间: {newTime:F2}s");
                }
                else Console.Error.WriteLine("用法: period set-time --track <n> --index <n> --time <s>");
                break;

            case "set-min-length":
                if (int.TryParse(args.GetValueOrDefault("--index"), out var mlIdx) &&
                    float.TryParse(args.GetValueOrDefault("--length"), out var newLen) &&
                    mlIdx >= 0 && mlIdx < staff.Periods.Count())
                {
                    timeline.UpdatePeriodMinLength(staff.Periods.ElementAt(mlIdx), newLen);
                    Console.WriteLine($"乐段 [{mlIdx}] 最小长度: {newLen:F2}s");
                }
                else Console.Error.WriteLine("用法: period set-min-length --track <n> --index <n> --length <s>");
                break;

            default:
                // List periods for track
                Console.WriteLine($"Track [{trackIdx}] {staff.ClassName} ({staff.Periods.Count()} periods):");
                for (int i = 0; i < staff.Periods.Count(); i++)
                {
                    var p = staff.Periods.ElementAt(i);
                    var elemCount = p is ElementPeriod ep ? ep.Elements.Count : 0;
                    Console.WriteLine($"  [{i}] {p.MethodName} @ {p.TimeOffset:F2}s, minLen={p.MinLength:F2}s, elements={elemCount}");
                }
                break;
        }
    }

    static void ShellInspect(IPropertyInspectionService inspectionService, ChartSession session, Dictionary<string, string?> args)
    {
        var subCmd = args.Keys.FirstOrDefault();
        if (subCmd == null) { Console.Error.WriteLine("用法: inspect classes|elements|object"); return; }

        switch (subCmd)
        {
            case "classes":
                var project = session.CurrentProject;
                if (project == null) { Console.Error.WriteLine("无已加载的项目。"); return; }
                Console.WriteLine($"类 ({project.Classes.Count}):");
                foreach (var cls in project.Classes)
                    Console.WriteLine($"  {cls.FullName} (Chart: {cls.IsChartCode}, Native: {cls.IsNative})");
                Console.WriteLine($"枚举 ({project.Enums.Count}):");
                foreach (var e in project.Enums)
                    Console.WriteLine($"  {e.FullName}: [{string.Join(", ", e.Values)}]");
                break;

            case "elements":
                var score = session.CurrentScore;
                if (score == null) { Console.Error.WriteLine("无已加载的谱面。"); return; }
                foreach (var staff in score.Stave)
                {
                    if (staff is ElementStaff es)
                    {
                        foreach (var period in es.Periods)
                        {
                            foreach (var elem in period.Elements)
                                Console.WriteLine($"  {es.ClassName}.{period.MethodName}.{elem.InjectedClassDeclaration.Name}");
                        }
                    }
                }
                break;

            case "object":
                Console.Error.WriteLine("此命令需要在交互模式下先选择对象。");
                break;

            default:
                Console.Error.WriteLine($"未知: {subCmd}");
                break;
        }
    }

    static void ShellSettings(ChartSession session, Dictionary<string, string?> args)
    {
        var subCmd = args.Keys.FirstOrDefault() ?? "get";
        switch (subCmd)
        {
            case "get":
                var s = session.Settings;
                Console.WriteLine($"BPM: {s.Bpm}, Offset: {s.Offset}, BeatsPerBar: {s.BeatsPerBar}, SubdivisionsPerBeat: {s.SubdivisionsPerBeat}");
                Console.WriteLine($"Forms: [{string.Join(", ", s.Forms)}]");
                break;

            case "set":
                if (args.TryGetValue("--key", out var key) && args.TryGetValue("--value", out var value))
                {
                    switch (key?.ToLowerInvariant())
                    {
                        case "bpm": if (int.TryParse(value, out var bpm)) session.Settings.Bpm = bpm; break;
                        case "offset": if (int.TryParse(value, out var off)) session.Settings.Offset = off; break;
                        case "beatsperbar": if (int.TryParse(value, out var bpb)) session.Settings.BeatsPerBar = bpb; break;
                        case "subdivisionsperbeat": if (int.TryParse(value, out var spb)) session.Settings.SubdivisionsPerBeat = spb; break;
                        default: Console.Error.WriteLine($"未知设置: {key}"); break;
                    }
                    Console.WriteLine($"设置 {key} = {value}");
                }
                else Console.Error.WriteLine("用法: settings set --key <key> --value <value>");
                break;

            default:
                Console.Error.WriteLine($"未知: {subCmd}");
                break;
        }
    }

    static void ShellSnap(Dictionary<string, string?> args)
    {
        var subCmd = args.Keys.FirstOrDefault() ?? "get";
        switch (subCmd)
        {
            case "set":
                var mode = args.GetValueOrDefault("--mode");
                Console.WriteLine($"Snap 模式: {mode ?? "(未指定)"}");
                break;
            default:
                Console.Error.WriteLine("用法: snap set --mode off|bar|beat|sub");
                break;
        }
    }

    #endregion
}
