namespace Contracts.Models;

public sealed class Payment
{
    public int PaymentId { get; set; }
    public int ContractId { get; set; }
    public DateTime PaymentDate { get; set; }
    public double Amount { get; set; }
    public int PaymentTypeId { get; set; }
    public string? DocumentNumber { get; set; }

    public Contract? Contract { get; set; }
    public PaymentType? PaymentType { get; set; }
}