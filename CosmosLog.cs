
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;
using Avalonia.Threading;

namespace CosmosConsoleRemote
{
    public static class CosmosLog
    {
        public static event Action<InlineCollection> OnProcessedLog;

        private static ConcurrentQueue<string> textToProcess = new ();
        
        public static void AddLog(string text) => textToProcess.Enqueue(text);

        public static async void Process()
        {
            while (textToProcess.TryDequeue(out string? text))
                await ProcessText(text);
        }

        private static Task ProcessText(string text)
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
            {
                InlineCollection collection = new();
                foreach(string t in text.Split('\n'))
                    collection.Add(t);
                
                OnProcessedLog.Invoke(collection);
            });
        }
    }
}