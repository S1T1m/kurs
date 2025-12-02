namespace Contracts.Models;

public sealed class Organization
{
    public int OrgId { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Inn { get; set; }
    public string? BankAccount { get; set; }
    public string? Bik { get; set; }
}