# WinForms to Avalonia Converter

A comprehensive .NET 10 application that automatically converts Windows Forms projects to Avalonia with MVVM architecture, intelligent layout detection, and extensible plugin system. Targets Avalonia 12.x by default; Avalonia 11.x remains fully supported as an explicit opt-in. Packaged as a dotnet tool (`winforms2avalonia`).

## Features

### ✨ Core Capabilities
- **Automatic Conversion**: Converts WinForms `.Designer.cs` files to Avalonia AXAML markup and ViewModels
- **Intelligent Layout Detection**: Analyzes control positioning to detect Grid, StackPanel, DockPanel patterns with confidence scoring
- **MVVM Architecture**: Generates ViewModels using CommunityToolkit.Mvvm with `[ObservableProperty]` and `[RelayCommand]` attributes
- **Event to Command**: Automatically converts event handlers to ICommand
- **Event-Handler Body Migration**: The original WinForms handler source (from the sibling non-designer `.cs` file) is embedded, verbatim, as an inert reference comment inside a correctly-signed, compiling stub - for `PreserveEventHandler` events (code-behind, also wired as a real AXAML event attribute) and `ConvertToCommand` events (the `[RelayCommand]` stub) alike
- **Style Extraction**: Detects common property patterns and generates reusable Avalonia Styles
- **Resource Conversion**: `.resx` string resources resolve inline; binary/image resources are extracted into `Assets/` and referenced via `avares://` URIs; unrecoverable legacy `BinaryFormatter` payloads are flagged as an explicit manual step

### 🔄 Incremental Conversion
- **File Hash Tracking**: SHA256-based change detection for reconverting only modified files (`--incremental`/`--force`)
- **Partial Classes**: ViewModels split into `.g.cs` (generated) and `.cs` (manual edits preserved)
- **Rollback Support**: Transactional file operations with automatic rollback on errors or cancellation
- **Checkpointing**: `--resume` picks up an interrupted run from its last per-form checkpoint instead of starting over

### 🔌 Plugin Architecture
- **Extensible Mappings**: Plugin interfaces for custom control/property/event converters, discovered via isolated `AssemblyLoadContext` and consulted by the generators ahead of the built-in mapping registries
- **`init-plugin`/`list-plugins`**: Scaffold a new plugin project or discover/list plugins in a directory
- **Custom Generators**: Plugin support for domain-specific code generation

### 🌳 Git Integration
- **Feature Branches**: Automatically creates feature branches for conversions
- **Gitignore Management**: Generates appropriate `.gitignore` entries
- **Checkpoint Commits**: Optional auto-commit at checkpoints

### 📊 Comprehensive Reporting
- **Multiple Formats**: HTML, JSON, Markdown, CSV reports
- **Detailed Metrics**: Conversion success rates, control mappings, manual steps required
- **Migration Guides**: Auto-generated documentation with before/after examples

## Project Structure

```
src/
├── Converter.Core/               # Core parsing, analysis, and infrastructure
│   ├── Configuration/            # .converterconfig loader
│   ├── Models/                   # Domain models (ControlNode, ConversionState)
│   ├── Services/                 # Checkpoint, rollback, hash tracking
│   └── Parsing/                  # Roslyn-based WinForms parser
├── Converter.Plugin.Abstractions/ # Plugin interfaces and contracts
│   ├── IControlMapper.cs
│   ├── IPropertyTranslator.cs
│   ├── ILayoutAnalyzer.cs
│   └── ICodeGenerator.cs
├── Converter.Mappings/           # Built-in control/property mappings
├── Converter.Generator/          # AXAML, ViewModel, code-behind generators
├── Converter.Reporting/          # Multi-format report generation
├── Converter.Documentation/      # Migration guide generator
├── Converter.Cli/                # Command-line interface
└── Converter.Tests/              # Unit tests
```

## Installation

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git (for git integration features)

### Install as a dotnet tool

Not published to nuget.org yet — pack and install locally:

