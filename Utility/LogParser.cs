
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace CosmosConsoleRemote
{
    public class LogParser
    {
        public event Action<RichTextBlock> OnProcessedTextBlock;

        private ConcurrentQueue<string> textToProcess = new ();
        
        public void AddLog(string text)
        {
            textToProcess.Enqueue(text);
        }

        public async void Process()
        {
            while (textToProcess.TryDequeue(out string? text))
                await ProcessText(text);
        }

        private Task ProcessText(string text)
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
            {
                string axamlStringStart = @"<RichTextBlock xml:space='preserve' xmlns='https://github.com/avaloniaui' TextWrapping='Wrap'>";
                string axamlStringEnd = @"</RichTextBlock>";
                
                RichTextBlock richText;
                
                try
                {
                    richText = AvaloniaRuntimeXamlLoader.Parse<RichTextBlock>(axamlStringStart + ParseUnityRichTextToAxaml(text) + axamlStringEnd);
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to parse text, exception: {e.Message}");
                    richText = new RichTextBlock()
                    {
                        Text = text,
                        TextWrapping = TextWrapping.Wrap
                    };
                }
                
                OnProcessedTextBlock.Invoke(richText);

            });
        }

        private static string ParseUnityRichTextToAxaml(string text)
        {
            int tagOpenIndex = text.IndexOf("<"); // Find first < tag
            while (tagOpenIndex != -1)
            {
                int tagCloseIndex = text.IndexOf(">", tagOpenIndex); // Find end of tag

                if (tagOpenIndex < tagCloseIndex - 1) // It's not just "<>"
                {
                    string subString = text.Substring(tagOpenIndex + 1, tagCloseIndex - tagOpenIndex - 1); // Grab text between < > tag chars
                    subString = subString.Replace(" ", "").ToLowerInvariant(); // Remove any empty spaces to make comparing easier

                    if (subString.Contains('='))
                        subString = ParseValueTag(subString); // replace <param=value> type tag
                    else
                        subString = ParseTag(subString); // replace <param> (or </param>) type tag

                    if (!string.IsNullOrWhiteSpace(subString)) // If we have a valid replacement
                        text = text.Remove(tagOpenIndex, (tagCloseIndex - tagOpenIndex) + 1).Insert(tagOpenIndex, subString); // Remove old tag, insert new one
                    else // We don't have a replacement but we still need to replace the < > chars with valid codes so axaml parse doesn't break
                        text = text.Remove(tagCloseIndex, 1).Insert(tagCloseIndex, "&gt;").Remove(tagOpenIndex, 1).Insert(tagOpenIndex, "&lt;");
                }
                else
                {
                    // Replace the < > chars with valid codes so axaml parse doesn't break
                    if (tagCloseIndex != -1)
                        text = text.Remove(tagCloseIndex, 1).Insert(tagCloseIndex, "&gt;");
                        
                    text = text.Remove(tagOpenIndex, 1).Insert(tagOpenIndex, "&lt;");
                }
                
                tagOpenIndex = text.IndexOf("<", tagOpenIndex + 1); // Find next < tag
            }
            
            text = text.Replace(Environment.NewLine, "<LineBreak/>");
            return text;
        }

        private static string ParseValueTag(string text)
        {
            string[] split = text.Split('=');
            if (split.Length != 2)
                return "";

            switch (split[0])
            {
                case "color": return $"<Span Foreground='{split[1]}'>";
                case "size": return $"<Span FontSize='{split[1]}'>";
                default: return "";
            }
        }

        private static string ParseTag(string text)
        {
            switch (text)
            {
                case "b" : return "<Bold>";
                case "/b": return "</Bold>";
                case "i": return "<Italic>";
                case "/i": return "</Italic>";
                case "/color": return "</Span>";
                case "/size": return "</Span>";
                default: return "";
            }
        }
    }
}