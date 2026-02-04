using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace QuickFileManager
{
    public class ConsoleUI
    {
        private readonly FileManager _manager;
        private readonly ILogger _logger;
        private readonly Config _config;

        private bool _exitRequested = false;
        private int _currentBookmarkIndex = 0;

        // Layout
        private int _totalWidth;
        private int _totalHeight;
        private int _hotkeyWidth;   // Left pane
        private int _logWidth;      // Middle pane
        private int _fileWidth;     // Right pane
        private int _divider1X;     // Between Hotkeys and Logs
        private int _divider2X;     // Between Logs and Files

        // Left pane buffer
        private readonly List<string> _leftBuffer = new();
        private const int LeftBufferMaxLines = 2000;

        // Directory entries for right pane
        private List<string> _entries = new();
        private int _selectedIndex = 0;
        private int _scrollOffset = 0;

        // Hotkeys
        private readonly Dictionary<string, Action> _hotkeyActions = new(StringComparer.OrdinalIgnoreCase);
        private string _previousDirectory = Environment.CurrentDirectory;

        public ConsoleUI(FileManager manager, ILogger logger, Config config)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _logger = logger;
            _config = config ?? new Config();

            UpdateLayout();
            InitializeHotkeys();
            LoadDirectoryEntries(Environment.CurrentDirectory);
        }

        // ---------- Layout & drawing ----------

        private void UpdateLayout()
        {
            _totalWidth = Math.Max(60, Console.WindowWidth);
            _totalHeight = Math.Max(10, Console.WindowHeight);

            // Calculate widths: 20% Hotkeys, 40% Logs, 40% Files (approx)
            _hotkeyWidth = (int)(_totalWidth * 0.20);
            _fileWidth = (int)(_totalWidth * 0.35);
            _logWidth = _totalWidth - _hotkeyWidth - _fileWidth - 2; // -2 for the two dividers

            _divider1X = _hotkeyWidth;
            _divider2X = _divider1X + _logWidth + 1;
        }

        private void DrawDividers()
        {
            var bar = "│";
            for (int y = 0; y < _totalHeight; y++)
            {
                SafeWriteAt(_divider1X, y, bar);
                SafeWriteAt(_divider2X, y, bar);
            }
        }

        private void RenderPanes()
        {
            UpdateLayout();
            Console.Clear(); // Clear once at the start of a full render
            DrawDividers();

            RenderHotkeyPane();
            RenderLogPane();
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void RenderHotkeyPane()
        {
            var lines = BuildHeaderLines().ToList();
            for (int i = 0; i < lines.Count && i < _totalHeight; i++)
            {
                SafeWriteAt(0, i, PadAndTrunc(lines[i], _hotkeyWidth));
            }
        }

        private void RenderLogPane()
        {
            int startX = _divider1X + 1;
            int availableHeight = _totalHeight - 1; // reserve last line for prompt

            if (_leftBuffer.Count > LeftBufferMaxLines)
                _leftBuffer.RemoveRange(0, _leftBuffer.Count - LeftBufferMaxLines);

            var bufferToShow = _leftBuffer.Skip(Math.Max(0, _leftBuffer.Count - availableHeight)).ToList();

            for (int i = 0; i < bufferToShow.Count && i < availableHeight; i++)
            {
                SafeWriteAt(startX, i, PadAndTrunc(bufferToShow[i], _logWidth));
            }
        }
        private IEnumerable<string> BuildHeaderLines()
        {
            yield return "=== Quick File Manager ===";
            yield return "Hotkeys:";
            if (_config.Hotkeys != null && _config.Hotkeys.Count > 0)
            {
                foreach (var kv in _config.Hotkeys)
                    yield return $"  {Canonicalize(kv.Value)} : {kv.Key}";
            }
            else
            {
                yield return "  (No hotkeys configured.)";
            }
            yield return "------------------------";
        }

        private void DrawDirectoryPane(string directory)
        {
            try { _entries = Directory.GetFileSystemEntries(directory).ToList(); }
            catch { _entries = new List<string> { "Access Denied" }; }

            int startX = _divider2X + 1;
            int viewHeight = _totalHeight - 1; // reserve top line for "Dir:" header

            // Header
            SafeWriteAt(startX, 0, PadAndTrunc($"Dir: {Path.GetFileName(directory)}", _fileWidth));

            EnsureSelectionInRange(viewHeight);

            for (int i = 0; i < viewHeight; i++)
            {
                int idx = _scrollOffset + i;
                string line = idx < _entries.Count ? GetDisplayFor(_entries[idx]) : "";
                int y = i + 1;

                if (idx == _selectedIndex)
                    WriteRightHighlighted(startX, y, PadAndTrunc(line, _fileWidth));
                else
                    SafeWriteAt(startX, y, PadAndTrunc(line, _fileWidth));
            }
        }

        private void UpdateSelection(int oldIndex, int newIndex)
        {
            int startX = _divider2X + 1;
            int viewTop = 1;

            int oldY = viewTop + (oldIndex - _scrollOffset);
            if (oldY >= viewTop && oldY < _totalHeight)
                SafeWriteAt(startX, oldY, PadAndTrunc(GetDisplayFor(_entries[oldIndex]), _fileWidth));

            int newY = viewTop + (newIndex - _scrollOffset);
            if (newY >= viewTop && newY < _totalHeight)
                WriteRightHighlighted(startX, newY, PadAndTrunc(GetDisplayFor(_entries[newIndex]), _fileWidth));
        }

        private string GetDisplayFor(string fullPath)
        {
            string name = Path.GetFileName(fullPath);
            if (Directory.Exists(fullPath)) return $"[DIR]  {name}";
            return $"[FILE] {name}";
        }

        private void EnsureSelectionInRange(int viewHeight)
        {
            if (_selectedIndex < 0) _selectedIndex = 0;
            if (_selectedIndex >= _entries.Count) _selectedIndex = Math.Max(0, _entries.Count - 1);

            if (_selectedIndex < _scrollOffset) _scrollOffset = _selectedIndex;
            if (_selectedIndex >= _scrollOffset + viewHeight) _scrollOffset = _selectedIndex - viewHeight + 1;

            if (_scrollOffset < 0) _scrollOffset = 0;
            int maxOffset = Math.Max(0, _entries.Count - viewHeight);
            if (_scrollOffset > maxOffset) _scrollOffset = maxOffset;
        }

        // ---------- Safe console helpers ----------

        private void SafeWriteAt(int x, int y, string text)
        {
            try
            {
                if (y < 0 || y >= Console.WindowHeight) return;
                if (x < 0 || x >= Console.WindowWidth) return;
                Console.SetCursorPosition(Math.Min(x, Console.WindowWidth - 1), Math.Min(Math.Max(0, y), Console.WindowHeight - 1));
                Console.Write(text);
            }
            catch { }
        }

        private void WriteRightHighlighted(int x, int y, string text)
        {
            var origFg = Console.ForegroundColor;
            var origBg = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            SafeWriteAt(x, y, text);
            Console.ForegroundColor = origFg;
            Console.BackgroundColor = origBg;
        }

        private string Truncate(string s, int w)
        {
            if (s == null) s = "";
            if (w <= 0) return "";
            return s.Length <= w ? s : s.Substring(0, Math.Max(0, w - 1));
        }

        private string PadAndTrunc(string s, int w)
        {
            s = Truncate(s ?? "", w);
            return s.PadRight(Math.Max(0, w));
        }

        // ---------- Input helpers ----------

        private string Prompt(string promptText)
        {
            UpdateLayout();
            int promptY = _totalHeight - 1;
            int startX = _divider1X + 1;
            string fullPrompt = promptText.EndsWith(" ") ? promptText : promptText + " ";
            
            SafeWriteAt(startX, promptY, new string(' ', _logWidth));
            SafeWriteAt(startX, promptY, Truncate(fullPrompt, _logWidth - 1));

            int cursorStart = startX + Math.Min(fullPrompt.Length, _logWidth - 1);
            Console.SetCursorPosition(cursorStart, promptY);
            Console.CursorVisible = true;

            string input = Console.ReadLine() ?? "";
            
            Console.CursorVisible = false;
            AddLeft($"> {fullPrompt}{input}");
            bool entering = true;

            while (entering)
            {
                // Check if window resized while waiting for input
                if (Console.WindowWidth != _totalWidth || Console.WindowHeight != _totalHeight)
                {
                    RenderHotkeyPane(); 
                    // Reset cursor after re-render
                    Console.SetCursorPosition(cursorStart + input.Length, promptY);
                }

                if (!Console.KeyAvailable) continue;

                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    entering = false;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    input = string.Empty; // Cancel input
                    entering = false;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input = input.Substring(0, input.Length - 1);
                        // Visual backspace: move back, write space, move back
                        Console.SetCursorPosition(cursorStart + input.Length, promptY);
                        Console.Write(" ");
                        Console.SetCursorPosition(cursorStart + input.Length, promptY);
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    // Limit input length to prevent overflowing the pane
                    if (cursorStart + input.Length < _hotkeyWidth - 1)
                    {
                        input += key.KeyChar;
                        Console.Write(key.KeyChar);
                    }
                }
            }

            Console.CursorVisible = false;
            
            // Log the interaction to the left buffer
            if (!string.IsNullOrEmpty(input))
                AddLeft($"> {fullPrompt}{input}");

            return input.Trim();
        }

        private void AddLeft(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                _leftBuffer.Add(line);
            
            RenderPanes(); // Full refresh to keep all 3 panes in sync
        }

        // ---------- Hotkeys & normalization ----------

        private static string Canonicalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var s = raw.Replace(" ", "").ToUpperInvariant();
            return s.Replace("CONTROL+", "CTRL+");
        }

        private void InitializeHotkeys()
        {
            if (_config.Hotkeys == null) _config.Hotkeys = new Dictionary<string, string>();
            foreach (var pair in _config.Hotkeys)
            {
                var action = pair.Key?.Trim();
                var hot = Canonicalize(pair.Value ?? "");
                if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(hot)) continue;
                switch (action)
                {
                    case "CreateFile": _hotkeyActions[hot] = CreateNewFile; break;
                    case "AppendFile": _hotkeyActions[hot] = AppendToFile; break;
                    case "DeleteFile": _hotkeyActions[hot] = DeleteFile; break;
                    case "ListFiles": _hotkeyActions[hot] = ListFiles; break;
                    case "SearchFiles": _hotkeyActions[hot] = SearchInFile; break;
                    case "ReadFile": _hotkeyActions[hot] = ReadFile; break;
                    case "Exit": _hotkeyActions[hot] = () => _exitRequested = true; break;
                    case "OpenInEditor": _hotkeyActions[hot] = OpenInEditor; break;
                    case "JumpToDefault": _hotkeyActions[hot] = JumpToDefaultDirectory; break;
                    case "AddBookmark": _hotkeyActions[hot] = AddBookmark; break;
                    case "CycleBookmarks": _hotkeyActions[hot] = CycleBookmarks; break;
                    case "BatchDelete": _hotkeyActions[hot] = BatchDeleteFiles; break;
                    case "BatchRename": _hotkeyActions[hot] = BatchRenameFiles; break;
                    case "BatchMove": _hotkeyActions[hot] = BatchMoveFiles; break;
                    case "QuickPreview": _hotkeyActions[hot] = QuickPreview; break;
                    case "MultiFileSearch": _hotkeyActions[hot] = MultiFileSearch; break;
                    case "FilterBySize": _hotkeyActions[hot] = FilterBySize; break;
                    case "FilterByDate": _hotkeyActions[hot] = FilterByDate; break;
                    case "FileInfo": _hotkeyActions[hot] = ShowFileInfo; break;
                    case "CopyFilePath": _hotkeyActions[hot] = CopyFilePath; break;
                    case "PasteFile": _hotkeyActions[hot] = PasteFile; break;
                    case "CreateFromTemplate": _hotkeyActions[hot] = CreateFromTemplate; break;
                    case "AppendSnippet": _hotkeyActions[hot] = AppendSnippet; break;
                    case "ChangeDirectory": _hotkeyActions[hot] = ChangeDirectory; break;
                    case "GoBackDirectory": _hotkeyActions[hot] = GoBackDirectory; break;
                    default: _logger?.Warn($"Unknown hotkey: {action}"); break;
                }
            }
        }

        private static string NormalizeKey(ConsoleKeyInfo keyInfo)
        {
            var parts = new List<string>();

            if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0) parts.Add("CTRL");
            if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0) parts.Add("SHIFT");
            if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0) parts.Add("ALT");

            // Handle special cases for non-character keys
            if (keyInfo.KeyChar == '\0' || char.IsControl(keyInfo.KeyChar))
            {
                parts.Add(keyInfo.Key.ToString().ToUpperInvariant());
            }
            else
            {
                parts.Add(char.ToUpperInvariant(keyInfo.KeyChar).ToString());
            }

            return string.Join("+", parts);
        }

        // ---------- Directory list management ----------

        private void LoadDirectoryEntries(string dir)
        {
            try { _entries = Directory.GetFileSystemEntries(dir).ToList(); }
            catch { _entries = new List<string>(); }
            _selectedIndex = 0;
            _scrollOffset = 0;
        }

        // ---------- Main loop with navigation ----------

        public void RunWithHotkeys()
        {
            _exitRequested = false;
            RenderPanes();

            int prevW = Console.WindowWidth;
            int prevH = Console.WindowHeight;

            while (!_exitRequested)
            {
                if (Console.WindowWidth != prevW || Console.WindowHeight != prevH)
                {
                    prevW = Console.WindowWidth;
                    prevH = Console.WindowHeight;
                    RenderPanes();
                }

                var keyInfo = Console.ReadKey(intercept: true);

                if (HandleNavigationKey(keyInfo)) continue;

                var normalized = NormalizeKey(keyInfo);
                if (normalized == "CTRL+S" || normalized == "CTRL+Q")
                    AddLeft($"Note: {normalized} may be intercepted by the terminal. Use another key or run: stty -ixon");

                var canonical = normalized.Replace(" ", "").ToUpperInvariant();
                if (_hotkeyActions.TryGetValue(canonical, out var action))
                {
                    try { action.Invoke(); }
                    catch (Exception ex) { AddLeft($"Action error: {ex.Message}"); }
                }
                else
                {
                    AddLeft($"Unknown hotkey: {canonical}");
                }
            }
        }

        private bool HandleNavigationKey(ConsoleKeyInfo keyInfo)
        {
            int viewHeight = Math.Max(0, _totalHeight - 1);
            
            int oldIndex = _selectedIndex;
            int oldOffset = _scrollOffset;

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    if (_entries.Count == 0) return true;
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    break;

                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    if (_entries.Count == 0) return true;
                    _selectedIndex = Math.Min(_entries.Count - 1, _selectedIndex + 1);
                    break;

                case ConsoleKey.PageUp:
                    if (_entries.Count == 0) return true;
                    _selectedIndex = Math.Max(0, _selectedIndex - (viewHeight - 1));
                    break;

                case ConsoleKey.PageDown:
                    if (_entries.Count == 0) return true;
                    _selectedIndex = Math.Min(Math.Max(0, _entries.Count - 1), _selectedIndex + (viewHeight - 1));
                    break;

                case ConsoleKey.Home:
                    if (_entries.Count == 0) return true;
                    _selectedIndex = 0;
                    break;

                case ConsoleKey.End:
                    if (_entries.Count == 0) return true;
                    _selectedIndex = Math.Max(0, _entries.Count - 1);
                    break;

                case ConsoleKey.Enter:
                    if (_entries.Count == 0) return true;
                    var target = _entries[_selectedIndex];
                    if (Directory.Exists(target))
                    {
                        try
                        {
                            _previousDirectory = Environment.CurrentDirectory;
                            Environment.CurrentDirectory = target;
                            LoadDirectoryEntries(target);
                            AddLeft($"Changed directory to: {target}");
                        }
                        catch (Exception ex) { AddLeft($"Failed to cd: {ex.Message}"); }
                        DrawDirectoryPane(Environment.CurrentDirectory);
                    }
                    else if (File.Exists(target))
                    {
                        try
                        {
                            _manager.OpenInEditor(target);
                            AddLeft($"Opened: {target}");
                        }
                        catch (Exception ex) { AddLeft($"Open failed: {ex.Message}"); }
                        DrawDirectoryPane(Environment.CurrentDirectory);
                    }
                    return true;

                default:
                    return false;
            }

            EnsureSelectionInRange(viewHeight);

            if (_scrollOffset != oldOffset)
            {
                DrawDirectoryPane(Environment.CurrentDirectory);
            }
            else if (_selectedIndex != oldIndex)
            {
                UpdateSelection(oldIndex, _selectedIndex);
            }

            return true;
        }

        // ---------- File operation methods (use AddLeft/Prompt) ----------

        private string ExpandPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            path = path.Trim();
            if (path == "~") return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (path.StartsWith("~/")) return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.Substring(2));
            return path;
        }

        private void CreateNewFile()
        {
            var path = Prompt("Enter file path to create:");
            if (string.IsNullOrEmpty(path)) { AddLeft("Path cannot be empty."); return; }
            path = ExpandPath(path);
            try { _manager.CreateFile(path); AddLeft($"File created: {path}"); }
            catch (Exception ex) { AddLeft($"Create failed: {ex.Message}"); }
            LoadDirectoryEntries(Environment.CurrentDirectory);
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void ChangeDirectory()
        {
            var input = Prompt("cd to (.., ~/Documents, absolute or relative):");
            if (string.IsNullOrEmpty(input)) return;

            string target = ExpandPath(input);
            if (!Path.IsPathRooted(target))
                target = Path.Combine(Environment.CurrentDirectory, target);

            if (Directory.Exists(target))
            {
                _previousDirectory = Environment.CurrentDirectory; // store current for "back"
                Environment.CurrentDirectory = Path.GetFullPath(target);
                LoadDirectoryEntries(Environment.CurrentDirectory);
                AddLeft($"Directory changed to: {Environment.CurrentDirectory}");
            }
            else
            {
                AddLeft($"Directory does not exist: {target}");
            }

            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void GoBackDirectory()
        {
            if (!string.IsNullOrEmpty(_previousDirectory) && Directory.Exists(_previousDirectory))
            {
                string temp = Environment.CurrentDirectory;
                Environment.CurrentDirectory = _previousDirectory;
                _previousDirectory = temp; // swap to allow back/forth
                LoadDirectoryEntries(Environment.CurrentDirectory);
                AddLeft($"Returned to: {Environment.CurrentDirectory}");
            }
            else
            {
                AddLeft("No previous directory to return to.");
            }

            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void ReadFile()
        {
            var path = Prompt("Enter file path to read:");
            if (string.IsNullOrEmpty(path)) { AddLeft("Path cannot be empty."); return; }
            path = ExpandPath(path);
            try
            {
                var content = _manager.ReadFile(path);
                AddLeft("--- File contents start ---");
                AddLeft(content);
                AddLeft("--- File contents end ---");
            }
            catch (Exception ex) { AddLeft($"Read failed: {ex.Message}"); }
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void AppendToFile()
        {
            var path = Prompt("Enter file path to append to:");
            if (string.IsNullOrEmpty(path)) { AddLeft("Path cannot be empty."); return; }
            path = ExpandPath(path);
            AddLeft("Enter text to append. Finish with a single line containing only '::end'");
            var lines = new List<string>();
            while (true)
            {
                var line = Prompt("");
                if (line == "::end") break;
                lines.Add(line);
            }
            try { _manager.AppendToFile(path, string.Join(Environment.NewLine, lines) + Environment.NewLine); AddLeft("Append complete."); }
            catch (Exception ex) { AddLeft($"Append failed: {ex.Message}"); }
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void DeleteFile()
        {
            var path = Prompt("Enter file path to delete:");
            if (string.IsNullOrEmpty(path)) { AddLeft("Path cannot be empty."); return; }
            path = ExpandPath(path);
            var ans = Prompt($"Are you sure you want to delete {path}? (y/N):");
            if (ans?.Trim().ToLower() == "y")
            {
                try { _manager.DeleteFile(path); AddLeft("File deleted (if existed)."); }
                catch (Exception ex) { AddLeft($"Delete failed: {ex.Message}"); }
            }
            else AddLeft("Delete cancelled.");
            LoadDirectoryEntries(Environment.CurrentDirectory);
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void ListFiles()
        {
            var dir = Prompt("Enter directory path (leave empty for current):");
            if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;
            dir = ExpandPath(dir);
            try
            {
                var files = _manager.ListFiles(dir);
                AddLeft($"Files in {dir}:");
                foreach (var f in files) AddLeft(f);
            }
            catch (Exception ex) { AddLeft($"List failed: {ex.Message}"); }
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void SearchInFile()
        {
            var path = Prompt("Enter file path to search:");
            if (string.IsNullOrEmpty(path)) { AddLeft("Path cannot be empty."); return; }
            path = ExpandPath(path);
            var term = Prompt("Enter search text:");
            try
            {
                var results = _manager.SearchInFile(path, term);
                AddLeft($"Found {results.Count} matching lines:");
                foreach (var r in results) AddLeft(r);
            }
            catch (Exception ex) { AddLeft($"Search failed: {ex.Message}"); }
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        // Placeholders for your multi-file and filter ops (reuse your implementations)
        private void BatchDeleteFiles() { AddLeft("Batch delete starting..."); }
        private void BatchRenameFiles() { AddLeft("Batch rename starting..."); }
        private void BatchMoveFiles() { AddLeft("Batch move starting..."); }
        private void QuickPreview() { AddLeft("Quick preview..."); }
        private void MultiFileSearch() { AddLeft("Multi-file search..."); }
        private void FilterBySize() { AddLeft("Filter by size..."); }
        private void FilterByDate() { AddLeft("Filter by date..."); }

        private void OpenInEditor()
        {
            var path = Prompt("Enter file path to open in editor:");
            if (string.IsNullOrEmpty(path)) { AddLeft("Path cannot be empty."); return; }
            path = ExpandPath(path);
            try { _manager.OpenInEditor(path); AddLeft($"Opened in editor: {path}"); }
            catch (Exception ex) { AddLeft($"Open editor failed: {ex.Message}"); }
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void JumpToDefaultDirectory()
        {
            var target = ExpandPath(_config.DefaultDirectory ?? ".");
            if (!Path.IsPathRooted(target)) target = Path.Combine(Environment.CurrentDirectory, target);
            if (Directory.Exists(target))
            {
                _previousDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = Path.GetFullPath(target);
                LoadDirectoryEntries(Environment.CurrentDirectory);
                AddLeft($"Changed directory: {Environment.CurrentDirectory}");
            }
            else AddLeft($"Directory not found: {target}");
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void AddBookmark()
        {
            var p = Prompt("Enter directory to bookmark:");
            if (string.IsNullOrEmpty(p)) p = Environment.CurrentDirectory;
            p = ExpandPath(p);
            if (!_config.Bookmarks.Contains(p)) { _config.Bookmarks.Add(p); AddLeft($"Bookmark added: {p}"); }
            else AddLeft("Bookmark exists.");
        }

        private void CycleBookmarks()
        {
            if (_config.Bookmarks.Count == 0) { AddLeft("No bookmarks."); return; }
            _currentBookmarkIndex = (_currentBookmarkIndex + 1) % _config.Bookmarks.Count;
            _previousDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _config.Bookmarks[_currentBookmarkIndex];
            LoadDirectoryEntries(Environment.CurrentDirectory);
            AddLeft($"Jumped to: {_config.Bookmarks[_currentBookmarkIndex]}");
            DrawDirectoryPane(Environment.CurrentDirectory);
        }

        private void ShowFileInfo()
        {
            // 1. Ask for input
            var p = Prompt("Enter file path (leave empty for selected):");
            string targetPath;

            if (!string.IsNullOrWhiteSpace(p))
            {
                // Use user input
                targetPath = ExpandPath(p);
            }
            else if (_entries.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _entries.Count)
            {
                // Fallback: Use the item selected in the UI list
                targetPath = _entries[_selectedIndex];
                AddLeft($"Inspecting selected: {Path.GetFileName(targetPath)}");
            }
            else
            {
                AddLeft("Nothing selected and no path provided.");
                return;
            }

            try
            {
                // 2. Get info via manager
                var info = _manager.GetFileInfo(targetPath);
                
                // 3. Display details
                AddLeft($"--- Info: {info.Name} ---");
                
                // Check if it's a directory or file for better formatting
                if (Directory.Exists(targetPath))
                {
                    AddLeft("Type: Directory");
                    // Note: FileInfo.Length throws for directories; handle accordingly
                    AddLeft($"Modified: {info.LastWriteTime}");
                }
                else
                {
                    AddLeft("Type: File");
                    AddLeft($"Size: {info.Length} bytes");
                    AddLeft($"Modified: {info.LastWriteTime}");
                    AddLeft($"Extension: {info.Extension}");
                }
            }
            catch (Exception ex) 
            { 
                AddLeft($"Info failed: {ex.Message}"); 
            }
        }

        private void CopyFilePath()
        {
            var p = Prompt("Enter file path to copy:");
            if (string.IsNullOrEmpty(p)) { AddLeft("Empty."); return; }
            p = ExpandPath(p);
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    Process.Start("cmd", $"/c echo {p} | clip");
                else
                {
                    try { var pr = Process.Start("xclip", "-selection clipboard"); if (pr != null) pr.StandardInput.Write(p); }
                    catch { var pr2 = Process.Start("pbcopy"); if (pr2 != null) pr2.StandardInput.Write(p); }
                }
                AddLeft("Copied to clipboard.");
            }
            catch (Exception ex) { AddLeft($"Copy failed: {ex.Message}"); }
        }

        private void PasteFile() { AddLeft("PasteFile is platform specific. Implement as needed."); }
        private void CreateFromTemplate() { AddLeft("CreateFromTemplate placeholder."); }
        private void AppendSnippet() { AddLeft("AppendSnippet placeholder."); }
    }
}
