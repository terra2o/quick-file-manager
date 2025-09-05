using System;

namespace QuickFileManager
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();
            IFileService fileService = new FileSystemFileService(logger);
            var manager = new FileManager(fileService, logger);

            var ui = new ConsoleUI(manager, logger);
            ui.RunWithHotkeys();
        }
    }
}