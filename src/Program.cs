using System;

namespace QuickFileManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var config = ConfigLoader.Load();

            logger.Info($"Loaded {config.Hotkeys?.Count} hotkeys");
            logger.Info($"Default directory: {config.DefaultDirectory}");
    
            var fileService = new FileSystemFileService(logger);
            var manager = new FileManager(fileService, logger, config);

            // Pass config to UI
            var ui = new ConsoleUI(manager, logger, config);
            ui.RunWithHotkeys();
        }
    }
}