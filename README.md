# WinForms to Avalonia Converter

A comprehensive .NET 10 application that automatically converts Windows Forms projects to Avalonia 11.x with MVVM architecture, intelligent layout detection, and extensible plugin system.

## 🚀 Project Status

**Current Phase**: Initial Implementation - Core Architecture Complete

### ✅ Completed Components
- **Solution Structure**: 8 projects targeting .NET 10
  - Converter.Core (parsing, analysis, infrastructure)
  - Converter.Plugin.Abstractions (extensibility interfaces)
  - Converter.Mappings (control/property mappings)
  - Converter.Generator (code generation)
  - Converter.Reporting (multi-format reports)
  - Converter.Documentation (migration guides)
  - Converter.Cli (command-line interface)
  - Converter.Tests (unit tests)

- **Plugin Architecture**: Extensible interfaces for custom converters
  - `IControlMapper` - Custom control mapping
  - `IPropertyTranslator` - Property translation
  - `ILayoutAnalyzer` - Layout detection
  - `ICodeGenerator` - Code generation
  - `IValidationRule` - Custom validation

- **Configuration System**: JSON-based `.converterconfig` with:
  - Custom control mappings
  - Third-party library handling
  - Style extraction rules
  - Layout detection thresholds
  - Git integration settings
  - Plugin configuration

- **Infrastructure Services**:
  - File hash tracking (SHA256) for incremental conversion
  - Checkpoint manager with resume capability
  - Transactional rollback manager
  - Configuration loader with validation

- **CLI Framework**: Complete command structure using System.CommandLine
  - `convert` - Convert WinForms to Avalonia
  - `init-config` - Generate configuration template
  - `init-plugin` - Create plugin project (planned)
  - `list-plugins` - List available plugins (planned)

### 🚧 In Development
- Roslyn-based WinForms parser
- Layout detection algorithms
- Control mapping registry
- AXAML and ViewModel generators
- Git integration with LibGit2Sharp
- Multi-format reporting system
- Migration guide generator

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

## 🎯 Planned Features

- **Automatic Conversion**: WinForms → Avalonia AXAML with ViewModels
- **Intelligent Layouts**: Grid, StackPanel, DockPanel detection
- **MVVM Architecture**: CommunityToolkit.Mvvm integration
- **Event to Command**: Automatic ICommand generation
- **Resource Conversion**: `.resx` → `.axaml` dictionaries
- **Style Extraction**: Automatic style generation
- **Incremental Updates**: Hash-based change detection
- **Git Integration**: Feature branch creation and commits
- **Plugin System**: Third-party control handlers
- **Comprehensive Reports**: HTML/JSON/Markdown/CSV formats
- **Migration Guides**: Auto-generated documentation

## 🤝 Contributing

This project is in active development. Contributions welcome! See [src/README.md](winforms-to-avalonia-converter/src/README.md) for areas needing implementation.

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 👤 Author

David Kozma (2025)
