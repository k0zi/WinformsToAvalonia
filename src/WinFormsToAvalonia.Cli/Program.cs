using Spectre.Console.Cli;
using WinFormsToAvalonia.Cli.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    // The name the tool is installed under, so every usage line and example in --help reads the
    // way the user will actually type it.
    config.SetApplicationName("wf2a");

    config.AddCommand<ConvertCommand>("convert")
        .WithDescription("Convert a WinForms project (.csproj) into a new Avalonia application project.")
        .WithExample("convert", "--source", "MyWinFormsApp.csproj", "--output", "./MyAvaloniaApp");

    config.AddCommand<AnalyzeCommand>("analyze")
        .WithDescription("Run discovery only (no output written) and print what would be converted.")
        .WithExample("analyze", "--source", "MyWinFormsApp.csproj");

    config.AddCommand<ListMappingsCommand>("list-mappings")
        .WithDescription("Print the full WinForms -> Avalonia control mapping table.")
        .WithExample("list-mappings")
        .WithExample("list-mappings", "--filter", "Box");
});

return app.Run(args);
