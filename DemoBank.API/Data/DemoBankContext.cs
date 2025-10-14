using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Data;

public class DemoBankContext : DbContext
{
    public DemoBankContext(DbContextOptions<DemoBankContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<LoanPayment> LoanPayments { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
    public DbSet<ExchangeRateHistory> ExchangeRateHistories { get; set; }
    public DbSet<FavoriteCurrencyPair> FavoriteCurrencyPairs { get; set; }
    public DbSet<ExchangeRateAlert> ExchangeRateAlerts { get; set; }
    public DbSet<Investment> Investments { get; set; }
    public DbSet<InvestmentPlan> InvestmentPlans { get; set; }
    public DbSet<InvestmentReturn> InvestmentReturns { get; set; }
    public DbSet<InvestmentRate> InvestmentRates { get; set; }
    public DbSet<InvestmentTransaction> InvestmentTransactions { get; set; }
    public DbSet<BankingDetails> BankingDetails { get; set; }
    public DbSet<ClientInvestment> ClientInvestment { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Role)
                .HasConversion<string>();

            entity.HasMany(e => e.Accounts)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Loans)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Notifications)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Settings)
                .WithOne(e => e.User)
                .HasForeignKey<UserSettings>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add these relationships:
            entity.HasMany(e => e.Investments)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Invoices)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AccountNumber).IsUnique();

            entity.Property(e => e.Type)
                .HasConversion<string>();

            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18,2)");

            entity.HasMany(e => e.Transactions)
                .WithOne(e => e.Account)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .HasConversion<string>();

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.ExchangeRate)
                .HasColumnType("decimal(18,6)");

            entity.Property(e => e.AmountInAccountCurrency)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.BalanceAfter)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.ToAccount)
                .WithMany()
                .HasForeignKey(e => e.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Currency configuration
        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.ExchangeRateToUSD)
                .HasColumnType("decimal(18,6)");
        });

        // Loan configuration
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.InterestRate)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.MonthlyPayment)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalPaid)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.RemainingBalance)
                .HasColumnType("decimal(18,2)");

            entity.HasMany(e => e.Payments)
                .WithOne(e => e.Loan)
                .HasForeignKey(e => e.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LoanPayment configuration
        modelBuilder.Entity<LoanPayment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.PrincipalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.InterestAmount)
                .HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            // Remove the duplicate User relationship configuration since it's now in User entity
            // The relationship is already configured from the User side
        });
        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .HasConversion<string>();
        });

        // UserSettings configuration
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DailyTransferLimit)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.DailyWithdrawalLimit)
                .HasColumnType("decimal(18,2)");
        });

        // ExchangeRateHistory configuration
        modelBuilder.Entity<ExchangeRateHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FromCurrency, e.ToCurrency, e.RecordedAt });

            entity.Property(e => e.Rate)
                .HasColumnType("decimal(18,6)");
        });

        // FavoriteCurrencyPair configuration
        modelBuilder.Entity<FavoriteCurrencyPair>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.FromCurrency, e.ToCurrency }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExchangeRateAlert configuration
        modelBuilder.Entity<ExchangeRateAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.IsActive });

            entity.Property(e => e.TargetRate)
                .HasColumnType("decimal(18,6)");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Investment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.CustomROI)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.BaseROI)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.ProjectedReturn)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalPaidOut)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MinimumBalance)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.Term)
                .HasConversion<string>();

            entity.Property(e => e.PayoutFrequency)
                .HasConversion<string>();

            // Remove the duplicate User relationship configuration since it's now in User entity
            // The relationship is already configured from the User side

            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Investments)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Returns)
                .WithOne(r => r.Investment)
                .HasForeignKey(r => r.InvestmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.Investment)
                .HasForeignKey(t => t.InvestmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        // InvestmentPlan configuration
        modelBuilder.Entity<InvestmentPlan>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MinimumInvestment)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MaximumInvestment)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.BaseROI)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.EarlyWithdrawalPenalty)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.VolatilityIndex)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.Type)
                .HasConversion<string>();

            entity.Property(e => e.DefaultPayoutFrequency)
                .HasConversion<string>();

            entity.Property(e => e.RiskLevel)
                .HasConversion<string>();
        });

        // InvestmentReturn configuration
        modelBuilder.Entity<InvestmentReturn>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.InterestAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.PrincipalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Type)
                .HasConversion<string>();

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.HasOne(e => e.Investment)
                .WithMany(i => i.Returns)
                .HasForeignKey(e => e.InvestmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // InvestmentRate configuration
        modelBuilder.Entity<InvestmentRate>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Rate)
                .HasColumnType("decimal(5,2)");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            entity.HasOne(e => e.Plan)
                .WithMany()
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            entity.HasIndex(e => new { e.UserId, e.PlanId, e.RateType })
                .IsUnique();
        });

        // InvestmentTransaction configuration
        modelBuilder.Entity<InvestmentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.BalanceBefore)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.BalanceAfter)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Type)
                .HasConversion<string>();

            entity.HasOne(e => e.Investment)
                .WithMany(i => i.Transactions)
                .HasForeignKey(e => e.InvestmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BankingDetails>(entity =>
        {

            entity.HasKey(e => e.Id);
            // Indexes
            entity.HasIndex(e => e.UserId);

            // Columns
            entity.Property(e => e.BeneficialName)
                .HasMaxLength(50);

            entity.Property(e => e.IBAN)
                .HasMaxLength(34);

            entity.Property(e => e.Reference)
                .HasMaxLength(50);

            entity.Property(e => e.BIC)
                .HasMaxLength(12);

            entity.HasOne(e => e.User)
                .WithMany(u => u.BankingDetails)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClientInvestment>(entity =>
        {

            entity.HasKey(e => e.Id);
            // Indexes
            entity.HasIndex(e => e.UserId);

            // Columns
            entity.Property(e => e.YearlyReturn)
                   .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MonthlyReturn)
                   .HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.User)
                .WithMany(u => u.ClientInvestment)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}