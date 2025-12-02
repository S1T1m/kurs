using System;
using System.Data;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Contracts.ViewModels
{
    public sealed class ReportsViewModel : BaseViewModel
    {
        private readonly DbContextFactory _factory;

        public ObservableCollection<KeyValuePair<string,string>> ReportsList { get; } =
        [
            new("v_contract_info", "Сведения по договорам"),
            new("v_payment_schedule", "График оплат"),
            new("v_plan_schedule", "График этапов")
        ];

        private string _selectedReport = "v_contract_info";
        public string SelectedReport
        {
            get => _selectedReport;
            set => Set(ref _selectedReport, value);
        }

        private DateTime? _dateFrom, _dateTo;
        public DateTime? DateFrom { get => _dateFrom; set => Set(ref _dateFrom, value); }
        public DateTime? DateTo   { get => _dateTo;   set => Set(ref _dateTo,   value); }

        private DataView? _reportView;
        public DataView? ReportView
        {
            get => _reportView;
            private set => Set(ref _reportView, value);
        }

        private string _summaryText = "";
        public string SummaryText
        {
            get => _summaryText;
            set => Set(ref _summaryText, value);
        }

        public RelayCommand LoadReportCmd { get; }

        public ReportsViewModel(DbContextFactory factory)
        {
            _factory = factory;
            LoadReportCmd = new RelayCommand(async void (_) => await LoadReportAsync());
        }

        private async Task LoadReportAsync()
        {
            SummaryText = "";
            ReportView = null;

            await using var db = _factory.Create();
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                SelectedReport == "v_payment_schedule"
                    ? "SELECT  * FROM v_payment_schedule"
                    : $"SELECT * FROM {SelectedReport}";

            var dt = new DataTable();
            using (var reader = await cmd.ExecuteReaderAsync())
                dt.Load(reader);

        
            var dateCols = dt.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName.Contains("Дата", StringComparison.OrdinalIgnoreCase) ||
                            c.ColumnName.Contains("Date", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if ((DateFrom != null || DateTo != null) && dateCols.Count != 0)
            {
                foreach (var row in dt.Rows.Cast<DataRow>().ToList())
                {
                    foreach (var c in dateCols)
                    {
                        if (!DateTime.TryParse(Convert.ToString(row[c]), out var d)) continue;
                        if ((DateFrom == null || !(d < DateFrom)) && (DateTo == null || !(d > DateTo))) continue;
                        dt.Rows.Remove(row); break;
                    }
                }
            }

            foreach (var c in dateCols)
                foreach (DataRow row in dt.Rows)
                    if (DateTime.TryParse(Convert.ToString(row[c]), out var d))
                        row[c] = d.ToString("dd.MM.yyyy");
            var moneyCols = dt.Columns.Cast<DataColumn>()
                .Where(c =>
                    c.ColumnName.Contains("Сумма", StringComparison.OrdinalIgnoreCase) ||
                    c.ColumnName.Contains("Оплачено", StringComparison.OrdinalIgnoreCase) ||
                    c.ColumnName.Contains("Плановая", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (moneyCols.Count != 0)
            {
                SummaryText = "Итоги — " + string.Join("   ",
                    moneyCols.Select(c =>
                    {
                        var sum = dt.AsEnumerable()
                            .Select(r => Convert.ToString(r[c]))
                            .Where(s => double.TryParse(s, out _))
                            .Sum(s => Convert.ToDouble(s));

                        return $"{c.ColumnName}: {sum:N2}";
                    }));
            }

            ReportView = dt.DefaultView;
        }

    }
}
