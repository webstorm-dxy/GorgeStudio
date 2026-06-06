---
name: gorge-cli
description: >
  GorgeStudio.Cli command-line tool for compiling, saving, launching, and
  inspecting Gorge chart packages (.gpkg). Use when debugging compilation
  errors, verifying round-trip save/load, launching Godot to test charts,
  inspecting chart element structure, or performing any chart operation
  from the terminal without the GUI.
---

# GorgeStudio CLI

`GorgeStudio.Cli.exe` is the command-line interface for GorgeStudio. It supports
both one-shot commands and an interactive shell mode. Build with:

```bash
dotnet build GorgeStudio.Cli/GorgeStudio.Cli.csproj
```

The executable is at `GorgeStudio.Cli/bin/Debug/net9.0/GorgeStudio.Cli.exe`.

## One-shot commands

All one-shot commands exit after completion. Useful for scripting and CI.

### compile — compile a chart package

```bash
GorgeStudio.Cli.exe compile --input <path-to.gpkg>
```

Loads and compiles the .gpkg file. Prints class/enum counts and compile time.
Exit code 0 on success, 1 on failure (error message to stderr).

### save — load and re-save a chart package

```bash
GorgeStudio.Cli.exe save --input <path-to.gpkg> [--output <path-to.gpkg>]
```

Loads the input .gpkg, builds the chart document, and saves it back. If
`--output` is omitted, overwrites the input file. Use this to normalize a
package or verify that a round-trip load→save→compile succeeds.

### launch — save and launch Godot

```bash
GorgeStudio.Cli.exe launch --input <path-to.gpkg>
```

Loads the chart, saves it, launches `GorgeGodotPlugin.exe`, waits for the UDP
runtime helper to connect, then sends the `.gpkg` path to the running Godot
instance. The Godot process will display the chart.

### inspect — inspect chart internals

```bash
GorgeStudio.Cli.exe inspect classes --input <path-to.gpkg>
GorgeStudio.Cli.exe inspect elements --input <path-to.gpkg>
```

- `classes` — lists all compiled classes (name, Chart/Native flags) and enums.
- `elements` — builds the full Staff→Period→Element tree and prints it.

### forms — list available forms

```bash
GorgeStudio.Cli.exe forms list
```

Discovers all form assemblies from the configured forms directory and lists
them with name, version, and directory name.

### Common debugging workflow

```bash
# 1. Compile to check for errors
GorgeStudio.Cli.exe compile --input test.gpkg

# 2. Round-trip: load → save → compile the saved result
GorgeStudio.Cli.exe save --input test.gpkg --output test_fixed.gpkg
GorgeStudio.Cli.exe compile --input test_fixed.gpkg

# 3. Inspect element structure
GorgeStudio.Cli.exe inspect elements --input test_fixed.gpkg

# 4. Launch in Godot to visually verify
GorgeStudio.Cli.exe launch --input test_fixed.gpkg
```

## Interactive shell

```bash
GorgeStudio.Cli.exe shell
```

Enters a readline-style REPL. All commands below are shell-only (not one-shot).

### Session management

| Command | Description |
|---|---|
| `load <path>` | Load a .gpkg file as the current session |
| `compile [--input <path>]` | Re-compile current or specified .gpkg |
| `save` | Save current session to its loaded path |
| `save-as <path>` | Save current session to a new path |
| `launch` | Save current session and launch in Godot |
| `exit` / `quit` | Exit the shell |

### Forms

| Command | Description |
|---|---|
| `forms list` | List discovered form assemblies |

### Tracks (谱表)

| Command | Description |
|---|---|
| `tracks list` | List all staffs with index, class name, display name, period count |
| `track add [--type ElementStaff\|AudioStaff] [--form <name>]` | Add a new staff |
| `track delete --index <n>` | Remove staff at index |
| `track copy --index <n>` | Duplicate staff at index |
| `track rename --index <n> --name <text>` | Change staff display name |

### Periods (乐段)

| Command | Description |
|---|---|
| `period add --track <n> --time <s>` | Add period to track at given time (seconds) |
| `period delete --track <n> --index <n>` | Remove period from track |
| `period copy --track <n> --index <n>` | Duplicate period |
| `period set-time --track <n> --index <n> --time <s>` | Change period start time |
| `period set-min-length --track <n> --index <n> --length <s>` | Change period minimum length |
| `period` (no subcommand) | List periods for the track (requires `--track <n>`) |

### Inspection

| Command | Description |
|---|---|
| `inspect classes` | List all classes and enums from the compiled project |
| `inspect elements` | Walk Staff→Period→Element tree, print full paths |

### Settings

| Command | Description |
|---|---|
| `settings get` | Show BPM, Offset, BeatsPerBar, SubdivisionsPerBeat, Forms |
| `settings set --key <key> --value <v>` | Set a specific setting: `bpm`, `offset`, `beatsperbar`, `subdivisionsperbeat` |

### Snap

| Command | Description |
|---|---|
| `snap set --mode off\|bar\|beat\|sub` | Set timeline snap mode |

## Error handling conventions

- All errors go to stderr, normal output to stdout.
- Exit code 0 = success, 1 = error.
- Compilation errors include the source file path and line/column range.
- If a command requires a loaded session and none exists, the error message
  will say "无已加载的谱面" (no loaded chart).

## Project references

The CLI depends on `GorgeStudio.AppCore` which provides all shared services
(chart loading, code generation, packaging, Godot launch workflow). See the
AppCore skill for service architecture details.
