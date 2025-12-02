namespace Contracts.Models;

public sealed class VContractInfo
{
    public int ContractId { get; set; }
    public string? Customer { get; set; }
    public string? Contractor { get; set; }
    public string? ContractType { get; set; }
    public string? Stage { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Subject { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal AccountsReceivable { get; set; }
}

public sealed class VPaymentSchedule
{
    public int ContractId { get; set; }
    public string? ContractSubject { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public string? PaymentTypeName { get; set; }
    public string? DocumentNumber { get; set; }
}

public sealed class VPlanSchedule
{
    public int ContractId { get; set; }
    public string? ContractSubject { get; set; }
    public int PhaseNumber { get; set; }
    public DateTime? PhaseDueDate { get; set; }
    public decimal? PhaseAmount { get; set; }
    public decimal? AdvanceAmount { get; set; }
    public string? PhaseStage { get; set; }
}