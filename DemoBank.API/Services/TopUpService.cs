using DemoBank.API.Data;
using DemoBank.API.Helpers;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DemoBank.API.Services;

public class TopUpService : ITopUpService
{
    private readonly DemoBankContext _context;
    private readonly IAccountService _accountService;
    private readonly ICurrencyService _currencyService;
    private readonly INotificationHelper _notificationHelper;
    private readonly ILogger<TopUpService> _logger;
    private readonly IClientService _clientService;

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
        ILogger<TopUpService> logger,
        IClientService clientService)
    {
        _context = context;
        _accountService = accountService;
        _currencyService = currencyService;
        _notificationHelper = notificationHelper;
        _logger = logger;
        _clientService = clientService;
    }
    public async Task<TopUpRequestCreatedDto> CreatePendingTopUpAsync(Guid userId, AccountTopUpDto dto, CancellationToken ct = default)
    {
        var acc = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.AccountId, ct)
                  ?? throw new InvalidOperationException("Account not found");
        if (acc.UserId != userId) throw new UnauthorizedAccessException("You don't own this account");
        if (!acc.IsActive) throw new InvalidOperationException("Account is not active");

        if (await _currencyService.GetCurrencyAsync(dto.Currency) is null)
            throw new InvalidOperationException($"Currency {dto.Currency} is not supported");

        var pending = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = dto.AccountId,
            Type = TransactionType.Deposit,
            Amount = dto.Amount,
            Currency = dto.Currency.ToUpperInvariant(),
            Description = $"Top-up init via {dto.PaymentMethod}",
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(pending);
        await _context.SaveChangesAsync(ct);

        var detailsList = await _clientService.GetClientBankingDetails(userId);
        var instruction = BuildInstructionFrom(detailsList, dto);

        return new TopUpRequestCreatedDto
        {
            TransactionId = pending.Id,
            Status = pending.Status.ToString(),
            Amount = pending.Amount,
            Currency = pending.Currency,
            PaymentMethod = dto.PaymentMethod,
            PaymentInstruction = instruction
        };
    }

    public async Task<List<TopUpListItemDto>> GetTopUpsAsync(Guid requesterId, bool isAdmin, string? status = null, int take = 100, CancellationToken ct = default)
    {
        var q = _context.Transactions.AsNoTracking().Where(t => t.Type == TransactionType.Deposit);

        if (!isAdmin)
        {
            q = q.Join(_context.Accounts, t => t.AccountId, a => a.Id, (t, a) => new { t, a })
                 .Where(x => x.a.UserId == requesterId)
                 .Select(x => x.t);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TransactionStatus>(status, true, out var st))
            q = q.Where(t => t.Status == st);

        var list = await q.OrderByDescending(t => t.CreatedAt)
                          .Take(Math.Clamp(take, 1, 1000))
                          .Join(_context.Accounts, t => t.AccountId, a => a.Id, (t, a) => new TopUpListItemDto
                          {
                              TransactionId = t.Id,
                              CreatedAt = t.CreatedAt,
                              AccountId = a.Id,
                              AccountNumber = a.AccountNumber,
                              Amount = t.Amount,
                              Currency = t.Currency,
                              PaymentMethod = ParsePaymentFromDescription(t.Description),
                              Status = t.Status.ToString()
                          })
                          .ToListAsync(ct);

        return list;
    }

    public async Task AdminUpdateStatusAsync(Guid adminId, Guid transactionId, string newStatus, string? reason = null, CancellationToken ct = default)
    {
        var pending = await _context.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct)
            ?? throw new InvalidOperationException("Transaction not found");

        //if (pending.Type != TransactionType.Deposit)
        //    throw new InvalidOperationException("Not a top-up transaction");

        //if (pending.Status != TransactionStatus.Pending)
        //    throw new InvalidOperationException($"Transaction is not Pending (current: {pending.Status})");

        // Handle rejection
        if (newStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            pending.Status = TransactionStatus.Cancelled;
            pending.Description = (pending.Description ?? "") +
                (string.IsNullOrWhiteSpace(reason) ? "" : $" | Rejected: {reason}");
            await _context.SaveChangesAsync(ct);
            return;
        }

        // Handle approval
        if (!newStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsupported status transition");

        var account = pending.Account;

        // Validate currency
        //var currency = await _currencyService.GetCurrencyAsync(pending.Currency);
        //if (currency == null)
        //    throw new InvalidOperationException($"Currency {pending.Currency} is not supported");

        // Calculate amount in account currency if different
        decimal amountInAccountCurrency = pending.Amount;
        decimal? exchangeRate = null;

        //if (pending.Currency.ToUpper() != account.Currency)
        //{
        //    exchangeRate = await _currencyService.GetExchangeRateAsync(
        //        pending.Currency,
        //        account.Currency
        //    );
        //    amountInAccountCurrency = await _currencyService.ConvertCurrencyAsync(
        //        pending.Amount,
        //        pending.Currency,
        //        account.Currency
        //    );
        //}

        // Update account balance
        account.Balance += amountInAccountCurrency;
        account.UpdatedAt = DateTime.UtcNow;

        // Update the existing transaction
        pending.Status = TransactionStatus.Completed;
        pending.ExchangeRate = exchangeRate;
        pending.AmountInAccountCurrency = amountInAccountCurrency;
        pending.BalanceAfter = account.Balance;
        pending.Description = (pending.Description ?? "") + " | Admin Approved" +
            (string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}");

        await _context.SaveChangesAsync(ct);

        // Send notification
        var currencyInfo = await _currencyService.GetCurrencyAsync(account.Currency);
        await _notificationHelper.CreateNotification(
            account.UserId,
            "Top-Up Approved",
            $"Your account {account.AccountNumber} has been topped up with {currencyInfo.Symbol}{amountInAccountCurrency:N2}.",
            NotificationType.Transaction
        );

        _logger.LogInformation($"Top-up {transactionId} approved by admin {adminId}");
    }
    private static PaymentMethod ParsePaymentFromDescription(string? desc)
    {
        if (string.IsNullOrWhiteSpace(desc)) return PaymentMethod.BankTransfer;
        if (desc.Contains("Crypto", StringComparison.OrdinalIgnoreCase)) return PaymentMethod.Crypto;
        if (desc.Contains("Iban", StringComparison.OrdinalIgnoreCase)) return PaymentMethod.Iban;
        if (desc.Contains("Card", StringComparison.OrdinalIgnoreCase)) return PaymentMethod.CreditCard;
        if (desc.Contains("Bank", StringComparison.OrdinalIgnoreCase)) return PaymentMethod.BankTransfer;
        return PaymentMethod.BankTransfer;
    }

    private static PaymentInstructionDto BuildInstructionFrom(List<BankingDetailsDto>? details, AccountTopUpDto dto)
    {
        if (details == null || details.Count == 0)
            throw new ValidationException("No banking details found for the user.");

        var d = dto.PaymentMethod switch
        {
            PaymentMethod.Iban => details.FirstOrDefault(x => x.IbanDetails != null),
            PaymentMethod.BankTransfer => details.FirstOrDefault(x => x.BankDetails != null),
            PaymentMethod.Crypto => details.FirstOrDefault(x => x.CryptocurrencyDetails != null),
            PaymentMethod.CreditCard => details.FirstOrDefault(x => x.CardDetails != null),
            _ => null
        };

        if (d is null)
            throw new ValidationException($"No saved details match payment method: {dto.PaymentMethod}.");

        var pi = new PaymentInstructionDto
        {
            Method = dto.PaymentMethod
        };

        switch (dto.PaymentMethod)
        {
            case PaymentMethod.Iban:
                {
                    var iban = d.IbanDetails
                                ?? throw new ValidationException("IbanDetails is required for IBAN payment.");

                    pi.BeneficialName = iban.BeneficialName;
                    pi.Iban = iban.IBAN;
                    pi.Reference = iban.Reference;
                    pi.Bic = iban.BIC;
                    break;
                }

            case PaymentMethod.BankTransfer:
                {
                    _ = d.BankDetails ?? throw new ValidationException("BankDetails is required for bank transfer.");
                    break;
                }

            case PaymentMethod.Crypto:
                {
                    var crypto = d.CryptocurrencyDetails
                                  ?? throw new ValidationException("CryptocurrencyDetails is required for crypto payment.");

                    pi.WalletAddress = crypto.WalletAddress;
                    pi.CryptoAmount = dto.Amount;
                    break;
                }

            case PaymentMethod.CreditCard:
                {
                    _ = d.CardDetails ?? throw new ValidationException("CardDetails is required for card payment.");
                    break;
                }

            default:
                throw new ValidationException("Unsupported payment method.");
        }

        return pi;
    }
    public async Task<TopUpResultDto> ProcessTopUpAsync(Guid userId, AccountTopUpDto topUpDto)
    {
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

            await _context.SaveChangesAsync();

            // Send notification
            var currencyInfo = await _currencyService.GetCurrencyAsync(account.Currency);
            await _notificationHelper.CreateNotification(
                userId,
                "Account Top-Up Successful",
                $"Your account {account.AccountNumber} has been topped up with {currencyInfo.Symbol}{amountInAccountCurrency:N2}. ",
                NotificationType.Transaction
            );

            _logger.LogInformation($"Top-up successful for user {userId}, account {account.AccountNumber}, amount {topUpDto.Amount} {topUpDto.Currency}");

            return new TopUpResultDto
            {
                Success = true,
                TransactionId = topUpTransaction.Id,
                AccountNumber = account.AccountNumber,
                Amount = topUpDto.Amount,
                Currency = topUpDto.Currency,
                ProcessingFee = 0,
                TotalCharged = topUpDto.Amount,
                NewBalance = account.Balance,
                PaymentMethod = topUpDto.PaymentMethod,
                ReferenceNumber = GenerateReferenceNumber(),
                Timestamp = DateTime.UtcNow,
                Message = "Top-up completed successfully"
            };
        }
        catch (Exception ex)
        {
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
        // ყველა საზღვარი Utc-kind-ით
        var todayUtc = new DateTime(
            DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
            0, 0, 0, DateTimeKind.Utc);

        var firstDayOfMonthUtc = new DateTime(
            todayUtc.Year, todayUtc.Month, 1,
            0, 0, 0, DateTimeKind.Utc);

        // Get today's top-ups
        var todayTopUps = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId
                     && t.Type == TransactionType.Deposit
                     && t.CreatedAt >= todayUtc
                     && t.Description != null
                     && EF.Functions.ILike(t.Description!, "%top-up%"))
            .ToListAsync();

        // Get this month's top-ups
        var monthlyTopUps = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId
                     && t.Type == TransactionType.Deposit
                     && t.CreatedAt >= firstDayOfMonthUtc
                     && t.Description != null
                     && EF.Functions.ILike(t.Description!, "%top-up%"))
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