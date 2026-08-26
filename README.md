<div align="center">

# WinFormsToAvalonia

**Convert a Windows Forms application into a running Avalonia UI application — Views, ViewModels, controls and all.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.1.1-8B44AC)](https://avaloniaui.net/)
[![MVVM](https://img.shields.io/badge/MVVM-CommunityToolkit-0078D4)](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![PRs welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](#contributing)

[Quick start](#quick-start) · [Usage](#usage) · [Example](#example) · [How it works](#how-it-works) · [Limitations](#limitations) · [Contributing](#contributing)

</div>

---

## Overview

`WinFormsToAvalonia` is a .NET 10 command-line tool that reads a Windows Forms project — .NET
Framework or modern .NET — and **generates a brand-new Avalonia application** next to it. The
source project is never modified.

- Every `Form` becomes an Avalonia `Window` **View + ViewModel** pair (CommunityToolkit.Mvvm).
- Every `UserControl` becomes an Avalonia `UserControl` View + ViewModel, and is registered as a
  real control mapping, so a Form that hosts one gets the generated element.
- Every WinForms control is mapped to its closest Avalonia equivalent, to one of the tool's own
  **bundled fallback controls** where Avalonia has no built-in match, or reported with tailored
  migration guidance.
- Handler bodies are **translated where that is provable** and **preserved as comments where it
  is not**, inside correctly-signed, already-subscribed Avalonia handlers — so the generated
  project **builds and runs on day one** and migration proceeds one method at a time.

## Features

| | |
|---|---|
| 🔍 **Roslyn-based** | Parses `InitializeComponent` with the C# compiler API — no regex. |
| 🌐 **Reads resources** | `Localizable=true` forms, which set every property through `resources.ApplyResources(...)`, convert with their text, geometry and fonts intact; images are recovered from the `.resx` into `Assets/`. |
| 🧩 **89 control types recognized** | 59 mapped (45 Direct + 14 via bundled fallback controls), 30 more registered with guidance-only migration notes instead of a silent failure. See [`docs/Controls.md`](docs/Controls.md). |
| 📐 **Pixel-accurate layout** | Absolute `Canvas` positioning preserves the original design exactly; `Anchor`/`Dock` are recorded, never guessed at. |
| ⚡ **Always-compiling output** | `dotnet build && dotnet run` works on the generated project immediately — warning-free. |
| ✍️ **Handler bodies translated** | Bindable property access, `Close()`/`Focus()`, `MessageBox.Show`, `Application.Exit()`, opening another Form, and plain-.NET expressions become real Avalonia code; the report says how much came across. |
| 🎯 **Conservative MVVM** | Handlers are promoted to `[RelayCommand]` only when a Roslyn analysis *proves* it is safe — everything else stays event-driven. A handler that only kept a button's `Enabled` in sync becomes that command's `CanExecute` guard. |
| 📦 **Zero extra dependencies** | Fallback controls are copied into the generated project as source; only the ones actually used. |
| 🧪 **Verified end-to-end** | Integration tests convert real WinForms fixture projects and `dotnet build` the result. |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

Generated projects target `net10.0` and reference Avalonia 12.1.1 + CommunityToolkit.Mvvm 8.4.2.
The tool itself has **no Avalonia dependency** — you can run it anywhere the SDK runs.

## Quick start

```bash
git clone https://github.com/k0zi/WinformsToAvalonia.git
cd WinformsToAvalonia

# convert
dotnet run --project src/WinFormsToAvalonia.Cli -- convert \
  --source path/to/YourWinFormsApp.csproj \
  --output ./YourAvaloniaApp

# run the result
cd YourAvaloniaApp
dotnet build && dotnet run
```

Not sure what you're dealing with yet? Start read-only:

```bash
dotnet run --project src/WinFormsToAvalonia.Cli -- analyze --source path/to/YourWinFormsApp.csproj
```

## Usage

| Command | What it does |
|---|---|
| `convert --source <csproj> --output <dir>` | Converts a WinForms project into a new Avalonia project. |
| `analyze --source <csproj>` | Discovery only (nothing written) — lists the Forms/UserControls/Components that would be converted. |
| `list-mappings [--filter <substring>]` | Prints the full WinForms → Avalonia control mapping table. |

**`convert` options**

| Option | Effect |
|---|---|
| `--force` | Allow writing into a non-empty output directory. Files you have already edited are preserved — the regenerated version lands beside them as `*.w2a-new`. |
| `--overwrite-all` | Replace existing output files instead of preserving them. |
| `--dry-run` | Run the full pipeline and print the report, write nothing. |
| `--verbose` | Show every warning, not just the first few. |
| `--no-fallback-controls` | Strict mode: skip fallback controls instead of emitting them. |
| `--skip-code-behind-comments` | Omit the preserved code-behind block. |
| `--log-file <path>` | Also write the conversion report as JSON. |

## Example

A `UserControl` from [`samples/WinForms`](samples/WinForms), converted into
[`samples/Avalonia`](samples/Avalonia).

<table>
<tr><th>WinForms — <code>DemoUserControl.Designer.cs</code></th><th>Avalonia — <code>DemoUserControlView.axaml</code></th></tr>
<tr valign="top"><td>

```csharp
this.counterLabel.Location = new Point(8, 34);
this.counterLabel.Size = new Size(60, 20);
this.counterLabel.Text = "0";

this.incrementButton.Location = new Point(74, 30);
this.incrementButton.Size = new Size(96, 26);
this.incrementButton.Text = "Increment";
this.incrementButton.Click +=
    new EventHandler(this.incrementButton_Click);
```

</td><td>

```xml
<Canvas>
  <Button x:Name="incrementButton"
          Canvas.Left="74" Canvas.Top="30"
          Width="96" Height="26"
          Content="Increment"
          Command="{Binding IncrementButtonCommand}" />
  <TextBlock x:Name="counterLabel"
             Canvas.Left="8" Canvas.Top="34"
             Width="60" Height="20"
             Text="{Binding CounterLabelText, Mode=TwoWay}" />
</Canvas>
```

</td></tr>
</table>

`incrementButton_Click` only touched a bindable property on a directly-mapped control, so it was
promoted to a command on the generated ViewModel — and because that same proof makes the body
translatable, it comes across as working code rather than a comment:

```csharp
public sealed partial class DemoUserControlViewModel : ViewModelBase
{
    /// <summary>Bound to counterLabel.Text in the view.</summary>
    [ObservableProperty]
    public partial string CounterLabelText { get; set; } = "0";

    [RelayCommand]
    private void IncrementButton()
    {
        CounterLabelText = (int.Parse(CounterLabelText) + 1).ToString();
    }
}
```

Bodies the tool cannot prove equivalent are still preserved as a comment inside the method that
replaced them, with a `MigrationTodo.NotMigrated(...)` marker below — see
[Code-behind migration](#code-behind-migration).

## How it works

### Layout: absolute, not guessed

Every container becomes an Avalonia `Canvas`; children keep their original WinForms
`Location`/`Size` as `Canvas.Left`/`Canvas.Top`/`Width`/`Height`. The result is pixel-accurate and
fully deterministic. `Anchor`/`Dock` are **preserved** — as an XML comment *and* a `w2a:LayoutHint`
attached property — for later manual responsive-layout work, and never auto-translated into
something the tool cannot verify.

### Fallback controls

WinForms controls with no built-in Avalonia equivalent (`GroupBox`, `StatusStrip`, `ToolStrip`,
`MaskedTextBox`, `RichTextBox`, `ErrorProvider`, `PropertyGrid`, `BindingNavigator`, `WebBrowser`,
`PrintPreviewControl`, …) are backed by a fixed set of controls the tool ships itself. Only the ones
actually used are copied into the generated project's `Controls/` folder — as source, with no extra
NuGet dependency. One template is not a control at all: `MessageBoxFallback` is a small modal dialog
that a *translated handler body* pulls in when it calls `MessageBox.Show(...)`, since Avalonia ships
no message box.

### Code-behind migration

WinForms handler bodies are dominated by imperative control-API code —
`treeView1.Nodes.Add(...)`, `e.Effect = DragDropEffects.Copy`, `errorProvider.SetError(...)`,
`new DashboardForm().ShowDialog()`. That code addresses controls by field name, which Avalonia
*code-behind* can do unchanged (`x:Name` generates a field), but a ViewModel cannot. So:

> **Code-behind is the default target. Promotion to the ViewModel is the exception, and it happens
> only when a Roslyn analysis of the handler's own body proves it is possible.**

<details>
<summary><b>The six conditions for <code>[RelayCommand]</code> promotion</b></summary>

<br>

A handler becomes a `[RelayCommand]` (plus `Command="{Binding …}"` on its control) only when
**all** of these hold:

1. it is a `Click` on a control that maps to a real Avalonia `Button`/`MenuItem`;
2. it is wired to exactly one control (a shared handler needs `sender`);
3. its body uses neither `sender` nor the `EventArgs`;
4. it drives no Form member (`Close()`, `Hide()`, `DialogResult`, …) and opens no other Form;
5. it calls no code-behind helper method;
6. every control member it touches is a two-way bindable value property (`Text`, `Checked`,
   `Value`, `SelectedItem`, `Enabled`, `Visible`) on a directly-mapped control.

Everything else stays event-driven, exactly as in WinForms: mouse/keyboard/drag-drop handlers,
control-API code, form lifecycle (`Load` → `Loaded`, `FormClosing` → `Closing`), navigation, and
anything the analyzer cannot classify with confidence. Each such decision — and its reason — is
reported in the conversion output.

</details>

Handler *bodies* are translated a statement at a time, under the same evidence rule as everything
else: a statement is emitted as real Avalonia code only when it is provably equivalent, and
everything else stays preserved as a comment inside the method that replaced it, followed by a call
to the generated `MigrationTodo.NotMigrated(...)` marker. What gets translated today: writes and
reads of bindable control properties (`label1.Text`, `checkBox1.Checked`, `Enabled`, `Visible`, …),
`Close()`/`Show()`/`Hide()`, `control.Focus()`, `MessageBox.Show(...)` (via a bundled fallback
dialog, which makes the handler `async`), `Application.Exit()`, opening another converted Form
(`new SettingsForm().ShowDialog()` → `await new SettingsView().ShowDialog(this)`), and any
plain-.NET expression around them — interpolated strings included. Anything else — control APIs with no Avalonia counterpart, helper calls, locals, control flow
— stops the translation, and **translation stops at the first statement it cannot handle** so the
emitted prefix is a faithful partial execution rather than a method that silently skips work.

A promoted `[RelayCommand]` almost always translates *completely*: promotion already proved every
member its body touches is bindable, so the same body rewrites cleanly against the ViewModel's own
generated properties. The conversion report prints how many statements came across in total.

The generated project therefore always builds **and runs**, and the unit of manual migration is one
method rather than one file-sized comment block.

The marker reports (to stderr and to the debugger, once per member) rather than throwing, because
Avalonia invokes these handlers from the framework — a `TabControl` selects its first tab, a `Window`
raises `Loaded` — so a throwing stub used to kill the converted app during XAML initialization,
before its first window appeared. Set `MigrationTodo.ThrowOnUnmigratedCall = true` (e.g. in a smoke
test) to make un-migrated code fail loudly again, and read `MigrationTodo.ReportedMembers` to see
what ran un-migrated.

## Project layout

```
src/
  WinFormsToAvalonia.Cli/               Spectre.Console.Cli command-line host
  WinFormsToAvalonia.Core/              parsing, mapping, emission, scaffolding (no Avalonia dependency)
  WinFormsToAvalonia.FallbackControls/  bundled fallback control templates (embedded resources)
tests/
  WinFormsToAvalonia.Core.Tests/        unit tests (parsing, mapping, emission)
  WinFormsToAvalonia.Integration.Tests/ end-to-end: converts real WinForms fixture projects and
                                        `dotnet build`s the generated output
samples/
  WinForms/                             a WinForms app exercising the full control surface
  Avalonia/                             the same app after conversion
docs/
  Controls.md                           per-type WinForms → Avalonia mapping status + implementation plan
  known-limitations.md                  what isn't handled yet
```

## Limitations

This tool gets you a running Avalonia app and a per-method migration list — not a finished port.
Layout stays absolute, handler bodies stay commented, and some WinForms concepts have no Avalonia
counterpart at all. [`docs/known-limitations.md`](docs/known-limitations.md) is an honest,
current-state list; [`docs/Controls.md`](docs/Controls.md) has the per-type status.

## Contributing

Contributions are welcome — especially new control mappings and fallback controls.

```bash
dotnet build WinFormsToAvalonia.slnx
dotnet test  WinFormsToAvalonia.slnx
```

Integration tests shell out to `dotnet build` on the generated output, so a full run is slower than
a typical unit-test suite.

A few conventions worth knowing before opening a PR:

- **New control mapping** → add an `IControlMapper` entry to `DefaultControlMappers.All` and update
  the table in `docs/Controls.md` (the code is the source of truth; the doc is a hand-maintained
  snapshot).
- **New fallback control** → add the template to `WinFormsToAvalonia.FallbackControls/Templates/`
  *and* a `FallbackControlCatalog.All` entry. Templates are embedded text, not compiled — the real
  check is an integration test that builds a project using them.
- **Anything that changes generated code** → add a `SampleApps` fixture and assert the output still
  builds.

## License

[MIT](LICENSE) © David Kozma
