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

        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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
    }
}