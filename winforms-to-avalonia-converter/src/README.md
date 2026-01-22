# WinForms to Avalonia Converter

A comprehensive .NET 9 application that automatically converts Windows Forms projects to Avalonia 11.x with MVVM architecture, intelligent layout detection, and extensible plugin system.

## Features

### ✨ Core Capabilities
- **Automatic Conversion**: Converts WinForms `.Designer.cs` files to Avalonia AXAML markup and ViewModels
- **Intelligent Layout Detection**: Analyzes control positioning to detect Grid, StackPanel, DockPanel patterns with confidence scoring
- **MVVM Architecture**: Generates ViewModels using CommunityToolkit.Mvvm with `[ObservableProperty]` and `[RelayCommand]` attributes
- **Event to Command**: Automatically converts event handlers to ICommand with parameter detection
- **Resource Conversion**: Transforms `.resx` files to Avalonia `.axaml` resource dictionaries with localization support
- **Style Extraction**: Detects common property patterns and generates reusable Avalonia Styles

### 🔄 Incremental Conversion
- **File Hash Tracking**: SHA256-based change detection for reconverting only modified files
- **Partial Classes**: ViewModels split into `.g.cs` (generated) and `.cs` (manual edits preserved)
- **Checkpointing**: Automatic progress saving with resume capability for large projects
- **Rollback Support**: Transactional file operations with automatic rollback on errors

### 🔌 Plugin Architecture
- **Extensible Mappings**: Plugin interfaces for custom control/property/layout converters
- **Third-Party Controls**: Generates compilable placeholder stubs for DevExpress, Telerik, etc.
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
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
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
- Configuration system with JSON loader
- Checkpoint and rollback management
- File hash tracking for incremental conversion
- CLI framework with System.CommandLine

### 🚧 In Progress
- Roslyn-based WinForms parser
- Layout detection algorithms
- Control mapping registry
- Code generators (AXAML, ViewModels)

### 📋 Planned
- Git integration with LibGit2Sharp
- Multi-format reporting system
- Migration guide generator
- Plugin loader and discovery
- Comprehensive unit tests

## Contributing

Contributions are welcome! Areas needing implementation:

1. **Parser Implementation**: Roslyn-based `.Designer.cs` and `.resx` parsing
2. **Layout Analyzers**: Grid/StackPanel/DockPanel detection algorithms
3. **Control Mappings**: Comprehensive WinForms → Avalonia control mappings
4. **Generators**: AXAML and ViewModel code generation
5. **Plugins**: Sample plugins for popular third-party controls (DevExpress, Telerik)

## License

MIT License - see [LICENSE](../LICENSE) file for details.

## Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform XAML framework
- [Roslyn](https://github.com/dotnet/roslyn) - .NET compiler platform
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) - MVVM source generators
- [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) - Git integration

---

**Status**: Initial implementation - core architecture complete, conversion engine in development
