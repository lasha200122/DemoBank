using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class LoanService : ILoanService
{
    private readonly DemoBankContext _context;
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly INotificationHelper _notificationHelper;

    // Loan configuration
    private const decimal MIN_LOAN_AMOUNT = 50_000;
    private const decimal MAX_LOAN_AMOUNT = 1_000_000;
    private const decimal BASE_INTEREST_RATE = 5.0m; // 5% base APR
    private const int MIN_TERM_MONTHS = 6;
    private const int MAX_TERM_MONTHS = 72;
    private const decimal LATE_PAYMENT_FEE = 25;
    private const int GRACE_PERIOD_DAYS = 5;

    public LoanService(
        DemoBankContext context,
        IAccountService accountService,
        ITransactionService transactionService,
        INotificationHelper notificationHelper)
    {
        _context = context;
        _accountService = accountService;
        _transactionService = transactionService;
        _notificationHelper = notificationHelper;
    }

    public async Task<LoanApplicationResultDto> ApplyForLoanAsync(Guid userId, LoanApplicationDto applicationDto)
    {
        // Validate loan amount
        if (applicationDto.Amount < MIN_LOAN_AMOUNT || applicationDto.Amount > MAX_LOAN_AMOUNT)
            throw new InvalidOperationException($"Loan amount must be between ${MIN_LOAN_AMOUNT:N0} and ${MAX_LOAN_AMOUNT:N0}");

        // Validate term
        if (applicationDto.TermMonths < MIN_TERM_MONTHS || applicationDto.TermMonths > MAX_TERM_MONTHS)
            throw new InvalidOperationException($"Loan term must be between {MIN_TERM_MONTHS} and {MAX_TERM_MONTHS} months");

        // Check eligibility
        var eligibility = await CheckLoanEligibilityAsync(userId, applicationDto.Amount);

        if (!eligibility.IsEligible)
            throw new InvalidOperationException($"Loan application denied: {string.Join(", ", eligibility.Reasons)}");

        // Calculate interest rate based on credit score and amount
        var interestRate = CalculateInterestRate(applicationDto.Amount, applicationDto.TermMonths, eligibility.CreditScore);

        // Calculate monthly payment
        var monthlyPayment = await CalculateMonthlyPaymentAsync(
            applicationDto.Amount,
            interestRate,
            applicationDto.TermMonths
        );

        // Create loan application
        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = applicationDto.Amount,
            InterestRate = interestRate,
            TermMonths = applicationDto.TermMonths,
            MonthlyPayment = monthlyPayment,
            TotalPaid = 0,
            RemainingBalance = applicationDto.Amount,
            Status = LoanStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        // Send notification
        await _notificationHelper.CreateNotification(
            userId,
            "Loan Application Submitted",
            $"Your loan application for ${applicationDto.Amount:N2} has been submitted and is under review.",
            NotificationType.Info
        );

        // Notify admin
        var admins = await _context.Users
            .Where(u => u.Role == UserRole.Admin && u.Status == Status.Active)
            .ToListAsync();

        foreach (var admin in admins)
        {
            await _notificationHelper.CreateNotification(
                admin.Id,
                "New Loan Application",
                $"New loan application for ${applicationDto.Amount:N2} requires review.",
                NotificationType.Info
            );
        }

        return new LoanApplicationResultDto
        {
            Success = true,
            LoanId = loan.Id,
            Status = loan.Status.ToString(),
            Amount = loan.Amount,
            InterestRate = loan.InterestRate,
            TermMonths = loan.TermMonths,
            MonthlyPayment = loan.MonthlyPayment,
            TotalRepayment = monthlyPayment * applicationDto.TermMonths,
            Message = "Loan application submitted successfully. Pending approval."
        };
    }

    public async Task<LoanDto> GetLoanByIdAsync(Guid loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan == null)
            return null;

        return MapToLoanDto(loan);
    }

    public async Task<List<LoanDto>> GetUserLoansAsync(Guid userId)
    {
        var loans = await _context.Loans
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return loans.Select(MapToLoanDto).ToList();
    }

    public async Task<List<LoanDto>> GetPendingLoansAsync()
    {
        var loans = await _context.Loans
            .Include(l => l.User)
            .Where(l => l.Status == LoanStatus.Pending)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        return loans.Select(MapToLoanDto).ToList();
    }

    public async Task<LoanApprovalResultDto> ApproveLoanAsync(Guid loanId, LoanApprovalDto approvalDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var loan = await _context.Loans
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null)
                throw new InvalidOperationException("Loan not found");

            if (loan.Status != LoanStatus.Pending)
                throw new InvalidOperationException("Only pending loans can be approved");

            // Update loan status
            loan.Status = LoanStatus.Approved;
            loan.ApprovedDate = DateTime.UtcNow;
            loan.NextPaymentDate = DateTime.UtcNow.AddMonths(1);

            // Override interest rate if provided
            if (approvalDto.OverrideInterestRate.HasValue)
            {
                loan.InterestRate = approvalDto.OverrideInterestRate.Value;
                loan.MonthlyPayment = await CalculateMonthlyPaymentAsync(
                    loan.Amount,
                    loan.InterestRate,
                    loan.TermMonths
                );
            }

            // Get disbursement account
            Account disbursementAccount;
            if (approvalDto.DisbursementAccountId.HasValue)
            {
                disbursementAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == approvalDto.DisbursementAccountId.Value);
            }
            else
            {
                // Use user's primary EUR account
                disbursementAccount = await _accountService.GetPriorityAccountAsync(loan.UserId, "EUR");
            }

            if (disbursementAccount == null)
                throw new InvalidOperationException("No valid account for loan disbursement");

            // Disburse loan amount
            disbursementAccount.Balance += loan.Amount;
            disbursementAccount.UpdatedAt = DateTime.UtcNow;

            // Create disbursement transaction
            var disbursementTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = disbursementAccount.Id,
                Type = TransactionType.Deposit,
                Amount = loan.Amount,
                Currency = "EUR",
                AmountInAccountCurrency = loan.Amount,
                Description = $"Loan disbursement - Loan #{loan.Id.ToString().Substring(0, 8)}",
                BalanceAfter = disbursementAccount.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(disbursementTransaction);

            // Update loan status to active
            loan.Status = LoanStatus.Active;

            await _context.SaveChangesAsync();

            // Send notification to borrower
            await _notificationHelper.CreateNotification(
                loan.UserId,
                "Loan Approved",
                $"Your loan for ${loan.Amount:N2} has been approved and disbursed to account {disbursementAccount.AccountNumber}. " +
                $"Monthly payment: ${loan.MonthlyPayment:N2}. First payment due: {loan.NextPaymentDate:yyyy-MM-dd}",
                NotificationType.Success
            );

            await transaction.CommitAsync();

            return new LoanApprovalResultDto
            {
                Success = true,
                LoanId = loan.Id,
                Status = loan.Status.ToString(),
                DisbursedAmount = loan.Amount,
                DisbursementAccount = disbursementAccount.AccountNumber,
                MonthlyPayment = loan.MonthlyPayment,
                FirstPaymentDate = loan.NextPaymentDate.Value,
                Message = "Loan approved and disbursed successfully"
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<LoanApprovalResultDto> RejectLoanAsync(Guid loanId, string reason)
    {
        var loan = await _context.Loans
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan == null)
            throw new InvalidOperationException("Loan not found");

        if (loan.Status != LoanStatus.Pending)
            throw new InvalidOperationException("Only pending loans can be rejected");

        loan.Status = LoanStatus.Rejected;

        await _context.SaveChangesAsync();

        // Send notification to borrower
        await _notificationHelper.CreateNotification(
            loan.UserId,
            "Loan Application Rejected",
            $"Your loan application for ${loan.Amount:N2} has been rejected. Reason: {reason}",
            NotificationType.Warning
        );

        return new LoanApprovalResultDto
        {
            Success = false,
            LoanId = loan.Id,
            Status = loan.Status.ToString(),
            Message = $"Loan rejected: {reason}"
        };
    }

    public async Task<PaymentScheduleDto> GetPaymentScheduleAsync(Guid loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan == null)
            return null;

        var schedule = new PaymentScheduleDto
        {
            LoanId = loan.Id,
            TotalAmount = loan.Amount,
            MonthlyPayment = loan.MonthlyPayment,
            InterestRate = loan.InterestRate,
            TermMonths = loan.TermMonths,
            RemainingBalance = loan.RemainingBalance,
            NextPaymentDate = loan.NextPaymentDate,
            Payments = new List<PaymentScheduleItemDto>()
        };

        // Calculate payment schedule
        var balance = loan.Amount - loan.TotalPaid;
        var monthlyInterestRate = loan.InterestRate / 100 / 12;
        var paymentDate = loan.ApprovedDate;

        for (int month = 1; month <= loan.TermMonths; month++)
        {
            paymentDate = paymentDate.AddMonths(1);

            // Check if payment was made
            var payment = loan.Payments.FirstOrDefault(p =>
                p.PaymentDate.Year == paymentDate.Year &&
                p.PaymentDate.Month == paymentDate.Month);

            var interestAmount = balance * monthlyInterestRate;
            var principalAmount = loan.MonthlyPayment - interestAmount;

            if (principalAmount > balance)
                principalAmount = balance;

            schedule.Payments.Add(new PaymentScheduleItemDto
            {
                PaymentNumber = month,
                DueDate = paymentDate,
                Amount = loan.MonthlyPayment,
                PrincipalAmount = principalAmount,
                InterestAmount = interestAmount,
                RemainingBalance = balance - principalAmount,
                Status = payment != null ? "Paid" :
                        paymentDate < DateTime.UtcNow ? "Overdue" : "Pending",
                PaidDate = payment?.PaymentDate
            });

            balance -= principalAmount;

            if (balance <= 0)
                break;
        }

        return schedule;
    }

    public async Task<LoanPaymentResultDto> MakeLoanPaymentAsync(Guid userId, Guid loanId, decimal amount)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var loan = await _context.Loans
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null)
                throw new InvalidOperationException("Loan not found");

            if (loan.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to pay this loan");

            if (loan.Status != LoanStatus.Active)
                throw new InvalidOperationException("Loan is not active");

            if (amount <= 0)
                throw new InvalidOperationException("Payment amount must be positive");

            // Get user's priority EUR account
            var paymentAccount = await _accountService.GetPriorityAccountAsync(userId, "EUR");

            if (paymentAccount == null)
                throw new InvalidOperationException("No EUR account found for payment");

            if (paymentAccount.Balance < amount)
                throw new InvalidOperationException("Insufficient balance for loan payment");

            // Calculate interest and principal
            var monthlyInterestRate = loan.InterestRate / 100 / 12;
            var interestAmount = loan.RemainingBalance * monthlyInterestRate;
            var principalAmount = amount - interestAmount;

            if (principalAmount < 0)
            {
                // Payment doesn't cover interest
                interestAmount = amount;
                principalAmount = 0;
            }

            // Update loan
            loan.TotalPaid += amount;
            loan.RemainingBalance -= principalAmount;

            if (loan.RemainingBalance <= 0)
            {
                loan.Status = LoanStatus.PaidOff;
                loan.RemainingBalance = 0;
            }
            else
            {
                loan.NextPaymentDate = DateTime.UtcNow.AddMonths(1);
            }

            // Create payment record
            var payment = new LoanPayment
            {
                Id = Guid.NewGuid(),
                LoanId = loan.Id,
                Amount = amount,
                PrincipalAmount = principalAmount,
                InterestAmount = interestAmount,
                PaymentDate = DateTime.UtcNow
            };

            _context.LoanPayments.Add(payment);

            // Process payment from account
            paymentAccount.Balance -= amount;
            paymentAccount.UpdatedAt = DateTime.UtcNow;

            // Create transaction
            var paymentTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = paymentAccount.Id,
                Type = TransactionType.LoanPayment,
                Amount = amount,
                Currency = "EUR",
                AmountInAccountCurrency = amount,
                Description = $"Loan payment - Loan #{loan.Id.ToString().Substring(0, 8)}",
                BalanceAfter = paymentAccount.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(paymentTransaction);

            await _context.SaveChangesAsync();

            // Send notification
            var message = loan.Status == LoanStatus.PaidOff
                ? $"Congratulations! Your loan has been fully paid off."
                : $"Payment of ${amount:N2} received. Remaining balance: ${loan.RemainingBalance:N2}";

            await _notificationHelper.CreateNotification(
                userId,
                "Loan Payment Processed",
                message,
                NotificationType.Success
            );

            await transaction.CommitAsync();

            return new LoanPaymentResultDto
            {
                Success = true,
                PaymentId = payment.Id,
                LoanId = loan.Id,
                AmountPaid = amount,
                PrincipalPaid = principalAmount,
                InterestPaid = interestAmount,
                RemainingBalance = loan.RemainingBalance,
                NextPaymentDate = loan.NextPaymentDate,
                LoanStatus = loan.Status.ToString(),
                Message = message
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<LoanPaymentResultDto> ProcessScheduledPaymentAsync(Guid loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan == null)
            throw new InvalidOperationException("Loan not found");

        return await MakeLoanPaymentAsync(loan.UserId, loanId, loan.MonthlyPayment);
    }

    public async Task<List<LoanPaymentHistoryDto>> GetPaymentHistoryAsync(Guid loanId)
    {
        var payments = await _context.LoanPayments
            .Where(p => p.LoanId == loanId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new LoanPaymentHistoryDto
            {
                PaymentId = p.Id,
                Amount = p.Amount,
                PrincipalAmount = p.PrincipalAmount,
                InterestAmount = p.InterestAmount,
                PaymentDate = p.PaymentDate
            })
            .ToListAsync();

        return payments;
    }

    public async Task<LoanSummaryDto> GetLoanSummaryAsync(Guid userId)
    {
        var loans = await _context.Loans
            .Where(l => l.UserId == userId)
            .ToListAsync();

        var activeLoans = loans.Where(l => l.Status == LoanStatus.Active).ToList();

        return new LoanSummaryDto
        {
            TotalLoans = loans.Count,
            ActiveLoans = activeLoans.Count,
            TotalBorrowed = loans.Where(l => 
                l.Status == LoanStatus.Active || 
                l.Status == LoanStatus.Approved || 
                l.Status == LoanStatus.PaidOff ||
                l.Status == LoanStatus.Defaulted).Sum(l => l.Amount),
            TotalPaid = loans.Sum(l => l.TotalPaid),
            TotalRemaining = activeLoans.Sum(l => l.RemainingBalance),
            MonthlyPaymentsDue = activeLoans.Sum(l => l.MonthlyPayment),
            NextPaymentDate = activeLoans
                .Where(l => l.NextPaymentDate.HasValue)
                .Min(l => l.NextPaymentDate)
        };
    }

    public async Task<decimal> CalculateMonthlyPaymentAsync(decimal amount, decimal interestRate, int termMonths)
    {
        // M = P * (r(1+r)^n) / ((1+r)^n - 1)
        // Where:
        // M = Monthly payment
        // P = Principal amount
        // r = Monthly interest rate
        // n = Number of payments

        if (interestRate == 0)
            return amount / termMonths;

        var monthlyRate = (double)(interestRate / 100 / 12);
        var n = termMonths;

        var numerator = (double)amount * monthlyRate * Math.Pow(1 + monthlyRate, n);
        var denominator = Math.Pow(1 + monthlyRate, n) - 1;

        return (decimal)(numerator / denominator);
    }

    public async Task<LoanEligibilityDto> CheckLoanEligibilityAsync(Guid userId, decimal requestedAmount)
    {
        var user = await _context.Users
            .Include(u => u.Accounts)
            .Include(u => u.Loans)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("User not found");

        var eligibility = new LoanEligibilityDto
        {
            IsEligible = true,
            RequestedAmount = requestedAmount,
            MaxEligibleAmount = MAX_LOAN_AMOUNT,
            Reasons = new List<string>()
        };


        // Check account balance (minimum $100 in any account)
        var totalBalance = user.Accounts.Where(a => a.IsActive).Sum(a => a.Balance);
        if (totalBalance < 100)
        {
            eligibility.IsEligible = false;
            eligibility.Reasons.Add("Minimum account balance of $100 required");
        }

        // Simulate credit score (600-850)
        var random = new Random(userId.GetHashCode());
        eligibility.CreditScore = random.Next(600, 851);

        if (eligibility.CreditScore < 650)
        {
            eligibility.IsEligible = false;
            eligibility.Reasons.Add("Credit score too low");
        }

        return eligibility;
    }

    public async Task<bool> SetAutoPaymentAsync(Guid loanId, Guid accountId, bool enable)
    {
        var loan = await _context.Loans.FindAsync(loanId);
        if (loan == null)
            return false;

        // This would typically update a field for auto-payment settings
        // For now, we'll just return true
        await _notificationHelper.CreateNotification(
            loan.UserId,
            enable ? "Auto-payment Enabled" : "Auto-payment Disabled",
            $"Auto-payment for loan #{loan.Id.ToString().Substring(0, 8)} has been {(enable ? "enabled" : "disabled")}",
            NotificationType.Info
        );

        return true;
    }

    private decimal CalculateInterestRate(decimal amount, int termMonths, int creditScore)
    {
        var baseRate = BASE_INTEREST_RATE;

        // Adjust based on credit score
        if (creditScore >= 750)
            baseRate -= 1.0m;
        else if (creditScore >= 700)
            baseRate -= 0.5m;
        else if (creditScore < 650)
            baseRate += 2.0m;

        // Adjust based on loan amount
        if (amount >= 50000)
            baseRate += 0.5m;
        else if (amount <= 5000)
            baseRate -= 0.25m;

        // Adjust based on term
        if (termMonths >= 48)
            baseRate += 0.5m;
        else if (termMonths <= 12)
            baseRate -= 0.25m;

        // Ensure rate is within bounds
        return Math.Max(3.0m, Math.Min(15.0m, baseRate));
    }

    private LoanDto MapToLoanDto(Loan loan)
    {
        return new LoanDto
        {
            Id = loan.Id,
            Amount = loan.Amount,
            InterestRate = loan.InterestRate,
            TermMonths = loan.TermMonths,
            MonthlyPayment = loan.MonthlyPayment,
            TotalPaid = loan.TotalPaid,
            RemainingBalance = loan.RemainingBalance,
            Status = loan.Status.ToString(),
            ApprovedDate = loan.ApprovedDate,
            NextPaymentDate = loan.NextPaymentDate,
            CreatedAt = loan.CreatedAt,
            BorrowerName = loan.User != null ? $"{loan.User.FirstName} {loan.User.LastName}" : null
        };
    }
}