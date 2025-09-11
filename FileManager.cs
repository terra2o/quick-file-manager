using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace QuickFileManager
{
    public class FileManager
    {
        private readonly IFileService _fileService;
        private readonly ILogger _logger;
        private readonly Config _config;

        public FileManager(IFileService fileService, ILogger logger, Config config)
        {
            _fileService = fileService;
            _logger = logger;
            _config = config;
        }

        public void CreateFile(string path)
        {
            try
            {
                if (_fileService.Exists(path))
                {
                    _logger.Info("File already exists.");
                    return;
                }

                _fileService.CreateFile(path);
                _logger.Info("File created.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public string ReadFile(string path)
        {
            if (!_fileService.Exists(path))
            {
                _logger.Warn("File does not exist.");
                return string.Empty;
            }

            return _fileService.ReadAllText(path);
        }

        public void AppendToFile(string path, string text)
        {
            try
            {
                if (!_fileService.Exists(path))
                {
                    _logger.Warn("File does not exist — creating new file.");
                    _fileService.CreateFile(path);
                }

                _fileService.AppendAllText(path, text);
                _logger.Info("Text appended.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public void DeleteFile(string path)
        {
            try
            {
                if (!_fileService.Exists(path))
                {
                    _logger.Warn("File does not exist.");
                    return;
                }

                _fileService.DeleteFile(path);
                _logger.Info("File deleted.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public IEnumerable<string> ListFiles(string directory, string pattern = "*")
        {
            if (string.IsNullOrEmpty(directory))
                directory = Environment.CurrentDirectory;

            if (!Directory.Exists(directory))
                return Enumerable.Empty<string>();

            return Directory.GetFiles(directory, pattern);
        }

        public void RenameFile(string oldPath, string newPath)
        {
            try
            {
                if (!_fileService.Exists(oldPath))
                {
                    _logger.Warn("Original file does not exist.");
                    return;
                }

                _fileService.Move(oldPath, newPath);
                _logger.Info("File renamed/moved.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public List<string> SearchInFile(string path, string term)
        {
            var results = new List<string>();
            if (!_fileService.Exists(path))
            {
                _logger.Warn("File does not exist.");
                return results;
            }

            var lines = _fileService.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(term ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    results.Add($"{i + 1}: {lines[i]}");
                }
            }

            return results;
        }


        public void CopyFile(string sourcePath, string destPath)
        {
            try
            {
                if (!_fileService.Exists(sourcePath))
                {
                    _logger.Warn("Source file does not exist.");
                    return;
                }

                _fileService.CopyFile(sourcePath, destPath);
                _logger.Info("File copied.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public void BatchDeleteFiles(string[] paths)
        {
            try
            {
                foreach (var path in paths)
                {
                    if (_fileService.Exists(path))
                    {
                        _fileService.DeleteFile(path);
                        _logger.Info($"Deleted: {path}");
                    }
                    else
                    {
                        _logger.Warn($"File does not exist: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public void BatchRenameFiles(string directory, string searchPattern, string newNamePattern)
        {
            try
            {
                var files = _fileService.GetFiles(directory, searchPattern);
                for (int i = 0; i < files.Length; i++)
                {
                    var oldPath = files[i];
                    var extension = Path.GetExtension(oldPath);
                    var newFileName = string.Format(newNamePattern, i + 1);
                    var newPath = Path.Combine(directory, newFileName + extension);

                    _fileService.Move(oldPath, newPath);
                    _logger.Info($"Renamed: {Path.GetFileName(oldPath)} -> {newFileName + extension}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public void BatchMoveFiles(string[] sourcePaths, string destinationDirectory)
        {
            try
            {
                if (!Directory.Exists(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                foreach (var sourcePath in sourcePaths)
                {
                    if (_fileService.Exists(sourcePath))
                    {
                        var fileName = Path.GetFileName(sourcePath);
                        var destPath = Path.Combine(destinationDirectory, fileName);
                        _fileService.Move(sourcePath, destPath);
                        _logger.Info($"Moved: {fileName}");
                    }
                    else
                    {
                        _logger.Warn($"File does not exist: {sourcePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public string PreviewFile(string path, int maxLines = 10)
        {
            if (!_fileService.Exists(path))
            {
                _logger.Warn("File does not exist.");
                return string.Empty;
            }

            var lines = _fileService.ReadAllLines(path);
            return string.Join(Environment.NewLine, lines.Take(maxLines));
        }

        public Dictionary<string, List<string>> SearchInFiles(string directory, string searchTerm, string searchPattern = "*")
        {
            var results = new Dictionary<string, List<string>>();
            try
            {
                var files = _fileService.GetFiles(directory, searchPattern);
                foreach (var file in files)
                {
                    var fileResults = SearchInFile(file, searchTerm);
                    if (fileResults.Count > 0)
                    {
                        results[file] = fileResults;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            return results;
        }

        public IEnumerable<string> FilterFilesBySize(string directory, long minSize, long maxSize, string searchPattern = "*")
        {
            var files = _fileService.GetFiles(directory, searchPattern);
            return files.Where(file =>
            {
                var info = _fileService.GetFileInfo(file);
                return info.Length >= minSize && info.Length <= maxSize;
            });
        }

        public IEnumerable<string> FilterFilesByDate(string directory, DateTime startDate, DateTime endDate, string searchPattern = "*")
        {
            var files = _fileService.GetFiles(directory, searchPattern);
            return files.Where(file =>
            {
                var info = _fileService.GetFileInfo(file);
                return info.LastWriteTime >= startDate && info.LastWriteTime <= endDate;
            });
        }

        public void OpenInEditor(string path)
        {
            if (!_fileService.Exists(path))
            {
                _logger.Warn("File does not exist.");
                return;
            }

            try
            {
                string editor = _config.Editor ?? GetDefaultEditor();
                var startInfo = new ProcessStartInfo
                {
                    FileName = editor,
                    Arguments = BuildEditorArgs(editor, path),
                    UseShellExecute = false
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    _logger.Info($"Opened in {editor}: {path}");
                    process.WaitForExit(); // QFM pauses here until editor closes
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open editor: {ex.Message}");
            }
        }

        // Determine default editor if none configured
        private string GetDefaultEditor()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "notepad";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "nano";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "nano";
            return "nano"; // fallback
        }

        // Build editor arguments with "--wait" where supported
        private string BuildEditorArgs(string editor, string path)
        {
            editor = Path.GetFileName(editor).ToLowerInvariant();

            return editor switch
            {
                "code" => $"--wait \"{path}\"", // VS Code
                "subl" => $"--wait \"{path}\"", // Sublime Text
                "gedit" => $"\"{path}\"", // GUI Linux editors usually block anyway
                "notepad" => $"\"{path}\"", // Notepad blocks by default
                "nano" => $"\"{path}\"", // Terminal editor, blocks naturally
                "vim" => $"\"{path}\"", // Terminal editor, blocks naturally
                _ => $"\"{path}\"" // fallback
            };
        }



        public FileInfo GetFileInfo(string path)
        {
            return _fileService.GetFileInfo(path);
        }

        public void CreateFromTemplate(string path, string templateName)
        {
            try
            {
                if (_config.Templates.TryGetValue(templateName, out var templateContent))
                {
                    File.WriteAllText(path, templateContent);
                    _logger.Info($"Created file from template: {templateName}");
                }
                else
                {
                    _logger.Warn($"Template not found: {templateName}");
                    CreateFile(path); // Fallback to empty file
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }

        public void AppendSnippet(string path, string snippetName)
        {
            try
            {
                if (_config.Snippets.TryGetValue(snippetName, out var snippetContent))
                {
                    AppendToFile(path, snippetContent);
                    _logger.Info($"Appended snippet: {snippetName}");
                }
                else
                {
                    _logger.Warn($"Snippet not found: {snippetName}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }
    }
}