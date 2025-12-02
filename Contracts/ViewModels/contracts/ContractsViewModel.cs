using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Contracts.Data;
using Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace Contracts.ViewModels.contracts;

public sealed class ContractsViewModel : BaseViewModel
{
    private readonly DbContextFactory _factory;

    private ContractsDbContext _db = null!;

    public ObservableCollection<Contract> Items { get; } = [];

    private Contract? _selected;
    public Contract? Selected
    {
        get => _selected;
        set
        {
            Set(ref _selected, value); 
            UpdateChildCommands();
        }
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

    public ObservableCollection<Organization> Orgs { get; } = [];
    public ObservableCollection<ContractType> Types { get; } = [];
    public ObservableCollection<Stage> Stages { get; } = [];
    public ObservableCollection<VatRate> Vats { get; } = [];
    public ObservableCollection<PaymentType> PayTypes { get; } = [];

    public RelayCommand RefreshCmd { get; }
    public RelayCommand AddContractCmd { get; }
    public RelayCommand DeleteSelectedCmd { get; }
    public RelayCommand SaveAllCmd { get; }

    public RelayCommand AddPaymentCmd { get; }
    public RelayCommand DeletePaymentCmd { get; }
    public RelayCommand AddPhaseCmd { get; }
    public RelayCommand DeletePhaseCmd { get; }

    public ContractsViewModel(DbContextFactory factory)
    {
        _factory = factory;

        RefreshCmd        = new RelayCommand(async _ => await LoadAsync());
        AddContractCmd    = new RelayCommand(_ => AddContract());
        DeleteSelectedCmd = new RelayCommand(async _ => await DeleteSelectedAsync(), _ => Selected != null);
        SaveAllCmd        = new RelayCommand(async _ => await SaveAllAsync());

        AddPaymentCmd     = new RelayCommand(_ => AddPayment(),  _ => Selected != null);
        DeletePaymentCmd  = new RelayCommand(_ => DeletePayment(), _ => Selected?.Payments.Count != 0 == true);
        AddPhaseCmd       = new RelayCommand(_ => AddPhase(), _ => Selected != null);
        DeletePhaseCmd    = new RelayCommand(_ => DeletePhase(), _ => Selected?.Phases.Count != 0 == true);

        _itemsView = CollectionViewSource.GetDefaultView(Items);
        _itemsView.Filter = FilterContract;
        
        _ = LoadAsync();
    }
    private bool FilterContract(object obj)
    {
        if (obj is not Contract c)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var text = SearchText.Trim();
        var cmp = StringComparison.OrdinalIgnoreCase;

        if (c.Subject?.IndexOf(text, cmp) >= 0) return true;
        if (c.Note?.IndexOf(text, cmp) >= 0) return true;
        if (c.ContractId.ToString().Contains(text, cmp)) return true;
        if (c.DateSigned.ToString("dd.MM.yyyy").Contains(text, cmp)) return true;

        return false;
    }


    private void UpdateChildCommands()
    {
        DeleteSelectedCmd.RaiseCanExecuteChanged();
        AddPaymentCmd.RaiseCanExecuteChanged();
        DeletePaymentCmd.RaiseCanExecuteChanged();
        AddPhaseCmd.RaiseCanExecuteChanged();
        DeletePhaseCmd.RaiseCanExecuteChanged();
    }

    private async Task LoadAsync()
    {
        if (_db != null) await _db.DisposeAsync();
        _db = _factory.Create();

        Items.Clear(); Orgs.Clear(); Types.Clear(); Stages.Clear(); Vats.Clear(); PayTypes.Clear();

        var orgs = await _db.Organizations.OrderBy(x => x.Name).ToListAsync();
        foreach (var o in orgs) Orgs.Add(o);

        var types = await _db.ContractTypes.OrderBy(x => x.Name).ToListAsync();
        foreach (var t in types) Types.Add(t);

        var stages = await _db.Stages.OrderBy(x => x.Name).ToListAsync();
        foreach (var s in stages) Stages.Add(s);
        
        var vats = await _db.VatRates.ToListAsync();
        foreach (var v in vats.OrderBy(x => x.Rate ?? 0)) Vats.Add(v);

        
        var pays = await _db.PaymentTypes.OrderBy(x => x.Name).ToListAsync();
        foreach (var p in pays) PayTypes.Add(p);

        var contracts = await _db.Contracts
            .Include(c=>c.Phases)
            .Include(c=>c.Payments)
            .OrderByDescending(c => c.DateSigned)
            .ToListAsync();

        foreach (var c in contracts)
        {
            if (!Orgs.Any(o => o.OrgId == c.CustomerId))
                c.CustomerId = Orgs.FirstOrDefault()?.OrgId ?? 0;

            if (!Orgs.Any(o => o.OrgId == c.ContractorId))
                c.ContractorId = Orgs.Skip(1).FirstOrDefault()?.OrgId
                                 ?? Orgs.FirstOrDefault()?.OrgId ?? 0;

            Items.Add(c);
        }
        if (Items.Count > 0)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Selected = Items[0];
            });
        }

        _itemsView.Refresh();
    }

    private void AddContract()
    {
        var c = new Contract
        {
            DateSigned = DateTime.Today,
            CustomerId = Orgs.FirstOrDefault()?.OrgId ?? 0,
            ContractorId = Orgs.Skip(1).FirstOrDefault()?.OrgId ?? Orgs.FirstOrDefault()?.OrgId ?? 0,
            TypeId = Types.FirstOrDefault()?.TypeId ?? 0,
            StageId = Stages.FirstOrDefault()?.StageId ?? 0,
            VatId = Vats.FirstOrDefault()?.VatId ?? 0,
            DueDate = null,
            Subject = "",
            Note = ""
        };
        _db.Contracts.Add(c);
        Items.Insert(0, c);
        Selected = c;
    }

    private async Task DeleteSelectedAsync()
    {
        var item = Selected;
        if (item is null) return;

        if (item.ContractId == 0)
        {
            Items.Remove(item);
            Selected = null;
            return;
        }

        if (MessageBox.Show("Удалить договор и связанные этапы/оплаты?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _db.Contracts.Remove(item);
        await _db.SaveChangesAsync();
        Items.Remove(item);
        Selected = Items.FirstOrDefault();
    }

    private async Task SaveAllAsync()
    {
        await _db.SaveChangesAsync();
        MessageBox.Show("Сохранено.");
        await LoadAsync();
    }

    private void AddPayment()
    {
        if (Selected is null) return;
        var p = new Payment
        {
            ContractId = Selected.ContractId,
            PaymentDate = DateTime.Today,
            Amount = 0,
            PaymentTypeId = PayTypes.FirstOrDefault()?.PaymentTypeId ?? 0,
            DocumentNumber = ""
        };
        Selected.Payments.Add(p);
        _db.Payments.Add(p);
        UpdateChildCommands();
    }

    private void DeletePayment()
    {
        if (Selected is null) return;
        var p = Selected.Payments.LastOrDefault();
        if (p is null) return;

        _db.Payments.Remove(p);
        Selected.Payments.Remove(p);
        UpdateChildCommands();
    }

    
    private void AddPhase()
    {
        if (Selected is null) return;

        var nextNo = (Selected.Phases.Count != 0 ? Selected.Phases.Max(x => x.PhaseNum) : 0) + 1;

        var ph = new ContractPhase
        {
            ContractId = Selected.ContractId,
            PhaseNum = nextNo,
            DueDate = DateTime.Today,
            StageId = Stages.FirstOrDefault()?.StageId,
            Amount = 0,
            Advance = 0,
            Subject = ""
        };
        Selected.Phases.Add(ph);
        if (Selected.ContractId != 0)
            _db.ContractPhases.Add(ph);

        UpdateChildCommands();
    }


    
    private void DeletePhase()
    {
        if (Selected is null) return;

        var ph = Selected.Phases.OrderByDescending(x => x.PhaseNum).FirstOrDefault();
        if (ph is null) return;

        if (Selected.ContractId == 0 || ph.PhaseNum == 0)
        {
            Selected.Phases.Remove(ph);
            UpdateChildCommands();
            return;
        }

        _db.ContractPhases.Remove(ph);
        Selected.Phases.Remove(ph);

        UpdateChildCommands();
    }

}
