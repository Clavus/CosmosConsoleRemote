using System;
using System.IO;
using Newtonsoft.Json;

namespace CosmosConsoleRemote
{
    public class Settings
    {
        public static Settings? Current;
        
        private const string FILENAME = "settings.json";
        
        public double sizeX = 500;
        public double sizeY = 450;
        
        private Settings() { }
        
        public static void Load()
        {
            if (!File.Exists(FILENAME))
            {
                Current = new Settings();
                return;
            }
            
            try
            {
                string json = File.ReadAllText(FILENAME);
                
                Settings? result = JsonConvert.DeserializeObject<Settings>(json);
                Current = result ?? new Settings();
            }
            catch (Exception e)
            {
                Current = new Settings();
            }
        }

        public static void Save()
        {
            string json = JsonConvert.SerializeObject(Current ?? new Settings());
            File.WriteAllText(FILENAME, json);
        }
        
    }
}