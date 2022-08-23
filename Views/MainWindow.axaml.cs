using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Clavusaurus.Cosmos;

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

            CosmosLog.OnProcessedLog += HandleProcessedLogReceived;
            Closed += HandleClosed;
        }

        private void HandleProcessedLogReceived(InlineCollection collection)
        {
            RichTextBlock richText = new ()
            {
                Inlines = collection,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 1
            };
            
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
    }
}