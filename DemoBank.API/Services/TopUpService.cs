using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class TopUpService : ITopUpService
{
    private readonly DemoBankContext _context;
    private readonly IAccountService _accountService;
    private readonly ICurrencyService _currencyService;
    private readonly INotificationHelper _notificationHelper;
    private readonly ILogger<TopUpService> _logger;

    // Fee structure for different payment methods
    private readonly Dictionary<PaymentMethod, (decimal percentage, decimal minimum, decimal maximum)> _feeStructure = new()
    {
        { PaymentMethod.CreditCard, (0.029m, 0.50m, 100m) },      // 2.9% + min $0.50, max $100
        { PaymentMethod.BankTransfer, (0.005m, 1.00m, 25m) },     // 0.5% + min $1.00, max $25
    };

    // Daily and monthly limits
    private const decimal DAILY_TOPUP_LIMIT = 10000m;
    private const decimal MONTHLY_TOPUP_LIMIT = 50000m;
    private const decimal MIN_TOPUP_AMOUNT = 10m;
    private const decimal MAX_TOPUP_AMOUNT = 5000m;

    public TopUpService(
        DemoBankContext context,
        IAccountService accountService,
        ICurrencyService currencyService,
        INotificationHelper notificationHelper,
        ILogger<TopUpService> logger)
    {
        _context = context;
        _accountService = accountService;
        _currencyService = currencyService;
        _notificationHelper = notificationHelper;
        _logger = logger;
    }

    public async Task<TopUpResultDto> ProcessTopUpAsync(Guid userId, AccountTopUpDto topUpDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate account ownership
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == topUpDto.AccountId);

            if (account == null)
                throw new InvalidOperationException("Account not found");

            if (account.UserId != userId)
                throw new UnauthorizedAccessException("You don't own this account");

            if (!account.IsActive)
                throw new InvalidOperationException("Account is not active");

            // Validate amount
            if (topUpDto.Amount < MIN_TOPUP_AMOUNT || topUpDto.Amount > MAX_TOPUP_AMOUNT)
                throw new InvalidOperationException($"Top-up amount must be between ${MIN_TOPUP_AMOUNT:N2} and ${MAX_TOPUP_AMOUNT:N2}");

            // Validate currency
            var currency = await _currencyService.GetCurrencyAsync(topUpDto.Currency);
            if (currency == null)
                throw new InvalidOperationException($"Currency {topUpDto.Currency} is not supported");

            // Check top-up limits
            if (!await ValidateTopUpLimitAsync(userId, topUpDto.Amount))
            {
                var limits = await GetTopUpLimitsAsync(userId);
                throw new InvalidOperationException(
                    $"Top-up would exceed limits. Daily remaining: ${limits.RemainingToday:N2}, Monthly remaining: ${limits.RemainingThisMonth:N2}");
            }

            // Validate payment method
            //var validationResult = await ValidatePaymentMethodAsync(new ValidatePaymentMethodDto
            //{
            //    PaymentMethod = topUpDto.PaymentMethod,
            //    CardDetails = topUpDto.CardDetails,
            //    BankDetails = topUpDto.BankDetails,
            //    PayPalDetails = topUpDto.PayPalDetails
            //});

            //if (!validationResult.IsValid)
            //    throw new InvalidOperationException($"Payment validation failed: {string.Join(", ", validationResult.Errors)}");

            // Calculate fees
            var fee = CalculateProcessingFee(topUpDto.Amount, topUpDto.PaymentMethod);
            var totalCharged = topUpDto.Amount + fee;

            // Process payment with external payment provider (simulated)
            var paymentResult = await ProcessPaymentAsync(topUpDto);
            if (!paymentResult.Success)
                throw new InvalidOperationException($"Payment processing failed: {paymentResult.Message}");

            // Calculate amount in account currency if different
            decimal amountInAccountCurrency = topUpDto.Amount;
            decimal? exchangeRate = null;

            if (topUpDto.Currency.ToUpper() != account.Currency)
            {
                exchangeRate = await _currencyService.GetExchangeRateAsync(
                    topUpDto.Currency,
                    account.Currency
                );
                amountInAccountCurrency = await _currencyService.ConvertCurrencyAsync(
                    topUpDto.Amount,
                    topUpDto.Currency,
                    account.Currency
                );
            }

            // Update account balance
            account.Balance += amountInAccountCurrency;
            account.UpdatedAt = DateTime.UtcNow;

            // Create transaction record
            var topUpTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = TransactionType.Deposit,
                Amount = topUpDto.Amount,
                Currency = topUpDto.Currency.ToUpper(),
                ExchangeRate = exchangeRate,
                AmountInAccountCurrency = amountInAccountCurrency,
                Description = topUpDto.Description ?? $"Account top-up via {topUpDto.PaymentMethod}",
                BalanceAfter = account.Balance,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(topUpTransaction);

            // Create fee transaction if applicable
            if (fee > 0)
            {
                var feeInAccountCurrency = topUpDto.Currency.ToUpper() != account.Currency
                    ? await _currencyService.ConvertCurrencyAsync(fee, topUpDto.Currency, account.Currency)
                    : fee;

                var feeTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    Type = TransactionType.Fee,
                    Amount = fee,
                    Currency = topUpDto.Currency.ToUpper(),
                    AmountInAccountCurrency = feeInAccountCurrency,
                    Description = $"Top-up processing fee ({topUpDto.PaymentMethod})",
                    BalanceAfter = account.Balance,
                    Status = TransactionStatus.Completed,
                    RelatedTransactionId = topUpTransaction.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(feeTransaction);
            }

            await _context.SaveChangesAsync();

            // Send notification
            var currencyInfo = await _currencyService.GetCurrencyAsync(account.Currency);
            await _notificationHelper.CreateNotification(
                userId,
                "Account Top-Up Successful",
                $"Your account {account.AccountNumber} has been topped up with {currencyInfo.Symbol}{amountInAccountCurrency:N2}. " +
                $"Processing fee: {currencyInfo.Symbol}{fee:N2}. New balance: {currencyInfo.Symbol}{account.Balance:N2}",
                NotificationType.Transaction
            );

            await transaction.CommitAsync();

            _logger.LogInformation($"Top-up successful for user {userId}, account {account.AccountNumber}, amount {topUpDto.Amount} {topUpDto.Currency}");

            return new TopUpResultDto
            {
                Success = true,
                TransactionId = topUpTransaction.Id,
                AccountNumber = account.AccountNumber,
                Amount = topUpDto.Amount,
                Currency = topUpDto.Currency,
                ProcessingFee = fee,
                TotalCharged = totalCharged,
                NewBalance = account.Balance,
                PaymentMethod = topUpDto.PaymentMethod,
                ReferenceNumber = GenerateReferenceNumber(),
                Timestamp = DateTime.UtcNow,
                Message = "Top-up completed successfully"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, $"Top-up failed for user {userId}");
            throw;
        }
    }

    public async Task<TopUpQuoteDto> GetTopUpQuoteAsync(decimal amount, string currency, PaymentMethod paymentMethod)
    {
        var fee = CalculateProcessingFee(amount, paymentMethod);
        var estimatedTime = GetEstimatedProcessingTime(paymentMethod);

        return await Task.FromResult(new TopUpQuoteDto
        {
            Amount = amount,
            Currency = currency.ToUpper(),
            PaymentMethod = paymentMethod,
            ProcessingFee = fee,
            TotalAmount = amount + fee,
            EstimatedArrivalTime = estimatedTime,
            FeeExplanation = GetFeeExplanation(paymentMethod)
        });
    }

    public async Task<List<PaymentMethodInfoDto>> GetAvailablePaymentMethodsAsync()
    {
        var methods = new List<PaymentMethodInfoDto>();

        foreach (var method in Enum.GetValues<PaymentMethod>())
        {
            if (_feeStructure.TryGetValue(method, out var fees))
            {
                methods.Add(new PaymentMethodInfoDto
                {
                    Method = method,
                    DisplayName = GetPaymentMethodDisplayName(method),
                    Icon = GetPaymentMethodIcon(method),
                    FeePercentage = fees.percentage * 100,
                    MinimumFee = fees.minimum,
                    MaximumFee = fees.maximum,
                    IsActive = IsPaymentMethodActive(method),
                    EstimatedProcessingTime = GetEstimatedProcessingTime(method)
                });
            }
        }

        return await Task.FromResult(methods);
    }

    public async Task<List<TopUpHistoryDto>> GetTopUpHistoryAsync(Guid userId, int limit = 50)
    {
        var topUps = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId &&
                       t.Type == TransactionType.Deposit &&
                       t.Description != null &&
                       t.Description.Contains("top-up", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new TopUpHistoryDto
            {
                Id = t.Id,
                AccountNumber = t.Account.AccountNumber,
                Amount = t.Amount,
                Currency = t.Currency,
                PaymentMethod = ExtractPaymentMethod(t.Description),
                ProcessingFee = 0, // Would need to fetch from related fee transaction
                Status = t.Status.ToString(),
                ReferenceNumber = t.Id.ToString().Substring(0, 8).ToUpper(),
                CreatedAt = t.CreatedAt,
                Description = t.Description
            })
            .ToListAsync();

        return topUps;
    }

    public async Task<TopUpLimitsDto> GetTopUpLimitsAsync(Guid userId)
    {
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        // Get today's top-ups
        var todayTopUps = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId &&
                       t.Type == TransactionType.Deposit &&
                       t.CreatedAt >= today &&
                       t.Description != null &&
                       t.Description.Contains("top-up", StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        // Get this month's top-ups
        var monthlyTopUps = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId &&
                       t.Type == TransactionType.Deposit &&
                       t.CreatedAt >= firstDayOfMonth &&
                       t.Description != null &&
                       t.Description.Contains("top-up", StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        // Calculate totals in USD
        decimal todayTotal = 0;
        foreach (var topUp in todayTopUps)
        {
            if (topUp.Currency == "USD")
                todayTotal += topUp.Amount;
            else
                todayTotal += await _currencyService.ConvertCurrencyAsync(topUp.Amount, topUp.Currency, "USD");
        }

        decimal monthlyTotal = 0;
        foreach (var topUp in monthlyTopUps)
        {
            if (topUp.Currency == "USD")
                monthlyTotal += topUp.Amount;
            else
                monthlyTotal += await _currencyService.ConvertCurrencyAsync(topUp.Amount, topUp.Currency, "USD");
        }

        return new TopUpLimitsDto
        {
            MinimumAmount = MIN_TOPUP_AMOUNT,
            MaximumAmount = MAX_TOPUP_AMOUNT,
            DailyLimit = DAILY_TOPUP_LIMIT,
            MonthlyLimit = MONTHLY_TOPUP_LIMIT,
            UsedToday = todayTotal,
            UsedThisMonth = monthlyTotal,
            RemainingToday = Math.Max(0, DAILY_TOPUP_LIMIT - todayTotal),
            RemainingThisMonth = Math.Max(0, MONTHLY_TOPUP_LIMIT - monthlyTotal)
        };
    }

    public async Task<bool> ValidateTopUpLimitAsync(Guid userId, decimal amount)
    {
        var limits = await GetTopUpLimitsAsync(userId);

        // Convert amount to USD if needed
        decimal amountInUSD = amount; // Assuming amount is already in USD for simplicity

        return amountInUSD <= limits.RemainingToday && amountInUSD <= limits.RemainingThisMonth;
    }

    public async Task<PaymentValidationResultDto> ValidatePaymentMethodAsync(ValidatePaymentMethodDto validationDto)
    {
        var result = new PaymentValidationResultDto
        {
            IsValid = true,
            Errors = new List<string>(),
            PaymentMethodStatus = "Valid"
        };

        switch (validationDto.PaymentMethod)
        {
            case PaymentMethod.CreditCard:

            case PaymentMethod.BankTransfer:
                if (validationDto.BankDetails == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Bank account details are required");
                    result.PaymentMethodStatus = "Invalid";
                }
                else
                {
                    // Validate routing number (US specific - 9 digits)
                    if (string.IsNullOrEmpty(validationDto.BankDetails.RoutingNumber) ||
                        validationDto.BankDetails.RoutingNumber.Length != 9)
                    {
                        result.IsValid = false;
                        result.Errors.Add("Invalid routing number");
                    }

                    // Validate account number
                    if (string.IsNullOrEmpty(validationDto.BankDetails.AccountNumber) ||
                        validationDto.BankDetails.AccountNumber.Length < 4 ||
                        validationDto.BankDetails.AccountNumber.Length > 17)
                    {
                        result.IsValid = false;
                        result.Errors.Add("Invalid account number");
                    }
                }
                break;

                result.PaymentMethodStatus = "Requires device authentication";
                break;
        }

        return await Task.FromResult(result);
    }

    public async Task<TopUpResultDto> ProcessPaymentAsync(AccountTopUpDto topUpDto)
    {
        // This is where you would integrate with actual payment providers
        // For demo purposes, we'll simulate successful payment

        _logger.LogInformation($"Processing payment via {topUpDto.PaymentMethod} for amount {topUpDto.Amount} {topUpDto.Currency}");

        // Simulate payment processing delay
        await Task.Delay(1000);

        // Simulate different payment provider APIs
        switch (topUpDto.PaymentMethod)
        {
            case PaymentMethod.CreditCard:
                // Simulate Stripe or similar card processor
                return new TopUpResultDto
                {
                    Success = true,
                    Message = "Card payment processed successfully",
                    ReferenceNumber = $"CARD-{GenerateReferenceNumber()}"
                };

            case PaymentMethod.BankTransfer:
                // Simulate ACH or wire transfer
                return new TopUpResultDto
                {
                    Success = true,
                    Message = "Bank transfer initiated successfully",
                    ReferenceNumber = $"ACH-{GenerateReferenceNumber()}"
                };

            default:
                throw new InvalidOperationException($"Unsupported payment method: {topUpDto.PaymentMethod}");
        }
    }

    // Helper methods
    private decimal CalculateProcessingFee(decimal amount, PaymentMethod paymentMethod)
    {
        if (_feeStructure.TryGetValue(paymentMethod, out var fees))
        {
            var calculatedFee = amount * fees.percentage;

            // Apply minimum fee
            if (calculatedFee < fees.minimum)
                calculatedFee = fees.minimum;

            // Apply maximum fee
            if (calculatedFee > fees.maximum)
                calculatedFee = fees.maximum;

            return Math.Round(calculatedFee, 2);
        }

        return 0;
    }

    private string GetPaymentMethodDisplayName(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => "Credit Card",
            PaymentMethod.BankTransfer => "Bank Transfer",
            _ => method.ToString()
        };
    }

    private string GetPaymentMethodIcon(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => "credit-card",
            PaymentMethod.BankTransfer => "bank",
            _ => "payment"
        };
    }

    private int GetEstimatedProcessingTime(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => 1,      // 1 minute
            PaymentMethod.BankTransfer => 60,   // 60 minutes
            _ => 5
        };
    }

    private string GetFeeExplanation(PaymentMethod method)
    {
        if (_feeStructure.TryGetValue(method, out var fees))
        {
            return $"{fees.percentage * 100:N1}% processing fee (min ${fees.minimum:N2}, max ${fees.maximum:N2})";
        }
        return "No processing fee";
    }

    private bool IsPaymentMethodActive(PaymentMethod method)
    {
        // All methods are active for demo
        return true;
    }

    private PaymentMethod ExtractPaymentMethod(string description)
    {
        if (description.Contains("CreditCard", StringComparison.OrdinalIgnoreCase))
            return PaymentMethod.CreditCard;
        if (description.Contains("Bank", StringComparison.OrdinalIgnoreCase))
            return PaymentMethod.BankTransfer;

        return PaymentMethod.BankTransfer; // Default
    }

    private string GenerateReferenceNumber()
    {
        return Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12).ToUpper();
    }

    private bool IsValidCardNumber(string cardNumber)
    {
        // Remove spaces and hyphens
        cardNumber = cardNumber?.Replace(" ", "").Replace("-", "") ?? "";

        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 13 || cardNumber.Length > 19)
            return false;

        // Simplified Luhn algorithm check
        int sum = 0;
        bool alternate = false;

        for (int i = cardNumber.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(cardNumber[i]))
                return false;

            int n = int.Parse(cardNumber[i].ToString());

            if (alternate)
            {
                n *= 2;
                if (n > 9)
                    n = n % 10 + 1;
            }

            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    private bool IsValidExpiryDate(string expiryDate)
    {
        if (string.IsNullOrEmpty(expiryDate))
            return false;

        var parts = expiryDate.Split('/');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int year))
            return false;

        if (month < 1 || month > 12)
            return false;

        // Convert 2-digit year to 4-digit
        if (year < 100)
            year += 2000;

        var expiryDateTime = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
        return expiryDateTime >= DateTime.UtcNow.Date;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}