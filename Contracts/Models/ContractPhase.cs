namespace Contracts.Models;

public sealed class ContractPhase
{
    public int ContractId { get; set; }
    public int PhaseNum { get; set; }
    public DateTime? DueDate { get; set; }
    public int? StageId { get; set; }
    public double? Amount { get; set; }
    public double? Advance { get; set; }
    public string? Subject { get; set; }

    public Contract? Contract { get; set; }
    public Stage? Stage { get; set; }
}