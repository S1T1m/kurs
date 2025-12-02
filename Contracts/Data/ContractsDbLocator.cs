using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Contracts.Data;

public static class ContractsDbLocator
{
    
    private static string ConfigPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Contracts",
            "config.txt");

    public static string? ResolveDbPath()
    {
        if (File.Exists(ConfigPath))
        {
            var saved = File.ReadAllText(ConfigPath).Trim();
            if (!string.IsNullOrWhiteSpace(saved) && File.Exists(saved))
                return saved;
        }

        var baseDir = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDir, "contracts.db"),
            Path.Combine(baseDir, "Data", "contracts.db")
        ];

        foreach (var c in candidates)
            if (File.Exists(c))
            {
                SavePath(c);
                return c;
            }

        var dlg = new OpenFileDialog
        {
            Title = "Укажите файл базы данных contracts.db",
            Filter = "SQLite DB (*.db)|*.db|Все файлы (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() != true) return null;
        SavePath(dlg.FileName);
        return dlg.FileName;

    }

    private static void SavePath(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            File.WriteAllText(ConfigPath, path);
        }
        catch(FileNotFoundException e)
        {
            MessageBox.Show($"""
                             "Файл базы данных не найден.
                             Ошибка {e.Message}"
                             """, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
