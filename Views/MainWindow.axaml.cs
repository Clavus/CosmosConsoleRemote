using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CosmosConsoleRemote.ViewModels;

namespace CosmosConsoleRemote.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Settings.Load();
            var s = Settings.Current;
            ClientSize = new Size(s.sizeX, s.sizeY);

            InitializeComponent();

            CosmosLog.OnProcessedTextBlock += HandleProcessedLogReceived;
            Closed += HandleClosed;
        }

        private void HandleProcessedLogReceived(RichTextBlock richText)
        {
            LogStackPanel.Children.Add(richText);
            
            // Delay scroll till after layout update
            Dispatcher.UIThread.Post(() =>
            {
                LogScrollView.ScrollToEnd();
            });
        }
        
        private void HandleClosed(object? sender, EventArgs e)
        {
            Settings.Current.sizeX = ClientSize.Width;
            Settings.Current.sizeY = ClientSize.Height;
            Settings.Save();
        }

        private void Input_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                ((MainWindowViewModel) DataContext).SubmitCommand.Execute(null);
        }
    }
}