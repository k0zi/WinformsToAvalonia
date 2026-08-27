using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using All_In_One_WinForms.ViewModels;
using All_In_One_WinForms.Views;

namespace All_In_One_WinForms;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }


    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Every generated View sets its own DataContext in its constructor, so the
            // {Binding}s work no matter how the window is opened - not only for the one
            // window App happens to construct here.
            desktop.MainWindow = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}