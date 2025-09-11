using System.Collections.Generic;

namespace QuickFileManager
{
    public class Config
    {
        public Dictionary<string, string> Hotkeys { get; set; } = new();
        public string DefaultDirectory { get; set; } = ".";
        public string Editor { get; set; } = "nano";
        public List<string> Bookmarks { get; set; } = new();
        public Dictionary<string, string> Templates { get; set; } = new();
        public Dictionary<string, string> Snippets { get; set; } = new();
    }
}