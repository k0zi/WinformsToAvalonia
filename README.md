# WinForms to Avalonia Converter

A comprehensive .NET 10 application that automatically converts Windows Forms projects to Avalonia 11.x with MVVM architecture, intelligent layout detection, and extensible plugin system.

## 🚀 Project Status

**Current Phase**: Core Conversion Pipeline Functional - Actively Tested

### ✅ Completed Components
- **Solution Structure**: 8 projects targeting .NET 10
  - Converter.Core (parsing, analysis, infrastructure)
  - Converter.Plugin.Abstractions (extensibility interfaces)
  - Converter.Mappings (control/property mappings)
  - Converter.Generator (code generation)
  - Converter.Reporting (multi-format reports)
  - Converter.Documentation (migration guides)
  - Converter.Cli (command-line interface)
  - Converter.Tests (xUnit test suite covering the parser, generators, mapping registries, layout analyzer, and end-to-end orchestrator runs)

- **Conversion Engine**:
  - Roslyn-based WinForms parser: extracts control hierarchy, properties, event-handler subscriptions, and data bindings from `.Designer.cs` files
  - Layout detection: Grid/StackPanel/DockPanel/Canvas confidence scoring (configurable weights), with fast-path detection for `TableLayoutPanel`/`FlowLayoutPanel`/`SplitContainer`
  - AXAML generator: maps controls and converts properties (colors, fonts, dock, location) into valid Avalonia attributes; unmapped custom/third-party controls still render their mapped children
  - ViewModel generator: produces real `[ObservableProperty]` fields (from data bindings) and `[RelayCommand]` methods (from event handlers) using CommunityToolkit.Mvvm
  - Style extraction: generates shared Avalonia styles for controls with repeated property values
  - Migration guide generator: reports concrete manual steps (unmapped controls, properties needing custom logic, event handlers needing manual porting) instead of a generic checklist

- **Plugin Architecture**: Extensible interfaces for custom converters (not yet consulted by the built-in mapping registries - see below)
  - `IControlMapper` - Custom control mapping
  - `IPropertyTranslator` - Property translation
  - `ILayoutAnalyzer` - Layout detection
  - `ICodeGenerator` - Code generation
  - `IValidationRule` - Custom validation

- **Configuration System**: JSON-based `.converterconfig`, merged with explicit CLI flags, covering:
  - Custom control mappings
  - Third-party library handling
  - Style extraction rules
  - Layout detection thresholds and weights
  - Naming conventions (root namespace, ViewModel suffix) and exclude patterns
  - Git integration settings (branch creation, branch name pattern)
  - Plugin configuration

- **Infrastructure Services**:
  - File hash tracking (SHA256) powering `--incremental`/`--force`
  - Transactional rollback manager - a failed or cancelled run cleans up every file it wrote
  - Configuration loader with validation

- **CLI Framework**: Complete command structure using System.CommandLine
  - `convert` - Convert WinForms to Avalonia
  - `init-config` - Generate configuration template
  - `init-plugin` - Create plugin project (planned)
  - `list-plugins` - List available plugins (planned)

### 🚧 Not Yet Implemented
- Plugin consumption: `PluginLoader` can discover plugin assemblies, but the mapping registries don't yet consult loaded plugins
- Checkpoint/resume support (`--resume`) - resuming a parallelized conversion batch needs its own design pass
- `.resx` resource conversion (WinForms resources → Avalonia resource dictionaries)
- WinForms event-handler *body* migration - events are mapped to commands, but the original handler code isn't ported automatically yet

## 📁 Repository Structure

```
├── LICENSE                           # MIT License
├── README.md                         # This file
└── winforms-to-avalonia-converter/
    └── src/                          # Source code
        ├── Converter.sln             # Visual Studio solution
        ├── README.md                 # Detailed documentation
        ├── Converter.Core/
        ├── Converter.Plugin.Abstractions/
        ├── Converter.Mappings/
        ├── Converter.Generator/
        ├── Converter.Reporting/
        ├── Converter.Documentation/
        ├── Converter.Cli/
        └── Converter.Tests/
```

## 🎯 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Build

```bash
cd winforms-to-avalonia-converter/src
dotnet restore
dotnet build
```

### Run

```bash
cd Converter.Cli
dotnet run -- --help
```

### Test

```bash
cd winforms-to-avalonia-converter/src
dotnet test
```

### Generate Configuration Template

```bash
dotnet run -- init-config
```

## 📖 Documentation

See [src/README.md](winforms-to-avalonia-converter/src/README.md) for detailed documentation including:
- Feature overview
- Architecture design
- Configuration options
- CLI usage examples
- Development roadmap

## 🎯 Feature Status

| Feature | Status |
|---|---|
| Automatic Conversion (WinForms → Avalonia AXAML + ViewModels) | ✅ Implemented |
| Intelligent Layouts (Grid, StackPanel, DockPanel, Canvas) | ✅ Implemented |
| MVVM Architecture (CommunityToolkit.Mvvm integration) | ✅ Implemented |
| Event to Command (automatic ICommand generation) | ✅ Implemented |
| Style Extraction | ✅ Implemented |
| Incremental Updates (hash-based change detection) | ✅ Implemented |
| Git Integration (feature branch creation and commits) | ✅ Implemented |
| Comprehensive Reports (HTML/JSON/Markdown/CSV) | ✅ Implemented |
| Migration Guides (auto-generated, with concrete manual steps) | ✅ Implemented |
| Resource Conversion (`.resx` → `.axaml` dictionaries) | 🚧 Planned |
| Plugin System (third-party control handlers) | 🚧 Planned |
| Checkpoint/Resume (`--resume`) | 🚧 Planned |
| Event-Handler Body Migration | 🚧 Planned |
| Avalonia 12 Support (generated projects currently target 11.2.0) | 🚧 Planned |

## 🤝 Contributing

This project is in active development. Contributions welcome! See [src/README.md](winforms-to-avalonia-converter/src/README.md) for areas needing implementation.

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 👤 Author

David Kozma (2025)
