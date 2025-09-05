using System;
using System.IO;

namespace QuickFileManager
{
    public class FileSystemFileService : IFileService
    {
        private readonly ILogger _logger;

        public FileSystemFileService(ILogger logger)
        {
            _logger = logger;
        }

        public void CreateFile(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                _logger.Info($"Created directory {dir}");
            }

            using (var fs = File.Create(path)) { }
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(path);
        }

        public void AppendAllText(string path, string contents)
        {
            File.AppendAllText(path, contents);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public string[] GetFiles(string directory)
        {
            if (string.IsNullOrEmpty(directory)) directory = Environment.CurrentDirectory;
            if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
            return Directory.GetFiles(directory);
        }

        public void Move(string sourcePath, string destPath)
        {
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
            File.Move(sourcePath, destPath);
        }
    }
}