using System.Windows;
using Contracts.Data;
using Contracts.ViewModels;

namespace Contracts;

public partial class App
{
    public static DbContextFactory DbFactory { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = ContractsDbLocator.ResolveDbPath();
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            MessageBox.Show("Файл базы данных не выбран. Приложение будет закрыто."
                ,"Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DbFactory = new DbContextFactory(dbPath);

        var main = new Views.MainWindow
        {
            DataContext = new MainViewModel(DbFactory)
        };
        main.Show();
    }
}