```bash
cd winforms-to-avalonia-converter/src
dotnet pack Converter.Cli/Converter.Cli.csproj -c Release -o ./nupkg
dotnet tool install --global --add-source ./nupkg WinformsToAvalonia.Converter
```

This installs the `winforms2avalonia` command. Re-run `dotnet tool update` (same flags) after pulling changes.

### Build from Source (for development)

```bash
cd winforms-to-avalonia-converter/src
dotnet restore
dotnet build
```

### Run from Source

```bash
cd Converter.Cli
dotnet run -- convert --help
```

### Test

```bash
dotnet test
```

Runs the `Converter.Tests` xUnit suite: parser, generator, mapping registry, layout analyzer, and end-to-end orchestrator tests (the latter run real conversions against temp-directory fixtures).

## Usage

### Quick Start

```bash
# Generate configuration template
winforms2avalonia init-config

# Convert a single project (defaults to Avalonia 12.x output)
winforms2avalonia convert \
  --input /path/to/MyApp.csproj \
  --output /path/to/output \
  --layout auto

# Convert entire solution
winforms2avalonia convert \
  --input /path/to/MySolution.sln \
  --output /path/to/output \
  --create-branch \
  --report report.html
```

Running from source instead of the installed tool? Replace `winforms2avalonia` with `dotnet run --` from inside `Converter.Cli`.

### Command Line Options

#### `convert` - Convert WinForms to Avalonia

| Option | Description | Default |
|--------|-------------|---------|
| `--input, -i` | Path to `.csproj` or `.sln` file | Required |
| `--output, -o` | Output directory | Required |
| `--layout, -l` | Layout mode: `auto`, `canvas`, `smart` | `auto` |
| `--report, -r` | Report output path | None |
| `--report-format` | Report format: `html`, `json`, `md`, `csv` | `html` |
| `--config, -c` | Path to `.converterconfig` | Auto-discover |
| `--plugins, -p` | Plugins directory | `plugins` |
| `--incremental` | Enable incremental conversion | `false` |
| `--force, -f` | Force full reconversion | `false` |
| `--resume` | Resume from checkpoint | `false` |
| `--parallel` | Enable parallel processing | `true` |
| `--create-branch` | Create git feature branch | `false` |
| `--branch-name` | Custom branch name | Auto-generated |
| `--no-git` | Disable git integration | `false` |
| `--migration-guide` | Generate migration docs | `true` |
| `--dry-run` | Validate without generating | `false` |

#### `init-config` - Generate Configuration Template

```bash
winforms2avalonia init-config --output .converterconfig
```

#### `list-plugins` - List Available Plugins

```bash
winforms2avalonia list-plugins --plugins ./plugins
```

## Configuration

Create a `.converterconfig` file in your project root:

```json
{
  "customMappings": [
    {
      "winFormsType": "MyApp.CustomControl",
      "avaloniaType": "MyApp.Avalonia.CustomControl",
      "propertyMappings": {
        "CustomProperty": "AvaloniaProperty"
      }
    }
  ],
  "layoutDetection": {
    "alignmentTolerance": 5,
    "confidenceThreshold": 70
  },
  "styleExtraction": {
    "enabled": true,
    "minimumOccurrence": 3
  },
  "projectGeneration": {
    "avaloniaVersion": "12.0.0",
    "communityToolkitMvvmVersion": "8.3.2",
    "targetFramework": "net10.0"
  },
  "gitIntegration": {
    "enabled": true,
    "createFeatureBranch": true,
    "branchNamePattern": "feature/avalonia-migration-{timestamp}"
  }
}
```

`projectGeneration.avaloniaVersion` is version-aware, not just a version-string passthrough: it drives both the generated `.csproj`'s package references and the one confirmed Avalonia 11→12 breaking API change affecting generated code (`GotFocus`/`LostFocus` handler signatures, resolved via `Converter.Mappings/BuiltIn/EventSignatureRegistry.cs`). Set it to `"11.2.0"` for Avalonia 11.x-compatible output.

