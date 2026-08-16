# Winforms2Avalonia

A .NET 10 command-line tool that converts a Windows Forms application (.NET Framework or
modern .NET) into a new Avalonia UI application: every `Form` becomes an Avalonia `Window`
View + ViewModel pair (CommunityToolkit.Mvvm) and every `UserControl` an Avalonia `UserControl`
one, WinForms controls are mapped to their closest Avalonia equivalent (or one of the tool's
own bundled fallback controls when Avalonia has no built-in match), and the original
code-behind is preserved - commented out - for manual migration.

## Quick start

```bash
dotnet run --project src/Winforms2Avalonia.Cli -- convert \
  --source path/to/YourWinFormsApp.csproj \
  --output ./YourAvaloniaApp

cd YourAvaloniaApp
dotnet build && dotnet run
```

## Commands

| Command | What it does |
|---|---|
| `convert --source <csproj> --output <dir>` | Converts a WinForms project into a new Avalonia project. |
| `analyze --source <csproj>` | Discovery only (no output written) - lists the Forms/UserControls/Components that would be converted. |
| `list-mappings [--filter <substring>]` | Prints the full WinForms → Avalonia control mapping table. |

`convert` options: `--force` (overwrite a non-empty output directory), `--dry-run` (run the
full pipeline, print the report, write nothing), `--verbose` (show every warning, not just
the first few), `--no-fallback-controls` (strict mode: skip fallback controls instead of
emitting them), `--skip-code-behind-comments` (omit the preserved code-behind block),
`--log-file <path>` (write the conversion report as JSON alongside the console output).

## Design decisions

- **Layout**: every container becomes an Avalonia `Canvas`, children keep their original
  WinForms `Location`/`Size` as `Canvas.Left`/`Canvas.Top`/`Width`/`Height` - pixel-accurate
  and fully deterministic. `Anchor`/`Dock` are preserved (both as an XML comment and a
  `w2a:LayoutHint` attached property) for later manual responsive-layout work, never
  auto-translated.
- **Fallback controls**: WinForms controls with no built-in Avalonia equivalent (`GroupBox`,
  `StatusStrip`, `ToolStrip`, `MaskedTextBox`, `RichTextBox`, `ErrorProvider`, `PropertyGrid`,
  `BindingNavigator`, `WebBrowser`, `PrintPreviewControl`) are backed by a fixed set of controls
  the tool ships with itself; only the ones actually used get copied into the generated
  project's `Controls/` folder, with no extra NuGet dependency.
- **Code-behind**: see the next section. Every event handler comes across as a real,
  subscribed method on the generated View, with its original body preserved inside it; only
  handlers that provably *can* be commands become `[RelayCommand]`s on the ViewModel.

## Code-behind migration

WinForms handler bodies are dominated by imperative control-API code -
`treeView1.Nodes.Add(...)`, `e.Effect = DragDropEffects.Copy`, `errorProvider.SetError(...)`,
`new DashboardForm().ShowDialog()`. That code addresses controls by field name, which Avalonia
*code-behind* can do unchanged (`x:Name` generates a field), but a ViewModel cannot. So:

> **Code-behind is the default target. Promotion to the ViewModel is the exception, and it
> happens only when a Roslyn analysis of the handler's own body proves it is possible.**

A handler becomes a `[RelayCommand]` (plus `Command="{Binding …}"` on its control) only when
**all** of these hold:

1. it is a `Click` on a control that maps to a real Avalonia `Button`/`MenuItem`;
2. it is wired to exactly one control (a shared handler needs `sender`);
3. its body uses neither `sender` nor the `EventArgs`;
4. it drives no Form member (`Close()`, `Hide()`, `DialogResult`, ...) and opens no other Form;
5. it calls no code-behind helper method;
6. every control member it touches is a two-way bindable value property (`Text`, `Checked`,
   `Value`, `SelectedItem`, `Enabled`, `Visible`) on a directly-mapped control.

Everything else stays event-driven, exactly as in WinForms: mouse/keyboard/drag-drop handlers,
control-API code, form lifecycle (`Load` → `Loaded`, `FormClosing` → `Closing`), navigation, and
anything the analyzer cannot classify with confidence. Each such decision - and its reason - is
reported in the conversion output.

Handler *bodies* are never re-emitted as compiling code (WinForms APIs do not exist in
Avalonia). Each generated method has the correct Avalonia signature and subscription, with the
original body preserved inside it as a comment, followed by a call to the generated
`MigrationTodo.NotMigrated(...)` marker. The generated project therefore always builds *and
runs*, and the unit of manual migration is one method rather than one file-sized comment block.

The marker reports (to stderr and to the debugger, once per member) rather than throwing,
because Avalonia invokes these handlers from the framework - a `TabControl` selects its first
tab, a `Window` raises `Loaded` - so a throwing stub used to kill the converted app during XAML
initialization, before its first window appeared. Set
`MigrationTodo.ThrowOnUnmigratedCall = true` (e.g. in a smoke test) to make un-migrated code
fail loudly again, and read `MigrationTodo.ReportedMembers` to see what ran un-migrated.

See `docs/known-limitations.md` for what isn't handled yet, and `docs/Controls.md` for the
per-type mapping status.

## Solution structure

```
src/
  Winforms2Avalonia.Cli/              Spectre.Console.Cli command-line host
  Winforms2Avalonia.Core/             parsing, mapping, emission, scaffolding (no Avalonia dependency)
  Winforms2Avalonia.FallbackControls/ bundled fallback control templates (embedded resources)
tests/
  Winforms2Avalonia.Core.Tests/       unit tests (parsing, mapping, emission)
  Winforms2Avalonia.Integration.Tests/ end-to-end: converts real WinForms fixture projects and
                                        `dotnet build`s the generated output
docs/
  Controls.md            per-type WinForms → Avalonia mapping status + implementation plan
  known-limitations.md
```

## Requirements

.NET 10 SDK. The generated Avalonia projects target `net10.0` and reference Avalonia
12.1.1 + CommunityToolkit.Mvvm 8.4.2.
