using System;
using System.IO;
using System.Text.Json;

namespace QuickFileManager
{
    public static class ConfigLoader
    {
        private static readonly string ConfigFile = GetConfigPath();

        private static string GetConfigPath()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                return Path.Combine(home, ".config", "qfm", "config.json");
            else if (OperatingSystem.IsWindows())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "qfm", "config.json");

            // Fallback
            return "config.json";
        }


        public static Config Load()
        {
            try
            {
                if (!File.Exists(ConfigFile))
                {
                    Console.WriteLine("No config.json found, using defaults.");
                    return new Config();
                }
                var json = File.ReadAllText(ConfigFile);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var config = JsonSerializer.Deserialize<Config>(json, options);

                if (config == null)
                {
                    Console.WriteLine("Failed to parse config.json, using defaults.");
                    return new Config();
                }

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                return new Config();
            }
        }
    }
}

