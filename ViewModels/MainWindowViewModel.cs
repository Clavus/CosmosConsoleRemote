using System;
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
        private const int CONSOLE_UPDATE_INTERVAL_MS = 100;
        
        private readonly CosmosConsole? console;
        
        private string commandString = "";
        public string CommandInput { 
            get => commandString;
            set => this.RaiseAndSetIfChanged(ref commandString, value);
        }

        public ICommand SubmitCommand { get; private set; }
        
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
                CosmosLog.Process();
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