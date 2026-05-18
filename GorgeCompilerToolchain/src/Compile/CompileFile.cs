using System.Collections.Generic;
using Gorge.GorgeLanguage.VirtualMachine;
using Gorge.GorgeCompiler;
using System;
using System.IO;
using Gorge.GorgeCompiler.CompileContext;
using Gorge.GorgeLanguage.Objective;

namespace GorgeCompilerToolchain.Compile;

public class CompileFile
{
    /// <summary>
    /// 存放编译过程中生成的中间代码
    /// </summary>
    public List<IntermediateCode> IntermediateCodes { get; } = new List<IntermediateCode>();
    public ClassImplementationContext context;
    
    /// <summary>
    /// 编译指定路径的 Gorge 源代码文件
    /// </summary>
    /// <param name="sourceFilePath">源代码文件路径</param>
    /// <returns>编译是否成功</returns>
    public bool CompileSourceFile(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"源文件未找到: {sourceFilePath}");
        }
        
        var code = File.ReadAllText(sourceFilePath);
        var sourceFile = new SourceCodeFile(sourceFilePath, code, false);
        
        try
        {
            var result = Compiler.Compile(new[] { sourceFile });
            context = result;
            // 编译成功完成
            return true;
        }
        catch (Exception ex)
        {
            // 编译过程中出现错误
            Console.WriteLine($"编译错误: {ex.Message}");
            return false;
        }
    }

    public ClassImplementationContext CompileString(string code)
    {
        
        var sourceFile = new SourceCodeFile("fromInput", code, false);
        
        try
        {
            var result = Compiler.Compile(new[] { sourceFile });
            // 编译成功完成
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"编译错误: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 编译多个 Gorge 源代码文件
    /// </summary>
    /// <param name="sourceFilePaths">源代码文件路径数组</param>
    /// <returns>编译是否成功</returns>
    public bool CompileSourceFiles(string[] sourceFilePaths)
    {
        var sourceFiles = new List<SourceCodeFile>();
        
        foreach (var filePath in sourceFilePaths)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"源文件未找到: {filePath}");
            }
            
            var code = File.ReadAllText(filePath);
            sourceFiles.Add(new SourceCodeFile(filePath, code, false));
        }
        
        try
        {
            var result = Compiler.Compile(sourceFiles);
            
            // 编译成功完成
            return true;
        }
        catch (Exception ex)
        {
            // 编译过程中出现错误
            Console.WriteLine($"编译错误: {ex.Message}");
            return false;
        }
    }
   
}