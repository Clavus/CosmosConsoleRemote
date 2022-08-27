using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CosmosConsoleRemote.ViewModels;
using CosmosConsoleRemote.Views;

namespace CosmosConsoleRemote
{
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
                desktop.MainWindow = new CosmosConsoleWindow
                {
                    DataContext = new CosmosConsoleViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}