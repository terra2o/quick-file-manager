using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpFileManager
{
    public class FileManager
    {
        private readonly IFileService _fileService;
        private readonly ILogger _logger;

        public FileManager(IFileService fileService, ILogger logger)
        {
            _fileService = fileService;
            _logger = logger;
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

        public IEnumerable<string> ListFiles(string directory)
        {
            try
            {
                return _fileService.GetFiles(directory);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return Enumerable.Empty<string>();
            }
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
    }
}