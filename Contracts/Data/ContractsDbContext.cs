using Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace Contracts.Data;

public sealed class ContractsDbContext(DbContextOptions<ContractsDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<ContractType> ContractTypes => Set<ContractType>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<VatRate> VatRates => Set<VatRate>();
    public DbSet<PaymentType> PaymentTypes => Set<PaymentType>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractPhase> ContractPhases => Set<ContractPhase>();
    public DbSet<Payment> Payments => Set<Payment>();
    
    public DbSet<VContractInfo> VContractInfos => Set<VContractInfo>();
    public DbSet<VPaymentSchedule> VPaymentSchedules => Set<VPaymentSchedule>();
    public DbSet<VPlanSchedule> VPlanSchedules => Set<VPlanSchedule>();
    

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.HasKey(x => x.OrgId);
            e.Property(x => x.OrgId).HasColumnName("org_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Address).HasColumnName("address");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.Inn).HasColumnName("inn");
            e.Property(x => x.BankAccount).HasColumnName("bank_account");
            e.Property(x => x.Bik).HasColumnName("bik");
        });

        b.Entity<ContractType>(e =>
        {
            e.ToTable("contract_types");
            e.HasKey(x => x.TypeId);
            e.Property(x => x.TypeId).HasColumnName("type_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
        });

        b.Entity<Stage>(e =>
        {
            e.ToTable("stages");
            e.HasKey(x => x.StageId);
            e.Property(x => x.StageId).HasColumnName("stage_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
        });

        b.Entity<VatRate>(e =>
        {
            e.ToTable("vat_rates");
            e.HasKey(x => x.VatId);
            e.Property(x => x.VatId).HasColumnName("vat_id");
            e.Property(x => x.Rate).HasColumnName("rate");
        });

        b.Entity<PaymentType>(e =>
        {
            e.ToTable("payment_types");
            e.HasKey(x => x.PaymentTypeId);
            e.Property(x => x.PaymentTypeId).HasColumnName("payment_type_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
        });

        b.Entity<Contract>(e =>
        {
            e.ToTable("contracts");
            e.HasKey(x => x.ContractId);
            e.Property(x => x.ContractId).HasColumnName("contract_id");
            e.Property(x => x.DateSigned).HasColumnName("date_signed");
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.ContractorId).HasColumnName("contractor_id");
            e.Property(x => x.TypeId).HasColumnName("type_id");
            e.Property(x => x.StageId).HasColumnName("stage_id");
            e.Property(x => x.VatId).HasColumnName("vat_id");
            e.Property(x => x.DueDate).HasColumnName("due_date");
            e.Property(x => x.Subject).HasColumnName("subject");
            e.Property(x => x.Note).HasColumnName("note");

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Contractor)
                .WithMany()
                .HasForeignKey(x => x.ContractorId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
            e.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId);
            e.HasOne(x => x.VatRate).WithMany().HasForeignKey(x => x.VatId);
        });

        b.Entity<ContractPhase>(e =>
        {
            e.ToTable("contract_phases");
            e.HasKey(x => new { x.ContractId, x.PhaseNum });
            e.Property(x => x.ContractId).HasColumnName("contract_id");
            e.Property(x => x.PhaseNum).HasColumnName("phase_num");
            e.Property(x => x.DueDate).HasColumnName("due_date");
            e.Property(x => x.StageId).HasColumnName("stage_id");
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.Advance).HasColumnName("advance");
            e.Property(x => x.Subject).HasColumnName("subject");

            e.HasOne(x => x.Contract)
                .WithMany(x => x.Phases)
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Stage)
                .WithMany()
                .HasForeignKey(x => x.StageId);
        });

        b.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.PaymentId).HasColumnName("payment_id");
            e.Property(x => x.ContractId).HasColumnName("contract_id");
            e.Property(x => x.PaymentDate).HasColumnName("payment_date");
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.PaymentTypeId).HasColumnName("payment_type_id");
            e.Property(x => x.DocumentNumber).HasColumnName("document_number");

            e.HasOne(x => x.Contract)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.PaymentType)
                .WithMany()
                .HasForeignKey(x => x.PaymentTypeId);
        });

        b.Entity<VContractInfo>(e =>
        {
            e.HasNoKey();
            e.ToView("v_contract_info");
            e.Property(p => p.ContractId)        .HasColumnName("Код_договора");
            e.Property(p => p.Customer)          .HasColumnName("Заказчик");
            e.Property(p => p.Contractor)        .HasColumnName("Исполнитель");
            e.Property(p => p.ContractType)      .HasColumnName("Тип_договора");
            e.Property(p => p.Stage)             .HasColumnName("Стадия");
            e.Property(p => p.SignedDate)        .HasColumnName("Дата_заключения");
            e.Property(p => p.DueDate)           .HasColumnName("Дата_исполнения");
            e.Property(p => p.Subject)           .HasColumnName("Тема");
            e.Property(p => p.PlannedAmount)     .HasColumnName("Плановая_сумма");
            e.Property(p => p.PaidAmount)        .HasColumnName("Оплачено");
            e.Property(p => p.AccountsReceivable) .HasColumnName("Дебиторская_задолженность");
        });
        
        b.Entity<VPaymentSchedule>(e =>
        {
            e.HasNoKey();
            e.ToView("v_payment_schedule");
            e.Property(p => p.ContractId)     .HasColumnName("Код_договора");
            e.Property(p => p.ContractSubject).HasColumnName("Тема_договора");
            e.Property(p => p.PaymentDate)    .HasColumnName("Дата_оплаты");
            e.Property(p => p.PaymentAmount)  .HasColumnName("Сумма_оплаты");
            e.Property(p => p.PaymentTypeName).HasColumnName("Вид_оплаты");
            e.Property(p => p.DocumentNumber) .HasColumnName("№_платежного_документа");
        });
        
        b.Entity<VPlanSchedule>(e =>
        {
            e.HasNoKey();
            e.ToView("v_plan_schedule");
            e.Property(p => p.ContractId)     .HasColumnName("Код_договора");
            e.Property(p => p.ContractSubject).HasColumnName("Тема_договора");
            e.Property(p => p.PhaseNumber)    .HasColumnName("Номер_этапа");
            e.Property(p => p.PhaseDueDate)   .HasColumnName("Дата_исполнения_этапа");
            e.Property(p => p.PhaseAmount)    .HasColumnName("Сумма_этапа");
            e.Property(p => p.AdvanceAmount)  .HasColumnName("Сумма_аванса");
            e.Property(p => p.PhaseStage)     .HasColumnName("Стадия_этапа");
        });
    }
}
