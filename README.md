# Quick File Manager (QFM) 📚📃

A lightweight, terminal-based file manager written in C#. Designed for developers and power users who want quick file operations without leaving the console. Works across Windows, Linux, macOS, and BSD (with .NET 9 SDK installed).

---

# Features

- **Hotkey** focused file manager. It's designed to be quick and efficient
- Cross-platform console UI
- Clean, modular C# codebase
- Check the Hotkeys tab to see what you can do

---

# Getting Started

### You need

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Terminal
- Git

---

# Build & Run

### Build
Use `setup-qfm.sh` make it executable via `chmod +x setup-qfm.sh` if not executable.

### Run
enter `qfm` in terminal.

---

### Hotkeys

Once the program is running, you can use the following key combinations:
```
Hotkey	--> Action
    "CreateFile": "Ctrl+N",
    "AppendFile": "Ctrl+A",
    "DeleteFile": "Ctrl+E",
    "ListFiles": "Ctrl+L",
    "SearchFiles": "Ctrl+S",
    "ReadFile": "Ctrl+R",
    "Exit": "Ctrl+Q",
    "BatchDelete": "Ctrl+Alt+D",
    "BatchRename": "Ctrl+Alt+R",
    "BatchMove": "Ctrl+Alt+M",
    "QuickPreview": "Ctrl+P",
    "MultiFileSearch": "Ctrl+Alt+F",
    "FilterBySize": "Ctrl+Alt+S",
    "FilterByDate": "Ctrl+Alt+D",
    "OpenInEditor": "Ctrl+Alt+E",
    "JumpToDefault": "Ctrl+G",
    "AddBookmark": "Ctrl+B",
    "CycleBookmarks": "Ctrl+Alt+B",
    "FileInfo": "Ctrl+I",
    "CopyFilePath": "Ctrl+C",
    "PasteFile": "Ctrl+V",
    "CreateFromTemplate": "Ctrl+T",
    "AppendSnippet": "Ctrl+Alt+A",
    "ChangeDirectory": "Ctrl+O",
    "GoBackDirectory": "F1"
```
You can change this via editing the config.json file.

---

### Contribution
This is just a simple project. Contributions are totally welcome!  
Feel free to fork the repo, tweak the code, fix bugs, or add features.  
Pull requests are appreciated. Anything you want to try out is fine.

### Warning
This project is WIP. It is VERY buggy. Some things don't work. There are a lot of features I'm planning to add.
