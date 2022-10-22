using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Clavusaurus.Cosmos;
using CosmosConsoleRemote.ViewModels;

namespace CosmosConsoleRemote.Views
{
    public partial class CosmosConsoleWindow : Window
    {
        private enum RemoteConnectState
        {
            None, Local, Direct, Connected
        }
        private List<string> history = new List<string>();
        private int historyIndex = 0;

        private CosmosConsoleViewModel viewModel;
        private CommandAutoCompleteProvider autoCompleteProvider;

        private Animation connectionPanelAnimation;
        private bool panelOpened;
        private RemoteConnectState remoteConnectState;
        
        public CosmosConsoleWindow()
        {
            Settings.Load();
            var s = Settings.Current;
            ClientSize = new Size(s.sizeX, s.sizeY);

            InitializeComponent();

            Closed += HandleClosed;

            Dispatcher.UIThread.Post(() =>
            {
                viewModel = ((CosmosConsoleViewModel) DataContext);
                viewModel.Console.OnLogEvent += HandleConsoleLog;
                viewModel.Console.OnLocalCommandsChangedEvent += HandleConfigChanged;
                viewModel.Console.OnRemoteConfigChangedEvent += HandleConfigChanged;
                viewModel.LogParser.OnProcessedTextBlock += HandleProcessedLogReceived;
                viewModel.OnConnectionStatusChanged += HandleViewModelConnectionStatusChanged;
                
                autoCompleteProvider = new CommandAutoCompleteProvider(viewModel.Console.GetCommandHints());
                CommandInputBox_OnTextChanged(null, null);
            });

            connectionPanelAnimation = new Animation()
            {
                Duration = TimeSpan.FromSeconds(.2),
                Children =
                {
                    new KeyFrame()
                    {
                        Setters = {new Setter(WidthProperty, 30d)},
                        KeyTime = TimeSpan.FromSeconds(0)
                    },
                    new KeyFrame()
                    {
                        Setters = {new Setter(WidthProperty, 280d)},
                        KeyTime = TimeSpan.FromSeconds(.2)
                    }
                },
                Easing = new CubicEaseOut(),
                FillMode = FillMode.Forward,
                PlaybackDirection = PlaybackDirection.Normal,
            };
        }

        private void HandleViewModelConnectionStatusChanged()
        {
            remoteConnectState = viewModel.IsConnected ? RemoteConnectState.Connected : RemoteConnectState.None;
            Dispatcher.UIThread.Post(UpdateNetworkUIControls);
        }

        private void HandleConfigChanged()
        {
            autoCompleteProvider = new CommandAutoCompleteProvider(viewModel.Console.GetCommandHints());
        }

        private void HandleProcessedLogReceived(RichTextBlock richText)
        {
            richText.IsTextSelectionEnabled = false;
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
                case Key.PageDown:
                    MoveHistoryDown();
                    break;
                case Key.PageUp:
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
            
            TextBox box = ((CommandInputBox.GetVisualChildren().First() as Grid).GetVisualChildren().First() as TextBox);
            box.CaretIndex = box.Text.Length;
        }

        private void MoveHistoryDown()
        {
            if (history.Count == 0)
                return;
        
            if (historyIndex < history.Count - 1)
                historyIndex++;
            
            viewModel.CommandInput = history[historyIndex];
            
            TextBox box = ((CommandInputBox.GetVisualChildren().First() as Grid).GetVisualChildren().First() as TextBox);
            box.CaretIndex = box.Text.Length;
        }

        private void CommandInputBox_OnTextChanged(object? sender, EventArgs e)
        {
            if (autoCompleteProvider == null || (string?) CommandInputBox.SelectedItem == CommandInputBox.Text)
                return;
            
            if (autoCompleteProvider.HasUpdatedSuggestions(CommandInputBox.Text, out string commandFormatHint, out List<string> suggestions))
            {
                CommandInputBox.Items = suggestions;
            }
        }

        private void ConnectionPanelButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                panelOpened = !panelOpened;
                connectionPanelAnimation.PlaybackDirection = (panelOpened ? PlaybackDirection.Normal : PlaybackDirection.Reverse);
                await connectionPanelAnimation.RunAsync(ConnectionPanel, null);
            });
        }

        private void NetworkSelectLocal_OnClick(object? sender, RoutedEventArgs e)
        {
            remoteConnectState = RemoteConnectState.Local;
            UpdateNetworkUIControls();
            LanRefresh_OnClick(null, null);
        }

        private void NetworkSelectDirect_OnClick(object? sender, RoutedEventArgs e)
        {
            remoteConnectState = RemoteConnectState.Direct;
            UpdateNetworkUIControls();
        }

        private void NetworkBackButton_OnClick(object? sender, RoutedEventArgs e)
        {
            remoteConnectState = RemoteConnectState.None;
            UpdateNetworkUIControls();
        }
        
        private void UpdateNetworkUIControls()
        {
            ConnectionTypePanel.IsVisible = (remoteConnectState == RemoteConnectState.None);
            ConnectionLocalIPPanel.IsVisible = (remoteConnectState == RemoteConnectState.Local);
            ConnectionDirectIPPanel.IsVisible = (remoteConnectState == RemoteConnectState.Direct);
            ConnectionConnectedPanel.IsVisible = (remoteConnectState == RemoteConnectState.Connected);
        }

        private void LanRefresh_OnClick(object? sender, RoutedEventArgs e)
        {
            viewModel.AvailableLanAddresses.Clear();
            Network.RefreshLocalServerListing(viewModel.Console, viewModel.Console.RemotePort, 3000);
        }
        
        private void ClearButton_OnClick(object? sender, RoutedEventArgs e)
        {
            LogStackPanel.Children.Clear();
        }
    }
}