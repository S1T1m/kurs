using System.Collections.ObjectModel;

namespace Contracts.Models;

public sealed class Contract
{
    public int ContractId { get; set; }
    public DateTime DateSigned { get; set; }
    public int CustomerId { get; set; }
    public int ContractorId { get; set; }
    public int TypeId { get; set; }
    public int StageId { get; set; }
    public int VatId { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Subject { get; set; }
    public string? Note { get; set; }

    public Organization? Customer { get; set; }
    public Organization? Contractor { get; set; }
    public ContractType? Type { get; set; }
    public Stage? Stage { get; set; }
    public VatRate? VatRate { get; set; }

    public ObservableCollection<ContractPhase> Phases { get; set; } = [];
    public ObservableCollection<Payment> Payments { get; set; } = [];
}