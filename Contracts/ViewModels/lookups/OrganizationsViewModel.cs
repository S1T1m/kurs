using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Contracts.Data;
using Contracts.Models;
using Microsoft.EntityFrameworkCore;


namespace Contracts.ViewModels.lookups;

public sealed class OrganizationsViewModel : BaseViewModel
{
    private readonly DbContextFactory _factory;

    public ObservableCollection<Organization> Items { get; } = [];

    private Organization? _selected;
    public Organization? Selected
    {
        get => _selected;
        set { Set(ref _selected, value); DeleteCmd.RaiseCanExecuteChanged(); }
    }
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

    public RelayCommand RefreshCmd { get; }
    public RelayCommand AddCmd { get; }
    public RelayCommand DeleteCmd { get; }
    public RelayCommand SaveCmd { get; }

    public OrganizationsViewModel(DbContextFactory factory)
    {
        _factory = factory;

        
        _itemsView = CollectionViewSource.GetDefaultView(Items);
        _itemsView.Filter = FilterItem;

        RefreshCmd = new RelayCommand(async _ => await LoadAsync());
        AddCmd     = new RelayCommand(_ => Items.Add(new Organization { Name = "Новая организация" }));
        DeleteCmd  = new RelayCommand(async _ => await DeleteAsync(), _ => Selected != null);
        SaveCmd    = new RelayCommand(async _ => await SaveAsync());

        _ = LoadAsync();
    }

    private bool FilterItem(object obj)
    {
        if (obj is not Organization o)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var text = SearchText.Trim();
        var cmp = StringComparison.OrdinalIgnoreCase;

        return
            (o.Name?.IndexOf(text, cmp) >= 0) ||
            (o.Address?.IndexOf(text, cmp) >= 0) ||
            (o.Phone?.IndexOf(text, cmp) >= 0) ||
            (o.Inn?.IndexOf(text, cmp) >= 0) ||
            (o.BankAccount?.IndexOf(text, cmp) >= 0) ||
            (o.Bik?.IndexOf(text, cmp) >= 0);
    }

    
    private async Task LoadAsync()
    {
        Items.Clear();
        await using var db = _factory.Create();
        var list = await db.Organizations
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync();
        foreach (var o in list) Items.Add(o);
        _itemsView.Refresh();
    }

    private async Task DeleteAsync()
    {
        var item = Selected;
        if (item is null) return;

        if (item.OrgId == 0) { Items.Remove(item); Selected = null; return; }

        await using var db = _factory.Create();

        var used = await db.Contracts
            .AsNoTracking()
            .AnyAsync(c => c.CustomerId == item.OrgId || c.ContractorId == item.OrgId);

        if (used)
        {
            MessageBox.Show(
                "Нельзя удалить организацию: она используется в договорах (как заказчик или исполнитель).",
                "Удаление запрещено",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        db.Entry(new Organization { OrgId = item.OrgId }).State = EntityState.Deleted;

        try
        {
            await db.SaveChangesAsync();
            Items.Remove(item);
            Selected = null;
        }
        catch (DbUpdateException ex)
        {
            MessageBox.Show("Ошибка при удалении: " + ex.Message, "Удаление не выполнено",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveAsync()
    {
        await using var db = _factory.Create();
        foreach (var o in Items)
        {
            if (o.OrgId == 0) db.Organizations.Add(o);
            else db.Organizations.Update(o);
        }
        await db.SaveChangesAsync();
        MessageBox.Show("Сохранено.");
        await LoadAsync();
    }
}
