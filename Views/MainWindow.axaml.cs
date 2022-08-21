using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

            Closed += HandleClosed;
        }

        private void HandleClosed(object? sender, EventArgs e)
        {
            Settings.Current.sizeX = ClientSize.Width;
            Settings.Current.sizeY = ClientSize.Height;
            Settings.Save();
            
        }
    }
}