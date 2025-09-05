namespace QuickFileManager
{
    public interface IFileService
    {
        void CreateFile(string path);
        string ReadAllText(string path);
        string[] ReadAllLines(string path);
        void AppendAllText(string path, string contents);
        void DeleteFile(string path);
        bool Exists(string path);
        string[] GetFiles(string directory);
        void Move(string sourcePath, string destPath);
    }
}