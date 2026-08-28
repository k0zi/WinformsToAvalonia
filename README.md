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
| 🧩 **92 control types recognized** | 59 mapped (45 Direct + 14 via bundled fallback controls), 33 more registered with guidance-only migration notes instead of a silent failure — run `list-mappings` for the live count. See [`docs/Controls.md`](docs/Controls.md). |
| 📐 **Pixel-accurate layout** | Absolute `Canvas` positioning preserves the original design exactly; `Anchor`/`Dock` are recorded, never guessed at. |
| ⚡ **Always-compiling output** | `dotnet build && dotnet run` works on the generated project immediately — warning-free. |
| ✍️ **Handler bodies translated** | Bindable property access, control flow and loops, `Close()`/`Focus()`, window properties, `MessageBox.Show`, opening another Form (with its `DialogResult` contract), file dialogs, the clipboard, `sender`, non-visual .NET components and your own helper methods become real Avalonia code; the report says how much came across. |
| 🎯 **Conservative MVVM** | Handlers are promoted to `[RelayCommand]` only when a Roslyn analysis *proves* it is safe — everything else stays event-driven. A private helper the handler calls moves with it when it can. A handler that only kept a button's `Enabled` in sync becomes that command's `CanExecute` guard. |
| 📦 **Zero extra dependencies** | Fallback controls are copied into the generated project as source; only the ones actually used. |
| 🗺️ **A checklist in the output** | The generated project carries a `MIGRATION.md` listing every method still waiting for a human, with the first statement that stopped it — built from the same data that decides whether to emit the `MigrationTodo` marker, so the two cannot drift apart. |
| 🧪 **Verified end-to-end** | Integration tests convert real WinForms fixture projects, `dotnet build` the result, and **start** it on Avalonia's headless platform — because building alone never catches a converted app that throws before its first window. |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

Generated projects target `net10.0` and reference Avalonia 12.1.1 + CommunityToolkit.Mvvm 8.4.2.
The tool itself has **no Avalonia dependency** — you can run it anywhere the SDK runs.

## Quick start

It installs as a .NET tool called `wf2a`:

```bash
dotnet tool install --global WinFormsToAvalonia

# convert
wf2a convert \
  --source path/to/YourWinFormsApp.csproj \
  --output ./YourAvaloniaApp

# run the result
cd YourAvaloniaApp
dotnet build && dotnet run
```

Not sure what you're dealing with yet? Start read-only:

```bash
wf2a analyze --source path/to/YourWinFormsApp.csproj
```

<details>
<summary>Running from source instead</summary>

```bash
git clone https://github.com/k0zi/WinformsToAvalonia.git
cd WinformsToAvalonia

dotnet run --project src/WinFormsToAvalonia.Cli -- convert \
  --source path/to/YourWinFormsApp.csproj \
  --output ./YourAvaloniaApp
```

Or build and install the tool from your own checkout:

```bash
dotnet pack src/WinFormsToAvalonia.Cli -c Release
dotnet tool install --global --add-source ./artifacts WinFormsToAvalonia
```

</details>

## Usage

| Command | What it does |
|---|---|
| `convert --source <csproj\|sln\|slnx> --output <dir>` | Converts a WinForms project into a new Avalonia project — or, given a solution, every WinForms project in it, into one generated solution, with the `ProjectReference`s between them preserved so a Form can host a UserControl from another project. |
| `analyze --source <csproj\|sln\|slnx>` | Discovery only (nothing written) — lists the Forms/UserControls/Components that would be converted, one table per project. |
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
5. every code-behind helper it calls could move to the ViewModel too — the helper's own
   requirements are merged into the handler's and checked by these same conditions, so the pair
   is promoted together or not at all;
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
to the generated `MigrationTodo.NotMigrated(...)` marker.

<details>
<summary><b>What gets translated today</b></summary>

<br>

**Controls and values** — writes and reads of bindable control properties (`label1.Text`,
`checkBox1.Checked`, `Enabled`, `Visible`, …); the zero- and one-argument control methods with an
exact counterpart (`Focus()`, `Clear()`, `SelectAll()`, `AppendText(x)`); any plain-.NET expression
around them, interpolated strings included.

**Control flow** — `if`/`else` and `foreach`/`for`/`while` when the condition *and the whole body*
translate, all-or-nothing; local variables when their initializer does; `return`.

**The window** — `Close()`/`Show()`/`Hide()`, and the `Form` properties a `Window` spells
differently (`Text` → `Title`, `WindowState`, `TopMost`, …), on this form or on a local holding
another converted one.

**Dialogs** — `MessageBox.Show(...)` via a bundled fallback (which makes the handler `async`);
opening another converted Form (`new SettingsForm().ShowDialog()` →
`await new SettingsView().ShowDialog(this)`), including the `== DialogResult.OK` branch — both
halves are generated, so the converted dialog closes with the matching `Close(true/false)`, whether
its designer declared the result or its handler assigns it; the file dialogs, translated **inline**
into a `StorageProvider` picker call.

**Events and input** — the `EventArgs` members with an exact answer (`e.Cancel`, `e.NewValue`, the
pointer position, the drag effect and payload query); and `sender` on a handler wired to exactly one
control, where the cast disappears because that local provably *is* that control.

**Beyond the UI** — non-visual .NET components (`BackgroundWorker`, `FileSystemWatcher`, `Process`,
…) emitted as real fields with their designer values and events wired; the `DispatcherTimer` this
conversion creates for a WinForms `Timer`; `Application.Exit()`; `Clipboard.SetText`; and **your own
private helper methods and backing fields**, when the helper's *whole* body translates.

</details>

Anything outside that list — control APIs with no Avalonia counterpart (`treeView1.Nodes.Add`),
drawing, `switch`/`try`, an un-promotable helper — stops the translation. **Translation stops at the
first statement it cannot handle**, so the emitted prefix is a faithful partial execution rather
than a method that silently skips work. The full boundary, including why each exclusion is where it
is, lives in [`docs/known-limitations.md`](docs/known-limitations.md).

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
