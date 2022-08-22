using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Clavusaurus.Cosmos;
using Newtonsoft.Json;
using ReactiveUI;

namespace CosmosConsoleRemote.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private const int CONSOLE_UPDATE_INTERVAL_MS = 100;
        
        private readonly CosmosConsole? console;
        private StringBuilder outputBuilder = new StringBuilder();
        
        private string commandString = "";
        public string CommandInput { 
            get => commandString;
            set => this.RaiseAndSetIfChanged(ref commandString, value);
        }

        public ICommand SubmitCommand { get; private set; }

        private string outputString = "";
        public string Output { 
            get => outputString;
            set => this.RaiseAndSetIfChanged(ref outputString, value);
        }
        
        public MainWindowViewModel()
        {
            CosmosLogger.SetCallbacks(Console.WriteLine, Console.WriteLine, Console.WriteLine);
            
            try
            {
                string json = File.ReadAllText("commandconfig.json");
                Console.WriteLine("Read json: " + json);
                CommandConfig? config = JsonConvert.DeserializeObject<CommandConfig>(json);
                
                Console.WriteLine("Config: " + config);
                Console.WriteLine("NumCommands: " + config?.commandList.Count);
                Console.WriteLine("NumUsers: " + config?.users.Count);
                console = CosmosConsole.CreateNetworked(config, new LiteNetClientOnlyNetworkFactory());
                console.SetupBuiltInCommands();
                console.OnLogEvent += OnConsoleLogEvent;

                Task.Run(ConsoleUpdateLoop);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e);
            }
            
            SubmitCommand = ReactiveCommand.Create(OnSubmitCommand);
        }

        private async void ConsoleUpdateLoop()
        {
            while (true)
            {
                await Task.Delay(CONSOLE_UPDATE_INTERVAL_MS);

                if (console == null)
                    break;
                
                console.Update();
            }
        }

        private void OnSubmitCommand()
        {
            if (!string.IsNullOrWhiteSpace(CommandInput))
            {
                console.Log(CommandInput, LogType.ECHO);
                console.QueueExecuteString(CommandInput);
                CommandInput = "";
                
                Console.WriteLine("Submit command: " + CommandInput);
            }
        }
        
        private void OnConsoleLogEvent(string log, LogType logType)
        {
            switch (logType)
            {
                case LogType.DEFAULT:
                    outputBuilder.AppendLine(ParseForAvalonia($"{log}"));
                    break;
                case LogType.ERROR:
                    outputBuilder.AppendLine($"<color=red>{log}</color>");
                    break;
                case LogType.EXCEPTION:
                    outputBuilder.AppendLine($"<color=red>{log}</color>");
                    break;
                case LogType.ECHO:
                    outputBuilder.AppendLine($"> {log}");
                    break;
            }

            Output = outputBuilder.ToString();
        }

        private string ParseForAvalonia(string input)
        {
            return input.Replace("\t", "    ");
        }
    }
}