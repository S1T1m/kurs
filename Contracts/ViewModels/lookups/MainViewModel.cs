using Contracts.Data;
using Contracts.ViewModels.contracts;
using Contracts.ViewModels.lookups;

namespace Contracts.ViewModels;

public sealed class MainViewModel(DbContextFactory factory) : BaseViewModel
{
    public ContractsViewModel Contracts { get; } = new ContractsViewModel(factory);
    public OrganizationsViewModel Organizations { get; } = new OrganizationsViewModel(factory);
    public SimpleNameLookupViewModel ContractTypes { get; } = new SimpleNameLookupViewModel(factory, table: "contract_types", idColumn: "type_id");
    public SimpleNameLookupViewModel Stages { get; } = new SimpleNameLookupViewModel(factory, table: "stages", idColumn: "stage_id");
    public SimpleNameLookupViewModel PaymentTypes { get; } = new SimpleNameLookupViewModel(factory, table: "payment_types", idColumn: "payment_type_id");
    public VatRatesViewModel VatRates { get; } = new VatRatesViewModel(factory);
    public ReportsViewModel Reports { get; } = new ReportsViewModel(factory);
}