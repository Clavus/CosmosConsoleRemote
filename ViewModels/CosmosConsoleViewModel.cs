using System;
using System.IO;
using System.Net;
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
    public class CosmosConsoleViewModel : ViewModelBase
    {
        public readonly CosmosConsole Console;
        public readonly CommandConfig Config;
        public readonly LogParser LogParser;
        
        private const int CONSOLE_UPDATE_INTERVAL_MS = 100;
        
        private string commandString = "";
        public string CommandInput { 
            get => commandString;
            set => this.RaiseAndSetIfChanged(ref commandString, value);
        }

        private string addressInput = "";
        public string AddressInput
        {
            get => addressInput;
            set => this.RaiseAndSetIfChanged(ref addressInput, value);
        }
        
        public ICommand SubmitCommand { get; }
        public ICommand ConnectLocalCommand { get; }
        public ICommand ConnectDirectCommand { get; }

        public CosmosConsoleViewModel()
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
                config = new CommandConfig();
            }

            LogParser = new LogParser();
            
            Console = CosmosConsole.CreateNetworked(config, new LiteNetClientOnlyNetworkFactory());
            Console.SetupBuiltInCommands();
            Console.OnLogEvent += OnConsoleLogEvent;

            Config = config;
            Task.Run(ConsoleUpdateLoop);
            
            SubmitCommand = ReactiveCommand.Create(OnSubmitCommand);
            ConnectLocalCommand = ReactiveCommand.Create(OnConnectLocalCommand);
            ConnectDirectCommand = ReactiveCommand.Create(OnConnectDirectCommand);
        }
        
        private async void ConsoleUpdateLoop()
        {
            while (true)
            {
                await Task.Delay(CONSOLE_UPDATE_INTERVAL_MS);

                if (Console == null)
                    break;
                
                Console.Update();
                LogParser.Process();
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
        
        private void OnConnectLocalCommand()
        {
            
        }
        
        private void OnConnectDirectCommand()
        {
            string hostToParse = addressInput;

            ushort port = Console.RemotePort;
            string[] hostSplit = hostToParse.Split(':'); // Allow for "123.4.5.6:7890" host string format to add port
                        
            if (hostSplit.Length > 1 && ushort.TryParse(hostSplit[1], out ushort parsedPort))
            {
                port = parsedPort;
                hostToParse = hostSplit[0];
            }
                        
            if (Network.ResolveHost(hostToParse, port, out IPEndPoint endPoint)) 
                CosmosUtility.ConnectClientToServerRoutine(Console, endPoint, new UserCredentials());
            else
                Console.Log($"Invalid host / IP \"{addressInput}\"");
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
            
            LogParser.AddLog(line.TrimEnd('\n').TrimEnd('\r'));
        }
    }
}