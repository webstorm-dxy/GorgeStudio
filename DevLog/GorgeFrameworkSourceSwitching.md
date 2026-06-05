# Gorge Framework Source Switching

## Overview

GorgeStudio supports building against two sets of Gorge framework C# sources:

| Mode | Source location | MSBuild property value |
|---|---|---|
| **Local** (default) | Within this repository: `GorgeCore/gorge-core-csharp/src/`, `GorgeCompileFramework/gorge-compiler/src/` | `Local` |
| **Plugin** | External Godot plugin: `GorgePluginGodot/addons/gorgeplugin/GorgeTools/GorgeCoreCSharp/src/`, `GorgeCompiler/src/` | `Plugin` |

The switch happens at MSBuild build time. All projects in the solution (`GorgeCore`, `GorgeCompileFramework`, `GorgeCompilerToolchain`, `GorgeStudio`) reference the same Gorge types, avoiding type identity conflicts, serialization mismatches, and binary incompatibility.

## Quick Start

```bash
# Default build using local (in-repo) Gorge sources
.\build_env.bat

# Build using plugin (external) Gorge sources
.\build_env.bat /p:GorgeFrameworkSource=Plugin

# Plugin build with explicit plugin repo path
dotnet build GorgeStudio.sln /p:GorgeFrameworkSource=Plugin /p:GorgePluginGodotRoot=C:\Users\daxingyi\RiderProjects\GorgePluginGodot
```

## MSBuild Properties

| Property | Description | Default |
|---|---|---|
| `GorgeFrameworkSource` | `Local` or `Plugin` — which source tree to compile | `Local` |
| `GorgePluginGodotRoot` | Root directory of GorgePluginGodot repository | `C:\Users\daxingyi\RiderProjects\GorgePluginGodot` |

Derived paths (set in `Directory.Build.props`):

| Property | Value |
|---|---|
| `GorgePluginToolsRoot` | `$(GorgePluginGodotRoot)\addons\gorgeplugin\GorgeTools` |
| `GorgePluginCoreSourceRoot` | `$(GorgePluginToolsRoot)\GorgeCoreCSharp\src` |
| `GorgePluginCompilerSourceRoot` | `$(GorgePluginToolsRoot)\GorgeCompiler\src` |

All derived properties are computed from `GorgePluginGodotRoot`, so overriding that single property adjusts everything.

## How It Works

### 1. `Directory.Build.props`
Defines the MSBuild properties with defaults. Only sets values when the property is not already defined on the command line (via `/p:`).

### 2. `Directory.Build.targets`
In Plugin mode, validates that both external source directories exist before build starts. Fails with a clear error message if a path is missing, showing the current `GorgePluginGodotRoot` and how to fix it.

### 3. `GorgeCore/GorgeCore.csproj`
- **Local mode** (default): No change. Compiles local `Class1.cs` and `gorge-core-csharp/src/**/*.cs`.
- **Plugin mode**: Removes local Core sources from `Compile`, and includes `$(GorgePluginCoreSourceRoot)\**\*.cs` with a `PluginGorgeCore\` link prefix for IDE clarity.

### 4. `GorgeCompileFramework/GorgeCompileFramework.csproj`
- **Local mode** (default): No change. Compiles local `Class1.cs` and `gorge-compiler/src/**/*.cs`.
- **Plugin mode**: Removes local compiler sources from `Compile`, and includes `$(GorgePluginCompilerSourceRoot)\**\*.cs` with a `PluginGorgeCompiler\` link prefix.

The `ProjectReference` to `GorgeCore` is always preserved — it ensures both the compiler and core use the same built assembly.

### 5. `GorgeStudio.csproj` and `GorgeCompilerToolchain.csproj`
Not modified. They reference GorgeCore and GorgeCompileFramework via `ProjectReference` and automatically pick up whichever source version was used for those projects.

### 6. `build_env.bat`
Appends `%*` to relay command-line arguments through to `dotnet build`.

## Switching Between Modes

When switching from one mode to another, clean stale build artifacts to avoid confusing errors:

```powershell
Remove-Item -Recurse -Force GorgeCore/bin, GorgeCore/obj
Remove-Item -Recurse -Force GorgeCompileFramework/bin, GorgeCompileFramework/obj
Remove-Item -Recurse -Force GorgeCompilerToolchain/bin, GorgeCompilerToolchain/obj
Remove-Item -Recurse -Force GorgeStudio/bin, GorgeStudio/obj
```

Or delete all bin/obj at once:

```powershell
Get-ChildItem -Directory -Recurse -Filter bin,obj | Remove-Item -Recurse -Force
```

## Error Handling

| Scenario | Behavior |
|---|---|
| Plugin mode, external Core path missing | Build fails. Error shows missing path and `/p:GorgePluginGodotRoot=...` fix. |
| Plugin mode, external Compiler path missing | Build fails. Error shows missing path and `/p:GorgePluginGodotRoot=...` fix. |
| Plugin API incompatible with Studio/Toolchain | Compile error in Studio or Toolchain source. Fix minimally at call sites. |
| Plugin sources need additional NuGet packages | Add conditional `PackageReference` to the project that needs it. |
| Local mode after Plugin build with stale cache | Clean bin/obj and rebuild. |

## What Is NOT Modified

- No files in `GorgePluginGodot` are ever changed.
- No external source files are copied into this repository.
- No runtime switching, DLL hot-load, reflection loading, or `AssemblyLoadContext` isolation.
- No UI toggle for switching — it's purely a build-time decision.
- No Godot-related packages or `Godot.NET.Sdk` are added to GorgeStudio projects.
