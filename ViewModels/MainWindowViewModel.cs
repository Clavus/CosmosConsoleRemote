﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Clavusaurus.Cosmos;
using Newtonsoft.Json;
using ReactiveUI;

namespace CosmosConsoleRemote.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public readonly CosmosConsole Console;
        
        private const int CONSOLE_UPDATE_INTERVAL_MS = 100;
        
        private string commandString = "";
        public string CommandInput { 
            get => commandString;
            set => this.RaiseAndSetIfChanged(ref commandString, value);
        }

        public ICommand SubmitCommand { get; private set; }
        
        public MainWindowViewModel()
        {
            CosmosLogger.SetCallbacks(System.Console.WriteLine, System.Console.WriteLine, System.Console.WriteLine);
            
            CommandConfig? config = null;
            try
            {
                string json = File.ReadAllText("commandconfig.json");
                config = JsonConvert.DeserializeObject<CommandConfig>(json);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Exception when deserializing commandconfig.json: " + e);
            }
            
            Console = CosmosConsole.CreateNetworked(config, new LiteNetClientOnlyNetworkFactory());
            Console.SetupBuiltInCommands();
            Console.OnLogEvent += OnConsoleLogEvent;
            Task.Run(ConsoleUpdateLoop);
            
            SubmitCommand = ReactiveCommand.Create(OnSubmitCommand);
        }

        private async void ConsoleUpdateLoop()
        {
            while (true)
            {
                await Task.Delay(CONSOLE_UPDATE_INTERVAL_MS);

                if (Console == null)
                    break;
                
                Console.Update();
                CosmosLog.Process();
            }
        }

        private void OnSubmitCommand()
        {
            if (!string.IsNullOrWhiteSpace(CommandInput))
            {
                Console.Log(CommandInput, LogType.ECHO);
                Console.QueueExecuteString(CommandInput);
                CommandInput = "";
            }
        }
        
        private void OnConsoleLogEvent(string log, LogType logType)
        {
            string line = "";
            
            switch (logType)
            {
                case LogType.DEFAULT:
                    line = log;
                    break;
                case LogType.ERROR:
                    line = $"<color=red>{log}</color>";
                    break;
                case LogType.EXCEPTION:
                    line = $"<color=red>{log}</color>";
                    break;
                case LogType.ECHO:
                    line = $"<color=gray>> {log}</color>";
                    break;
            }
            
            CosmosLog.AddLog(line.TrimEnd('\n').TrimEnd('\r'));
        }
    }
}