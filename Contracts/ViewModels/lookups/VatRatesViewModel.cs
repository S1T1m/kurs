using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Contracts.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Contracts.ViewModels.lookups
{
    public sealed class VatRateItem
    {
        public int Id { get; set; }
        public decimal? Rate { get; set; }
    }

    public sealed class VatRatesViewModel : BaseViewModel
    {
        private readonly DbContextFactory _factory;

        public ObservableCollection<VatRateItem> Items { get; } = [];

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
        
        private VatRateItem? _selected;
        public VatRateItem? Selected
        {
            get => _selected;
            set
            {
                Set(ref _selected, value);
                _deleteCmd.RaiseCanExecuteChanged();
            }
        }

        private string? _newRate;
        public string? NewRate
        {
            get => _newRate;
            set
            {
                Set(ref _newRate, value);
                _addCmd.RaiseCanExecuteChanged();
            }
        }


        private readonly RelayCommand _addCmd;
        public RelayCommand AddCmd => _addCmd;

        private readonly RelayCommand _refreshCmd;
        public RelayCommand RefreshCmd => _refreshCmd;

        private readonly RelayCommand _deleteCmd;
        public RelayCommand DeleteCmd => _deleteCmd;

        private readonly RelayCommand _saveCmd;
        public RelayCommand SaveCmd => _saveCmd;


        public VatRatesViewModel(DbContextFactory factory)
        {
            _factory = factory;

            _itemsView = CollectionViewSource.GetDefaultView(Items);
            _itemsView.Filter = FilterItem;
            
            _refreshCmd = new RelayCommand(async _ => await LoadAsync());

            _addCmd = new RelayCommand(
                async _ => await AddAsync(),
                _ => CanAdd()
            );

            _deleteCmd = new RelayCommand(
                async _ => await DeleteAsync(),
                _ => Selected != null
            );

            _saveCmd = new RelayCommand(async _ => await SaveAsync());

            _ = LoadAsync();
        }

        private bool FilterItem(object obj)
        {
            if (obj is not VatRateItem item)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var text = SearchText.Trim();
            var s = item.Rate?.ToString();
            return s != null && s.Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        
        private bool CanAdd()
        {
            if (string.IsNullOrWhiteSpace(NewRate))
                return false;

            var s = NewRate.Replace('.', ',');

            return decimal.TryParse(s, out var rate)
                   && rate is >= 0 and <= 100;
        }



        private async Task LoadAsync()
        {
            Items.Clear();

            await using var db = _factory.Create();
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT vat_id, rate FROM vat_rates ORDER BY rate";

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                Items.Add(new VatRateItem
                {
                    Id = r.GetInt32(0),
                    Rate = r.IsDBNull(1) ? null : r.GetDecimal(1)
                });
            }
            _itemsView.Refresh();
        }


        private async Task AddAsync()
        {
            var s = NewRate?.Replace('.', ',');

            if (!decimal.TryParse(s, out var rate) || rate < 0 || rate > 100)
            {
                MessageBox.Show("Введите корректную ставку НДС (0–100)");
                return;
            }

            await using var db = _factory.Create();
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO vat_rates(rate) VALUES ($r); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$r", rate);

            long id = (long)await cmd.ExecuteScalarAsync();

            Items.Add(new VatRateItem
            {
                Id = (int)id,
                Rate = rate
            });

            NewRate = "";
        }



        private async Task DeleteAsync()
        {
            if (Selected is null) return;
            await using var db = _factory.Create();
            try
            {
                
                var conn = (SqliteConnection)db.Database.GetDbConnection();
                await conn.OpenAsync();

                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM vat_rates WHERE vat_id=$id";
                cmd.Parameters.AddWithValue("$id", Selected.Id);
                await cmd.ExecuteNonQueryAsync();

                Items.Remove(Selected);
                Selected = null;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                MessageBox.Show(
                "Нельзя удалить ставку НДС: она используется в договорах.",
                "Удаление ставки НДС",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            }
        }


        private async Task SaveAsync()
        {
            await using var db = _factory.Create();
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            foreach (var item in Items)
            {
                if (item.Rate == null)
                    continue;

                if (item.Id == 0)
                {
                    var insert = conn.CreateCommand();
                    insert.CommandText = "INSERT INTO vat_rates(rate) VALUES ($r); SELECT last_insert_rowid();";
                    insert.Parameters.AddWithValue("$r", item.Rate!);

                    var newId = (long)await insert.ExecuteScalarAsync();
                    item.Id = (int)newId;  
                }
                else
                {
                    var update = conn.CreateCommand();
                    update.CommandText = "UPDATE vat_rates SET rate=$r WHERE vat_id=$id";
                    update.Parameters.AddWithValue("$r", item.Rate!);
                    update.Parameters.AddWithValue("$id", item.Id);
                    await update.ExecuteNonQueryAsync();
                }
            }
            MessageBox.Show("Успешно сохранено.", "Уведомление",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}
