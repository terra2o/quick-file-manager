namespace QuickFileManager
{
    public interface ILogger
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
    }
}