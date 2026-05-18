# Project Instructions

This file provides context for AI assistants working on this project.

## Project Type

Avalonia 11.3.10 desktop application (Windows), .NET 9.0.  
Embedded Godot engine runtime via Win32 window hosting.

## Build / Run

```bash
# 在标准 Windows 桌面环境（Visual Studio / Rider）中：
dotnet build GorgeStudio.sln

# 在精简 shell 环境（如 DeepSeek TUI）中，环境变量不全会导致 NuGet 失败，
# 使用项目根目录下的脚本：
build_env.bat
```

构建输出：`GorgeStudio/bin/Debug/net9.0/GorgeStudio.dll`。  
`GodotApplication/` 目录通过 MSBuild Target 自动复制到输出目录。

## Architecture

```
GorgeStudio/
├── App.axaml.cs               # 组合根：DI 容器配置、依赖组装
├── Services/
│   ├── IEmbedService.cs        # 嵌入服务抽象
│   └── EmbedService.cs         # 封装 Win32 窗口嵌入 + 路径解析
├── Interop/
│   └── Win32WindowEmbedder.cs  # LibraryImport P/Invoke、进程管理、SetParent
├── ViewModels/
│   ├── ViewModelBase.cs
│   └── MainWindowViewModel.cs  # 构造函数注入 IEmbedService
├── Views/
│   ├── MainWindow.axaml        # 工具栏 / 嵌入区域 / 状态栏
│   └── MainWindow.axaml.cs     # 仅 InitializeComponent + EmbedHostControl 属性
├── Models/                     # 数据模型（预留）
└── GodotApplication/           # GorgeGodotPlugin.exe 及运行时
```

### 分层约束

| 层 | 可以引用 | 禁止引用 |
|---|---|---|
| View | ViewModel（DataContext） | Service、Interop |
| ViewModel | Service 接口 | View 控件、Interop |
| Service | Interop | View、ViewModel |
| Interop | 无 | 所有上层 |

**ViewModel 通过构造函数注入 `IEmbedService`，不持有 View 引用。**  
**DI 容器在 `App.axaml.cs` 中配置，使用 `Microsoft.Extensions.DependencyInjection`。**

## Key Conventions

- **MVVM**: CommunityToolkit.Mvvm（`[ObservableProperty]`, `[RelayCommand]`）
- **P/Invoke**: 统一使用 `[LibraryImport]`（编译时源生成），不用 `[DllImport]`
- **命名**: 接口 `I` 前缀，服务 `Service` 后缀，ViewModel `ViewModel` 后缀
- **异步**: ViewModel 命令使用 `AsyncRelayCommand`（`[RelayCommand]` 自动生成）
- **线程**: UI 更新通过 `Avalonia.Threading.Dispatcher.UIThread.Post`
- **Win32 常量**: 定义在 `Win32WindowEmbedder.cs` 中，不散落各处

## Important Notes

- `build_env.bat` 设置 `APPDATA`、`ProgramData` 等环境变量，NuGet restore 依赖这些变量。不要在 TUI 中直接运行 `dotnet build`。
- `LibraryImport` 生成的代码需要 `unsafe`：`.csproj` 中已启用 `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`
- 64 位 Windows 上 `GetWindowLong`/`SetWindowLong` 不存在，P/Invoke 使用 `EntryPoint = "GetWindowLongPtrW"` / `"SetWindowLongPtrW"`
- `EmbedService` 在构造函数中订阅 `Window.Closing` 自动清理，无需在 View 层手动 Dispose
- 项目 pin .NET 9.0.301 SDK（`global.json`），NuGet 配置在根目录 `NuGet.Config`
