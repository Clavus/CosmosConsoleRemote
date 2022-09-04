using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using Clavusaurus.Cosmos;
using Newtonsoft.Json;
using ReactiveUI;

namespace CosmosConsoleRemote.ViewModels
{
    public class CosmosConsoleViewModel : ViewModelBase
    {
        public event Action OnConnectionStatusChanged;
        
        public readonly CosmosConsole Console;
        public readonly CommandConfig Config;
        public readonly LogParser LogParser;
        
        private string commandString = "";
        public string CommandInput { 
            get => commandString;
            set => this.RaiseAndSetIfChanged(ref commandString, value);
        }

        private string connectionPanelTitle = "Remote: Not Connected";
        public string ConnectionPanelTitle
        {
            get => connectionPanelTitle;
            set => this.RaiseAndSetIfChanged(ref connectionPanelTitle, value);
        }
        
        private string addressInput = "";
        public string AddressInput
        {
            get => addressInput;
            set => this.RaiseAndSetIfChanged(ref addressInput, value);
        }
        
        public IPEndPoint selectedLanAddress = new IPEndPoint(0,0);
        public IPEndPoint SelectedLanAddress
        {
            get => selectedLanAddress;
            set => this.RaiseAndSetIfChanged(ref selectedLanAddress, value);
        }

        public ObservableCollection<IPEndPoint> AvailableLanAddresses { get; } = new();

        private string connectedAddressText;
        public string ConnectedAddressText
        {
            get => connectedAddressText;
            set => this.RaiseAndSetIfChanged(ref connectedAddressText, value);
        }
        
        public ICommand SubmitCommand { get; }
        public ICommand ConnectLocalCommand { get; }
        public ICommand ConnectDirectCommand { get; }
        public ICommand DisconnectCommand { get; }

        private bool isConnected;
        public bool IsConnected
        {
            get => isConnected;
            private set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnConnectionStatusChanged?.Invoke();
                }
            }
        }
        
        private const int CONSOLE_UPDATE_INTERVAL_MS = 100;
        
        private const string REMOTE_ATTEMPT_CONNECT = "Remote: Attempting Connection";
        private const string REMOTE_NOT_CONNECTED = "Remote: Not Connected";
        private const string REMOTE_CONNECTED = "Remote: Connected";
        
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
            Console.OnLogEvent += HandleConsoleLogEvent;
            Network.OnLocalServerFound += HandleLocalServerFound;

            Config = config;
            Task.Run(ConsoleUpdateLoop);
            
            SubmitCommand = ReactiveCommand.Create(OnSubmitCommand);
            ConnectLocalCommand = ReactiveCommand.Create(OnConnectLocalCommand);
            ConnectDirectCommand = ReactiveCommand.Create(OnConnectDirectCommand);
            DisconnectCommand = ReactiveCommand.Create(OnDisconnectCommand);
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

                if (IsConnected && (Console.Networker.NetworkMode != NetworkMode.CLIENT || !Console.Networker.Client.IsConnected))
                {
                    IsConnected = false;
                    ConnectionPanelTitle = REMOTE_NOT_CONNECTED;
                }
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
            ConnectTo(SelectedLanAddress);
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
                ConnectTo(endPoint);
            else
                Console.Log($"Invalid host / IP \"{addressInput}\"");
        }

        private async void ConnectTo(IPEndPoint endPoint)
        {
            ConnectionPanelTitle = REMOTE_ATTEMPT_CONNECT;
                
            await CosmosUtility.ConnectClientToServerRoutine(Console, endPoint, new UserCredentials());

            IsConnected = (Console.Networker.NetworkMode == NetworkMode.CLIENT && Console.Networker.Client.IsConnected);
            if (IsConnected)
                ConnectedAddressText = $"Connected to: {Console.Networker.Client.ServerAddress}";
            
            ConnectionPanelTitle = isConnected ? REMOTE_CONNECTED : REMOTE_NOT_CONNECTED;
        }
        
        private void OnDisconnectCommand()
        {
            if (Console.Networker.NetworkMode == NetworkMode.CLIENT)
                Console.Log("Disconnecting Cosmos Console network client from server");

            Console.Networker.StopClient();
            ConnectionPanelTitle = REMOTE_NOT_CONNECTED;
        }

        private void HandleLocalServerFound(IPEndPoint endPoint)
        {
            System.Console.WriteLine($"Found local IP: {endPoint}");
            AvailableLanAddresses.Add(endPoint);

            if (AvailableLanAddresses.Count == 1)
                SelectedLanAddress = AvailableLanAddresses[0];
        }
        
        private void HandleConsoleLogEvent(string log, LogType logType)
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