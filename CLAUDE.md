# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build / Run

```bash
# Restore and build the entire solution (.NET 9.0, SDK 9.0.301 pinned in global.json)
dotnet build GorgeStudio.sln

# Build for Release
dotnet build GorgeStudio.sln -c Release

# Run the IDE (Avalonia desktop app)
dotnet run --project GorgeStudio/GorgeStudio.csproj
```

There are no tests in this repository (`GorgeCore.Tests` was intentionally not copied from the upstream GorgeConpile repo). The `GorgeCompilerToolchain` has a `--test` flag that performs a round-trip bytecode serialization check.

## Project architecture

The solution contains four projects layered as follows:

| Project | Type | Role |
|---|---|---|
| `GorgeCore` | Class library | Gorge language runtime types, intermediate-code VM, bytecode serialization |
| `GorgeCompileFramework` | Class library | Compiler frontend/backend: ANTLR4 grammar, multi-pass compilation, expression trees, optimizer |
| `GorgeCompilerToolchain` | Console app | CLI wrapper around the compiler; can compile `.g` files to bytecode |
| `GorgeStudio` | Avalonia desktop app | The language IDE; embeds a Godot game engine window via Win32 interop |

**Dependency chain:** `GorgeStudio` → all three others; `GorgeCompilerToolchain` → `GorgeCore` + `GorgeCompileFramework`; `GorgeCompileFramework` → `GorgeCore` + `Antlr4.Runtime.Standard`.

## GorgeStudio (the IDE)

An **Avalonia 11.3** desktop app using **CommunityToolkit.Mvvm** (source-generated `[ObservableProperty]` / `[RelayCommand]`) and **Microsoft.Extensions.DependencyInjection** for DI.

**Key pattern — DI wiring** (`App.axaml.cs`): The `MainWindow` View is created first, then its `EmbedHostControl` is passed to `EmbedService` (registered as a singleton `IEmbedService`). `MainWindowViewModel` is resolved from the container, receives `IEmbedService` via constructor injection, and is set as the `DataContext`.

```
App.OnFrameworkInitializationCompleted
  ├─ new MainWindow()                    // View first
  ├─ new ServiceCollection()
  │   ├─ AddSingleton<IEmbedService>(new EmbedService(hostControl, mainWindow))
  │   └─ AddTransient<MainWindowViewModel>()
  ├─ mainWindow.DataContext = sp.GetRequiredService<MainWindowViewModel>()
  └─ desktop.MainWindow = mainWindow
```

**View/ViewModel mapping** (`ViewLocator.cs`): Convention-based — replaces `ViewModel` with `View` in the type name and resolves via reflection.

**Window embedding** (`Interop/Win32WindowEmbedder.cs`): Uses Win32 `SetParent` to host an external process (Godot game) inside an Avalonia `Border` control. Multi-window processes are supported — it captures windows in two batches (splash screen first, then main window after a 1.5s delay) and runs a background timer to catch late-appearing windows. The largest window becomes the primary and fills the host area; others are hidden.

**Build-time copy** (`GorgeStudio.csproj`): The `CopyGodotApplication` MSBuild target copies `GodotApplication/` to the output directory after build. `EmbedService.ResolveExePath()` searches the output directory first, then falls back to the source tree (dev mode).

## Compiler pipeline

The compiler in `GorgeCompileFramework` (`Compiler.cs`) runs a **four-pass** compilation:

1. **TypeIdentifierVisitor** — identifies all type/namespace/class declarations and populates the global scope
2. **TypeExtensionVisitor** — resolves inheritance, interface implementations, type extensions
3. **TypeDeclarationVisitor** — resolves member declarations (methods, fields, constructors) and produces `ImplementationCompileTask` objects
4. **DoCompile** on each task — generates `IntermediateCode` (VM instructions) for method bodies, constructors, and field initializers

After all passes, `FreezeImplementation()` finalizes the compile context. The result (`ClassImplementationContext`) can be serialized to/from a custom binary format (`GorgeBinaryWriter`/`GorgeBinaryReader`).

