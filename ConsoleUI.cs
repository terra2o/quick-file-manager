using System;
using System.Collections.Generic;

namespace QuickFileManager
{
    public class ConsoleUI
    {
        private readonly FileManager _manager;
        private readonly ILogger _logger;
        private bool _exitRequested = false;

        public ConsoleUI(FileManager manager, ILogger logger)
        {
            _manager = manager;
            _logger = logger;
        }

        // Main loop: prints menu and waits for Ctrl+Key hotkeys
        public void RunWithHotkeys()
        {
            _exitRequested = false;

            Console.Clear(); // clear once at start
            PrintMenu();     // print the static menu at the top

            while (!_exitRequested)
            {
                // read key without echo
                var keyInfo = Console.ReadKey(intercept: true);
                HandleHotkey(keyInfo);

                // leave space between actions
                Console.WriteLine();
            }
        }

        // Display the menu and hotkey instructions
        private void PrintMenu()
        {
            Console.Clear();
            Console.WriteLine("=== File Manager ===");
            Console.WriteLine("Hotkeys (Ctrl + Key):");
            Console.WriteLine("Ctrl+N = Create new file");
            Console.WriteLine("Ctrl+R = Read file");
            Console.WriteLine("Ctrl+A = Append text to file");
            Console.WriteLine("Ctrl+D = Delete file");
            Console.WriteLine("Ctrl+L = List files in directory");
            Console.WriteLine("Ctrl+S = Search for text inside file");
            Console.WriteLine("Ctrl+E = Exit");
            Console.WriteLine("Press a hotkey combination...");
            Console.WriteLine("------------------------");

        }

        // Maps Ctrl+Key presses to actions
        private void HandleHotkey(ConsoleKeyInfo keyInfo)
        {
            // Only respond to Ctrl+Key
            if (!keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                Console.WriteLine("Please use Ctrl + a key for actions.");
                return;
            }

            switch (keyInfo.Key)
            {
                case ConsoleKey.N: CreateNewFile(); break;
                case ConsoleKey.R: ReadFile(); break;
                case ConsoleKey.A: AppendToFile(); break;
                case ConsoleKey.D: DeleteFile(); break;
                case ConsoleKey.L: ListFiles(); break;
                case ConsoleKey.S: SearchInFile(); break;
                case ConsoleKey.E: _exitRequested = true; break;
                default: Console.WriteLine("Unknown hotkey."); break;
            }

            if (!_exitRequested)
            {
                Console.WriteLine("Done. Waiting for next command...");
            }
        }

        // --- File operations below ---

        private void CreateNewFile()
        {
            Console.Write("Enter file path to create: ");
            var path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path)) { Console.WriteLine("Path cannot be empty."); return; }

            _manager.CreateFile(path);
            Console.WriteLine("File created (or already existed).");
        }

        private void ReadFile()
        {
            Console.Write("Enter file path to read: ");
            var path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path)) { Console.WriteLine("Path cannot be empty."); return; }

            var content = _manager.ReadFile(path);
            Console.WriteLine("--- File contents start ---");
            Console.WriteLine(content);
            Console.WriteLine("--- File contents end ---");
        }

        private void AppendToFile()
        {
            Console.Write("Enter file path to append to: ");
            var path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path)) { Console.WriteLine("Path cannot be empty."); return; }

            Console.WriteLine("Enter text to append. Finish with a single line containing only '::end'");

            var lines = new List<string>();

            string line;
            while ((line = Console.ReadLine() ?? "") != "")
            {
                if (line == "::end") break;
                lines.Add(line);
            }


            try
            {
                _manager.AppendToFile(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
                Console.WriteLine("Append complete.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to append to file: {ex.Message}");
            }
        }


        private void DeleteFile()
        {
            Console.Write("Enter file path to delete: ");
            var path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path)) { Console.WriteLine("Path cannot be empty."); return; }

            Console.Write("Are you sure? (y/N): ");
            var ans = Console.ReadLine();
            if (ans?.Trim().ToLower() == "y")
            {
                _manager.DeleteFile(path);
                Console.WriteLine("File deleted (if it existed).");
            }
            else Console.WriteLine("Delete cancelled.");
        }

        private void ListFiles()
        {
            Console.Write("Enter directory path (leave empty for current directory): ");
            var dir = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;

            var files = _manager.ListFiles(dir);
            Console.WriteLine($"Files in {dir}:");
            foreach (var f in files)
            {
                Console.WriteLine(f);
            }
        }

        private void SearchInFile()
        {
            Console.Write("Enter file path to search: ");
            var path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path)) { Console.WriteLine("Path cannot be empty."); return; }

            Console.Write("Enter search text: ");
            var term = Console.ReadLine() ?? string.Empty;

            var results = _manager.SearchInFile(path, term);
            Console.WriteLine($"Found {results.Count} matching lines:");
            foreach (var r in results)
            {
                Console.WriteLine(r);
            }
        }
    }
}
