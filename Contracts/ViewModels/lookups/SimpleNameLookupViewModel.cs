using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Contracts.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Contracts.ViewModels.lookups;

public sealed class SimpleNameLookupViewModel : BaseViewModel
{
    private readonly DbContextFactory _factory;
    private readonly string _table;
    private readonly string _idColumn;

    public ObservableCollection<LookupItem> Items { get; } = [];

    private readonly ICollectionView _itemsView;
    public ICollectionView ItemsView => _itemsView;
    
    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            Set(ref _searchText, value);
            _itemsView.Refresh();
        }
    }
    
    private LookupItem? _selected;
    public LookupItem? Selected
    {
        get => _selected;
        set { Set(ref _selected, value); DeleteCmd.RaiseCanExecuteChanged(); }
    }

    private string _newName = "";
    public string NewName
    {
        get => _newName;
        set { Set(ref _newName, value); AddCmd.RaiseCanExecuteChanged(); }
    }

    public RelayCommand RefreshCmd { get; }
    public RelayCommand AddCmd { get; }
    public RelayCommand DeleteCmd { get; }
    public RelayCommand SaveCmd { get; }

    public SimpleNameLookupViewModel(DbContextFactory factory, string table, string idColumn)
    {
        _factory = factory; _table = table; _idColumn = idColumn;

        
        _itemsView = CollectionViewSource.GetDefaultView(Items);
        _itemsView.Filter = FilterItem;
        
        RefreshCmd = new RelayCommand(async _ => await LoadAsync());
        AddCmd     = new RelayCommand(async _ => await AddAsync(),    _ => !string.IsNullOrWhiteSpace(NewName));
        DeleteCmd  = new RelayCommand(async _ => await DeleteAsync(), _ => Selected != null);
        SaveCmd    = new RelayCommand(async _ => await SaveAsync());

        _ = LoadAsync();
    }

    private bool FilterItem(object obj)
    {
        if (obj is not LookupItem item)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var text = SearchText.Trim();
        return item.Name?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private async Task LoadAsync()
    {
        Items.Clear();
        await using var db = _factory.Create();
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {_idColumn}, name FROM {_table} ORDER BY name";
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            Items.Add(new LookupItem
            {
                Id = r.GetInt32(0), Name = r.GetString(1)
            });
        _itemsView.Refresh();
    }

    private async Task AddAsync()
    {
        await using var db = _factory.Create();
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = $"INSERT INTO {_table}(name) VALUES ($n); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$n", NewName.Trim());
        long newId = (long)await cmd.ExecuteScalarAsync();

        Items.Add(new LookupItem { Id = (int)newId, Name = NewName.Trim() });
        NewName = "";
    }

    private async Task DeleteAsync()
    {
        var item = Selected;
        if (item is null) return;

        if (item.Id == 0) { Items.Remove(item); Selected = null; return; }

        await using var db = _factory.Create();
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        await conn.OpenAsync();

        try
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM {_table} WHERE {_idColumn}=$id";
            cmd.Parameters.AddWithValue("$id", item.Id);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected > 0) { Items.Remove(item); Selected = null; }
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) 
        {
            MessageBox.Show("Нельзя удалить: значение используется в связанных данных.", 
                            "Удаление запрещено",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task SaveAsync()
    {
        await using var db = _factory.Create();
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        await conn.OpenAsync();

        foreach (var item in Items)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE {_table} SET name=$n WHERE {_idColumn}=$id";
            cmd.Parameters.AddWithValue("$n", item.Name);
            cmd.Parameters.AddWithValue("$id", item.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        MessageBox.Show("Сохранено.");
        await LoadAsync();
    }
}
