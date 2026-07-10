<div align="center">

# WinForms → Avalonia Converter

**Automatically migrate Windows Forms projects to Avalonia 11.x with MVVM, intelligent layout detection, and a plugin-extensible pipeline.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Avalonia](https://img.shields.io/badge/UI-Avalonia%2011.x-8A2BE2)](https://avaloniaui.net/)
[![Tests](https://img.shields.io/badge/tests-xUnit-25A162)](winforms-to-avalonia-converter/src/Converter.Tests)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](#-contributing)

[Quick Start](#-quick-start) •
[Features](#-features) •
[Usage](#-usage) •
[Feature Status](#-feature-status) •
[Documentation](#-documentation) •
[Contributing](#-contributing)

</div>

---

WinForms is stuck on Windows. Avalonia isn't. This CLI parses your `.Designer.cs` files with Roslyn, figures out a sensible layout (Grid, StackPanel, DockPanel, or Canvas), and generates a runnable Avalonia project — AXAML views, `CommunityToolkit.Mvvm` ViewModels, code-behind, and a migration guide that tells you exactly what still needs a human.

It won't do 100% of the work for you (WinForms and Avalonia are different UI frameworks, not just different syntax), but it gets you a compiling, structured starting point instead of a blank page.

## 📚 Table of Contents

- [Features](#-features)
- [Quick Start](#-quick-start)
- [Usage](#-usage)
- [Configuration](#-configuration)
- [Feature Status](#-feature-status)
- [Project Structure](#-project-structure)
- [Documentation](#-documentation)
- [Contributing](#-contributing)
- [License](#-license)

## ✨ Features

- 🔍 **Roslyn-based parsing** — extracts control hierarchy, properties, event-handler subscriptions, and data bindings straight from `.Designer.cs`, no reflection or live WinForms runtime required
- 📐 **Intelligent layout detection** — Grid / StackPanel / DockPanel / Canvas, chosen by confidence scoring (configurable weights), with fast paths for `TableLayoutPanel` / `FlowLayoutPanel` / `SplitContainer`
- 🧩 **Real MVVM output** — generates actual `[ObservableProperty]` fields and `[RelayCommand]` methods via CommunityToolkit.Mvvm, not empty scaffolding
- 🔁 **Event-handler body migration** — your original handler code is preserved as a reference comment inside a correctly-signed, compiling stub, wired into AXAML/ViewModel so it's never dead code
- 🖼️ **`.resx` resource conversion** — strings resolve inline, images extract to `Assets/` with `avares://` references, unrecoverable legacy payloads are flagged instead of silently dropped
- 🔌 **Plugin system** — `AssemblyLoadContext`-isolated plugins can override control/property/event mapping ahead of the built-ins; scaffold one with `init-plugin`
- ♻️ **Incremental & resumable** — hash-based change detection (`--incremental`) and checkpointed resume (`--resume`) so a killed run doesn't mean starting over
- 🌳 **Git-aware** — optional feature-branch creation and auto-commit of the converted output
- 📊 **Real reporting** — HTML/JSON/Markdown/CSV conversion reports and a migration guide with concrete, per-form manual steps

<details>
<summary><strong>Full feature list</strong></summary>

- **Conversion Engine**
  - Layout detection: Grid/StackPanel/DockPanel/Canvas confidence scoring with fast-path detection for `TableLayoutPanel`/`FlowLayoutPanel`/`SplitContainer`
  - AXAML generator: maps controls and converts properties (colors, fonts, dock, location) into valid Avalonia attributes; unmapped custom/third-party controls still render their mapped children
  - ViewModel generator: real `[ObservableProperty]` fields (from data bindings) and `[RelayCommand]` methods (from event handlers)
  - Style extraction: generates shared Avalonia styles for controls with repeated property values
  - Migration guide generator: concrete manual steps (unmapped controls, properties needing custom logic, event handlers needing manual porting) instead of a generic checklist
- **Plugin Architecture** — `IControlMapper`, `IPropertyTranslator`, `IEventMapper`, `ILayoutAnalyzer`, `ICodeGenerator`, `IValidationRule`
- **Configuration System** — JSON `.converterconfig`, merged with explicit CLI flags: custom/third-party control mappings, style extraction rules, layout detection thresholds and weights, naming conventions, exclude patterns, git integration settings, plugin configuration, configurable Avalonia/CommunityToolkit.Mvvm target versions
- **Infrastructure** — SHA256 file hash tracking, transactional rollback (a failed/cancelled run cleans up every file it wrote), per-form checkpointing for `--resume`
- **CLI** — `convert`, `init-config`, `init-plugin`, `list-plugins`, all via System.CommandLine

</details>

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Build

```bash
git clone https://github.com/k0zi/WinformsToAvalonia.git
cd WinformsToAvalonia/winforms-to-avalonia-converter/src
dotnet restore
dotnet build
```

### Test

```bash
dotnet test
```

## 🖥 Usage

```bash
cd Converter.Cli

# Scaffold a config file
dotnet run -- init-config

# Convert a project
dotnet run -- convert \
  --input /path/to/WinFormsApp \
  --output /path/to/AvaloniaApp \
  --layout smart \
  --report report.html

# Resume an interrupted run
dotnet run -- convert -i ./MyApp -o ./MyApp.Avalonia --resume

# Discover / scaffold plugins
dotnet run -- list-plugins --plugins ./plugins
dotnet run -- init-plugin --name "MyPlugin" --output ./plugins/MyPlugin
```

Run `dotnet run -- convert --help` for the full flag list.

## ⚙️ Configuration

```bash
dotnet run -- init-config -o .converterconfig
```

```json
{
  "layoutDetection": { "alignmentTolerance": 5, "confidenceThreshold": 70 },
  "styleExtraction": { "enabled": true, "minimumOccurrence": 3 },
  "resourceConversion": { "enabled": true, "assetsDirectory": "Assets" },
  "eventHandlerMigration": { "enabled": true },
  "gitIntegration": { "enabled": true, "createFeatureBranch": true }
}
```

See [src/README.md](winforms-to-avalonia-converter/src/README.md#configuration) for the full schema.

## 🎯 Feature Status

| Feature | Status |
|---|---|
| Automatic Conversion (WinForms → Avalonia AXAML + ViewModels) | ✅ Implemented |
| Intelligent Layouts (Grid, StackPanel, DockPanel, Canvas) | ✅ Implemented |
| MVVM Architecture (CommunityToolkit.Mvvm integration) | ✅ Implemented |
| Event to Command (automatic ICommand generation) | ✅ Implemented |
| Event-Handler Body Migration (original code preserved as reference comments) | ✅ Implemented |
| Style Extraction | ✅ Implemented |
| Resource Conversion (`.resx` → `Assets/` + `avares://` references) | ✅ Implemented |
| Plugin System (third-party control/property/event handlers) | ✅ Implemented |
| Incremental Updates (hash-based change detection) | ✅ Implemented |
| Checkpoint/Resume (`--resume`) | ✅ Implemented |
| Git Integration (feature branch creation and commits) | ✅ Implemented |
| Comprehensive Reports (HTML/JSON/Markdown/CSV) | ✅ Implemented |
| Migration Guides (auto-generated, with concrete manual steps) | ✅ Implemented |
| Configurable Avalonia/CommunityToolkit.Mvvm Target Version | ✅ Implemented |
| Native Avalonia 12 API/Syntax Support | 🚧 Planned |

## 📁 Project Structure

<details>
<summary>Expand</summary>

```
├── LICENSE
├── README.md
└── winforms-to-avalonia-converter/
    └── src/
        ├── Converter.sln
        ├── README.md                      # Detailed documentation
        ├── Converter.Core/                # Parsing, layout analysis, config, services
        ├── Converter.Plugin.Abstractions/  # Plugin contracts
        ├── Converter.Mappings/             # Built-in control/property/event registries
        ├── Converter.Generator/            # AXAML / ViewModel / code-behind / style generators
        ├── Converter.Reporting/            # HTML/JSON/Markdown/CSV report builder
        ├── Converter.Documentation/        # Migration guide generator
        ├── Converter.Cli/                  # CLI entry point + orchestrator
        ├── Converter.Tests/                # xUnit test suite
        └── Converter.Tests.SamplePlugin/   # Compiled plugin fixture for integration tests
```

</details>

## 📖 Documentation

See **[src/README.md](winforms-to-avalonia-converter/src/README.md)** for the full picture: architecture, configuration schema, CLI reference, and development status.

## 🤝 Contributing

Contributions are welcome — this project is in active development. Check [src/README.md](winforms-to-avalonia-converter/src/README.md#contributing) for areas that still need work, then open a PR or issue.

## 📄 License

MIT License — see [LICENSE](LICENSE).

---

<div align="center">

Made by [David Kozma](https://github.com/k0zi)

</div>
