# WinForms to Avalonia Converter

A comprehensive .NET 10 application that automatically converts Windows Forms projects to Avalonia 11.x with MVVM architecture, intelligent layout detection, and extensible plugin system.

## Features

### ✨ Core Capabilities
- **Automatic Conversion**: Converts WinForms `.Designer.cs` files to Avalonia AXAML markup and ViewModels
- **Intelligent Layout Detection**: Analyzes control positioning to detect Grid, StackPanel, DockPanel patterns with confidence scoring
- **MVVM Architecture**: Generates ViewModels using CommunityToolkit.Mvvm with `[ObservableProperty]` and `[RelayCommand]` attributes
- **Event to Command**: Automatically converts event handlers to ICommand
- **Style Extraction**: Detects common property patterns and generates reusable Avalonia Styles
- **Resource Conversion** *(planned)*: Transforming `.resx` files to Avalonia `.axaml` resource dictionaries is not implemented yet

### 🔄 Incremental Conversion
- **File Hash Tracking**: SHA256-based change detection for reconverting only modified files (`--incremental`/`--force`)
- **Partial Classes**: ViewModels split into `.g.cs` (generated) and `.cs` (manual edits preserved)
- **Rollback Support**: Transactional file operations with automatic rollback on errors or cancellation
- **Checkpointing** *(planned)*: The checkpoint manager exists but resuming an interrupted run (`--resume`) is not wired up yet

### 🔌 Plugin Architecture
- **Extensible Mappings**: Plugin interfaces for custom control/property/layout converters (interfaces and assembly discovery exist; not yet consulted by the built-in mapping registries)
- **Third-Party Controls** *(planned)*: Generating compilable placeholder stubs for DevExpress, Telerik, etc. is not implemented yet
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

### Build from Source

```bash
cd winforms-to-avalonia-converter/src
dotnet restore
dotnet build
```

### Run

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
dotnet run -- init-config

# Convert a single project
dotnet run -- convert \
  --input /path/to/MyApp.csproj \
  --output /path/to/output \
  --layout auto

# Convert entire solution
dotnet run -- convert \
  --input /path/to/MySolution.sln \
  --output /path/to/output \
  --create-branch \
  --report report.html
```

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
dotnet run -- init-config --output .converterconfig
```

#### `list-plugins` - List Available Plugins

```bash
dotnet run -- list-plugins --plugins ./plugins
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
  "gitIntegration": {
    "enabled": true,
    "createFeatureBranch": true,
    "branchNamePattern": "feature/avalonia-migration-{timestamp}"
  }
}
```

## Development Status

### ✅ Completed
- Project structure and build configuration
- Plugin abstraction interfaces
- Configuration system with JSON loader, merged with explicit CLI flags
- Transactional rollback management (a failed or cancelled run cleans up every file it wrote)
- File hash tracking powering incremental conversion (`--incremental`/`--force`)
- CLI framework with System.CommandLine
- Roslyn-based WinForms parser (control hierarchy, properties, event-handler subscriptions, data bindings)
- Layout detection algorithms (Grid/StackPanel/DockPanel/Canvas, weighted confidence scoring, container-type fast paths)
- Control, property, and event mapping registries
- Code generators (AXAML, ViewModels with real `[ObservableProperty]`/`[RelayCommand]` members, code-behind, style extraction, project scaffolding)
- Git integration with LibGit2Sharp
- Multi-format reporting system
- Migration guide generator, including concrete manual-steps reporting
- Comprehensive unit test suite (`Converter.Tests`, xUnit)

### 📋 Planned
- Plugin loader consumption (discovery exists; the mapping registries don't yet consult loaded plugins)
- Checkpoint/resume support (`--resume`)
- `.resx` resource conversion
- WinForms event-handler body migration (handlers are mapped to commands; the original code isn't ported automatically yet)

## Contributing

Contributions are welcome! Areas needing implementation:

1. **`.resx` Parsing**: Convert WinForms resource files to Avalonia resource dictionaries
2. **Plugin Consumption**: Wire `PluginLoader`-discovered plugins into the control/property/event mapping registries (currently static classes with no extension seam)
3. **Checkpoint/Resume**: Design and implement `--resume` for interrupted conversions, accounting for the parallel form-conversion path
4. **Event-Handler Body Migration**: Locate and migrate the original handler method bodies from the non-designer form `.cs` file, not just the event-to-command mapping
5. **Plugins**: Sample plugins for popular third-party controls (DevExpress, Telerik)

## License

MIT License - see [LICENSE](../LICENSE) file for details.

## Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform XAML framework
- [Roslyn](https://github.com/dotnet/roslyn) - .NET compiler platform
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) - MVVM source generators
- [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) - Git integration

---

**Status**: Core conversion pipeline functional and covered by an automated test suite; plugin consumption, `.resx` conversion, checkpoint/resume, and event-handler body migration remain planned.
