using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Clavusaurus.Cosmos;
using CosmosConsoleRemote.ViewModels;

namespace CosmosConsoleRemote.Views
{
    public partial class MainWindow : Window
    {
        private List<string> history = new List<string>();
        private int historyIndex = 0;

        private MainWindowViewModel viewModel;
        
        public MainWindow()
        {
            Settings.Load();
            var s = Settings.Current;
            ClientSize = new Size(s.sizeX, s.sizeY);

            InitializeComponent();

            CosmosLog.OnProcessedTextBlock += HandleProcessedLogReceived;
            Closed += HandleClosed;
            
            Dispatcher.UIThread.Post(() =>
            {
                viewModel = ((MainWindowViewModel) DataContext);
                viewModel.Console.OnLogEvent += HandleConsoleLog;
            });
        }

        private void HandleProcessedLogReceived(RichTextBlock richText)
        {
            LogStackPanel.Children.Add(richText);
            ScrollLogToEnd();
        }
        
        private void HandleConsoleLog(string log, LogType logType)
        {
            if (logType == LogType.ECHO)
            {
                if (history.Count == 0 || history.Last() != log)
                {
                    history.Add(log);
                    historyIndex = history.Count;
                }
            }
        }
        
        private void HandleClosed(object? sender, EventArgs e)
        {
            Settings.Current.sizeX = ClientSize.Width;
            Settings.Current.sizeY = ClientSize.Height;
            Settings.Save();
        }

        private void Input_OnKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Return:
                    viewModel.SubmitCommand.Execute(null);
                    break;
                case Key.Down:
                    MoveHistoryDown();
                    break;
                case Key.Up:
                    MoveHistoryUp();
                    break;
            }
        }

        private void ScrollLogToEnd()
        {
            // Delay scroll till after layout update
            Dispatcher.UIThread.Post(() =>
            {
                LogScrollView.ScrollToEnd();
            });
        }
        
        private void MoveHistoryUp()
        {
            if (history.Count == 0)
                return;

            if (historyIndex > 0)
                historyIndex--;
            
            viewModel.CommandInput = history[historyIndex];
            CommandInputBox.CaretIndex = viewModel.CommandInput.Length;
        }

        private void MoveHistoryDown()
        {
            if (history.Count == 0)
                return;
        
            if (historyIndex < history.Count - 1)
                historyIndex++;
            
            viewModel.CommandInput = history[historyIndex];
            CommandInputBox.CaretIndex = viewModel.CommandInput.Length;
        }
    }
}