## Development Status

### ✅ Completed
- Project structure and build configuration
- Plugin abstraction interfaces, `AssemblyLoadContext`-isolated loading, and full consumption by the AXAML/ViewModel/style generators (`init-plugin`/`list-plugins` CLI commands)
- Configuration system with JSON loader, merged with explicit CLI flags
- Transactional rollback management (a failed or cancelled run cleans up every file it wrote)
- File hash tracking powering incremental conversion (`--incremental`/`--force`)
- Checkpoint/resume support (`--resume`), with per-form checkpointing surviving a hard kill mid-run
- CLI framework with System.CommandLine
- Roslyn-based WinForms parser (control hierarchy, properties, event-handler subscriptions, data bindings, `.resx`-backed resource resolution)
- `.resx` resource conversion: string resources resolved inline, binary/image resources extracted to `Assets/` + `avares://` references, unrecoverable legacy payloads flagged as manual steps
- WinForms event-handler body migration: original handler source preserved as reference comments inside compiling stubs, wired into both code-behind (AXAML event attribute) and ViewModel command paths
- Layout detection algorithms (Grid/StackPanel/DockPanel/Canvas, weighted confidence scoring, container-type fast paths)
- Control, property, and event mapping registries
- Code generators (AXAML, ViewModels with real `[ObservableProperty]`/`[RelayCommand]` members, code-behind, style extraction, project scaffolding)
- Configurable, version-aware Avalonia/CommunityToolkit.Mvvm target package versions (`projectGeneration.avaloniaVersion`/`communityToolkitMvvmVersion`/`targetFramework`) — Avalonia 12.x is the default target, Avalonia 11.x remains fully supported as an opt-in, and `EventSignatureRegistry` tracks the one confirmed 11→12 breaking API change affecting generated code (`GotFocus`/`LostFocus` handler signatures)
- Packaged as a dotnet tool (`winforms2avalonia`, package id `WinformsToAvalonia.Converter`) — `dotnet pack` + local `dotnet tool install`
- Git integration with LibGit2Sharp
- Multi-format reporting system
- Migration guide generator, including concrete manual-steps reporting
- Comprehensive unit test suite (`Converter.Tests`, xUnit)

### 📋 Planned
- Publishing `WinformsToAvalonia.Converter` (and `Converter.Plugin.Abstractions` as its own package) to nuget.org — currently local-pack-and-install only, no CI/publish automation
- Broader Avalonia 12 API validation beyond the one confirmed breaking change already handled (`GotFocus`/`LostFocus`) — the official breaking-changes list covers areas (Android/iOS lifecycle, clipboard `IDataObject` removal, `ToggleButton` event renames, etc.) that don't affect what this tool generates today, but should be re-checked as generator coverage grows

## Contributing

Contributions are welcome! Areas needing implementation:

1. **NuGet Publishing**: Set up CI to publish `WinformsToAvalonia.Converter` to nuget.org, and publish `Converter.Plugin.Abstractions` as its own package so `init-plugin`'s scaffolded `HintPath` reference (currently points at wherever the tool is currently installed, which can go stale across `dotnet tool update`/`uninstall`) can become a proper `PackageReference`
2. **Plugins**: Sample plugins for popular third-party controls (DevExpress, Telerik)

## License

MIT License - see [LICENSE](../LICENSE) file for details.

## Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform XAML framework
- [Roslyn](https://github.com/dotnet/roslyn) - .NET compiler platform
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) - MVVM source generators
- [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) - Git integration

---

**Status**: Core conversion pipeline functional and covered by an automated test suite, including plugin consumption, `.resx` conversion, checkpoint/resume, event-handler body migration, and Avalonia 12 support (now the default target, with 11.x fully supported via config); packaged as a dotnet tool (`winforms2avalonia`), installable locally — nuget.org publishing remains planned.