Key namespaces:
- `Gorge.GorgeLanguage.Objective` — runtime type system (`GorgeType`, `GorgeClass`, `CompiledGorgeClass`, `GorgeEnum`, etc.)
- `Gorge.GorgeLanguage.VirtualMachine` — stack-based VM (`IntermediateCode`, `IntermediateCodeVirtualMachine`)
- `Gorge.GorgeCompiler.CompileContext` — scopes and symbols used during compilation
- `Gorge.GorgeCompiler.Visitors` — ANTLR parse-tree visitors that drive each pass

## Platform constraints

- **Windows only** — `Win32WindowEmbedder` uses `user32.dll` P/Invoke extensively
- **.NET 9.0** with `AllowUnsafeBlocks` enabled on GorgeCore and GorgeStudio
- The Godot runtime DLLs live in `GorgeStudio/GodotApplication/data_GorgeGodotPlugin_windows_x86_64/`

## Build gotchas

**TUI / reduced-shell environments** (DeepSeek TUI, CI runners with minimal env): `dotnet restore` crashes at `Path.Combine(null, "NuGet")` because `%APPDATA%`, `%ProgramData%`, and other standard Windows env vars are not exported. Use `build_env.bat` at the repo root instead of bare `dotnet build`:

```bash
build_env.bat
```

**DLL lock**: The running `GorgeStudio.exe` process locks `bin/Debug/net9.0/GorgeStudio.dll`. Before rebuilding, kill it:

```bash
taskkill /F /IM GorgeStudio.exe 2>nul && dotnet build GorgeStudio.sln
```

## P/Invoke specifics

All Win32 calls in `Win32WindowEmbedder.cs` use `[LibraryImport]` (compile-time source generation), not `[DllImport]`. Implications:

- The containing class must be `partial` (`sealed partial class Win32WindowEmbedder`)
- `private static extern` becomes `private static partial`
- `SetLastError = true` is dropped (LibraryImport always preserves last error)
- `bool` parameters and return values require explicit `[MarshalAs(UnmanagedType.Bool)]`
- 64-bit entry points: `GetWindowLong` / `SetWindowLong` don't exist in 64-bit user32.dll. Use `EntryPoint = "GetWindowLongPtrW"` / `"SetWindowLongPtrW"` with `nint` types, then `(int)` at call sites

## GorgeStudio embedding lifecycle

1. **DI wiring**: `App.axaml.cs` creates `MainWindow` first, extracts `EmbedHostControl`, passes it to `new EmbedService(hostControl, window)` registered as `IEmbedService` singleton. `MainWindowViewModel` receives it via constructor.
2. **Button click** → `LaunchAsync` [RelayCommand] → `IEmbedService.LaunchAsync()`
3. **EmbedService** creates `Win32WindowEmbedder`, resolves `GorgeGodotPlugin.exe` path (output dir → source tree fallback), calls `EmbedAsync(hostControl, window, exePath)`
4. **Win32WindowEmbedder** starts the process, collects windows in two phases (1.5s gap for Godot splash to close), runs a 1-second polling `Timer` for late windows. Every found window is immediately hidden with `ShowWindow(SW_HIDE)`. Only the largest window (`_primaryHwnd`) is shown and sized to fill the host area.
5. **Cleanup**: `EmbedService` subscribes `window.Closing` → `Dispose()`. Disposal restores `SetParent(nullptr)` for all child windows and kills the process.

## Known issues

| Issue | Resolution |
|---|---|
| NuGet restore fails with `Path.Combine(null, ...)` | Run `build_env.bat` instead of bare `dotnet build` |
| DLL locked by running process | `taskkill /F /IM GorgeStudio.exe` before rebuild |
| Multiple standalone Godot windows appearing | Multi-window collection logic with two-phase capture + background timer |
| `EntryPointNotFoundException` for `GetWindowLong` | Use `EntryPoint = "GetWindowLongPtrW"` on 64-bit |
| `SYSLIB1051` / `SYSLIB1062` from LibraryImport | Add `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` and `[MarshalAs(UnmanagedType.Bool)]` |
