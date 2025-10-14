using DemoBank.API.Data;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoBank.API.Services;

public class ClientService : IClientService
{
    private readonly DemoBankContext _context;

    public ClientService(DemoBankContext context)
    {
        _context = context;
    }

    public async Task<List<AdminClientListDto>> GetClientList()
    {
        var investmentCounts = await _context.ClientInvestment
            .GroupBy(ci => ci.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var clients = await _context.Users
            .Where(u => u.Role == UserRole.Client)
            .Select(u => new AdminClientListDto
            {
                ClientId = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                Username = u.Username,
                Email = u.Email,
                InvestmentRange = u.PotentialInvestmentRange ?? 0,
                Status = u.Status,
                EmailStatus = false,
                Passkey = u.Passkey,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin,
                ActiveAccounts = u.Accounts.Count(a => a.IsActive),
                ActiveInvestments = investmentCounts.ContainsKey(u.Id) ? investmentCounts[u.Id] : 0,
                ActiveLoans = u.Loans.Count(l => l.Status == LoanStatus.Active),
                TotalBalanceUSD = u.Accounts.Where(a => a.IsActive && a.Currency == "USD").Sum(a => (decimal?)a.Balance) ?? 0m,
                TotalBalanceEUR = u.Accounts.Where(a => a.IsActive && a.Currency == "EUR").Sum(a => (decimal?)a.Balance) ?? 0m
            })
            .ToListAsync();

        return clients;
    }

    public async Task<bool> ApproveClient(Guid userId)
    {
        var client = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (client == null)
            return false;

        client.Status = Status.Active;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RejectClient(Guid userId)
    {
        var client = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (client == null)
            return false;

        client.Status = Status.Rejected;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> CreateBankingDetails(CreateBankingDetailsDto createDto)
    {
        var user = await _context.Users.FindAsync(createDto.UserId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var bankingDetails = new BankingDetails
        {
            Id = Guid.NewGuid(),
            UserId = (Guid)createDto.UserId,

            // IBAN details
            BeneficialName = createDto.BankingDetails?.IbanDetails?.BeneficialName,
            IBAN = createDto.BankingDetails?.IbanDetails?.IBAN,
            Reference = createDto.BankingDetails?.IbanDetails?.Reference,
            BIC = createDto.BankingDetails?.IbanDetails?.BIC,

            // Card details
            CardNumber = createDto.BankingDetails?.CardDetails?.CardNumber,
            CardHolderName = createDto.BankingDetails?.CardDetails?.CardHolderName,
            ExpiryDate = createDto.BankingDetails?.CardDetails?.ExpiryDate,
            CVV = createDto.BankingDetails?.CardDetails?.CVV,

            // Cryptocurrency details
            WalletAddress = createDto.BankingDetails?.CryptocurrencyDetails?.WalletAddress,
            TransactionHash = createDto.BankingDetails?.CryptocurrencyDetails?.TransactionHash,

            // Bank Details
            AccountHolderName = createDto.BankingDetails?.BankDetails?.AccountHolderName,
            AccountNumber = createDto.BankingDetails?.BankDetails?.AccountNumber,
            RoutingNumber = createDto.BankingDetails?.BankDetails?.RoutingNumber
        };


        _context.BankingDetails.Add(bankingDetails);

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<ClientBankSummaryDto>> GetClientListById(Guid? guid)
    {
        if (guid is null)
            return new List<ClientBankSummaryDto>();

        var clientInvestments = await _context.ClientInvestment
            .Where(ci => ci.UserId == guid)
            .ToListAsync();

        var users = await _context.Users
            .Where(u => (u.Role == UserRole.Client || u.Role == UserRole.Admin) && u.Id == guid)
            .Include(u => u.Accounts)
            .Include(u => u.Loans)
            .Include(u => u.BankingDetails)
            .AsNoTracking()
            .ToListAsync();

        var result = users.Select(u =>
        {
            var activeAccounts = u.Accounts.Where(a => a.IsActive).ToList();

            var totalBalanceUSD = activeAccounts
                .Where(a => a.Currency == "USD")
                .Sum(a => a.Balance);

            var totalBalanceEUR = activeAccounts
                .Where(a => a.Currency == "EUR")
                .Sum(a => a.Balance);

            var monthlyReturnsUSD = activeAccounts
                .Where(a => a.Currency == "USD")
                .Join(clientInvestments,
                      a => a.Id.ToString(),         
                      ci => ci.AccountId, 
                      (a, ci) => (a.Balance * ci.MonthlyReturn) / 100m)
                .Sum();

            var yearlyReturnsUSD = activeAccounts
                .Where(a => a.Currency == "USD")
                .Join(clientInvestments,
                      a => a.Id.ToString(),
                      ci => ci.AccountId,
                      (a, ci) => (a.Balance * ci.YearlyReturn) / 100m)
                .Sum();

            var monthlyReturnsEUR = activeAccounts
                .Where(a => a.Currency == "EUR")
                .Join(clientInvestments,
                      a => a.Id.ToString(),
                      ci => ci.AccountId,
                      (a, ci) => (a.Balance * ci.MonthlyReturn) / 100m)
                .Sum();

            var yearlyReturnsEUR = activeAccounts
                .Where(a => a.Currency == "EUR")
                .Join(clientInvestments,
                      a => a.Id.ToString(),
                      ci => ci.AccountId,
                      (a, ci) => (a.Balance * ci.YearlyReturn) / 100m)
                .Sum();

            return new ClientBankSummaryDto
            {
                ClientId = u.Id,
                FullName = $"{u.FirstName} {u.LastName}",
                Username = u.Username,
                Email = u.Email,
                InvestmentRange = (int)(u.PotentialInvestmentRange ?? 0),
                Status = (int)u.Status,
                EmailStatus = false,
                Passkey = u.Passkey,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin,
                ActiveAccounts = activeAccounts.Count,
                ActiveInvestments = clientInvestments.Count,
                ActiveLoans = u.Loans.Count(l => l.Status == LoanStatus.Active),
                TotalBalanceUSD = totalBalanceUSD,
                TotalBalanceEUR = totalBalanceEUR,
                MonthlyReturnsUSD = Math.Round(monthlyReturnsUSD, 2),
                YearlyReturnsUSD = Math.Round(yearlyReturnsUSD, 2),
                MonthlyReturnsEUR = Math.Round(monthlyReturnsEUR, 2),
                YearlyReturnsEUR = Math.Round(yearlyReturnsEUR, 2),
                BankingDetails = u.BankingDetails.Select(b => new BankingDetailsItemDto
                {
                    UserId = b.UserId,

                    BankDetails = new BankAccountDetails
                    {
                        AccountHolderName = b.AccountHolderName,
                        AccountNumber = b.AccountNumber,
                        RoutingNumber = b.RoutingNumber
                    },

                    CardPaymentDetails = new CardPaymentDetails
                    {
                        CardHolderName = b.CardHolderName,
                        CardNumber = b.CardNumber,
                        CVV = b.CVV,
                        ExpiryDate = b.ExpiryDate
                    },

                    CryptocurrencyDetails = new CryptocurrencyDetails
                    {
                        WalletAddress = b.WalletAddress,
                        TransactionHash = b.TransactionHash
                    },

                    IbanDetails = new IbanDetails
                    {
                        BeneficialName = b.BeneficialName,
                        IBAN = b.IBAN,
                        Reference = b.Reference,
                        BIC = b.BIC
                    }
                }).ToList()
            };
        }).ToList();

        return result;
    }

    public async Task<bool> GetClientInvestmentSummaryAsync(CreateBankInvestmentDto request)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => (u.Role == UserRole.Client || u.Role == UserRole.Admin) && u.Id == request.Id)
            .FirstOrDefaultAsync();

        if (user is null)
            return false;

        var existingInvestment = await _context.ClientInvestment
         .FirstOrDefaultAsync(ci => ci.AccountId == request.AccountId);

        if (existingInvestment != null)
        {
            throw new InvalidOperationException($"An investment already exists for AccountId {request.AccountId}.");
        }

        var clientInvestment = new ClientInvestment
        {
            Id = Guid.NewGuid(),
            UserId = request.Id,
            MonthlyReturn = request.MonthlyPercent,
            YearlyReturn = request.YearlyPercent,
            CreatedAt = DateTime.UtcNow,
            AccountId = request.AccountId
        };


        _context.ClientInvestment.Add(clientInvestment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ClientInvestmentResponse>> GetClientInvestmentAsync(Guid clientId, string? accountId)
    {
        var query = _context.ClientInvestment
            .Where(u => u.UserId == clientId);

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(u => u.AccountId == accountId);
        }

        var clientInvestments = await query
            .Select(u => new ClientInvestmentResponse
            {
                Id = u.Id,
                UserId = u.UserId,
                MonthlyReturn = u.MonthlyReturn,
                YearlyReturn = u.YearlyReturn,
                AccountId = u.AccountId,
                CreatedAt = u.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync();

        return clientInvestments;
    }


    public async Task<ClientInvestmentResponse?> UpdateInvestmentAsync(UpdateClientInvestmentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AccountId))
            return null;

        var investment = await _context.ClientInvestment
            .FirstOrDefaultAsync(x => x.AccountId == dto.AccountId);

        if (investment == null)
            return null;

        investment.MonthlyReturn = dto.MonthlyReturn;
        investment.YearlyReturn = dto.YearlyReturn;
        investment.UserId = dto.UserId;
        investment.CreatedAt = DateTime.UtcNow;

        _context.ClientInvestment.Update(investment);
        await _context.SaveChangesAsync();

        return new ClientInvestmentResponse
        {
            Id = investment.Id,
            UserId = investment.UserId,
            MonthlyReturn = investment.MonthlyReturn,
            YearlyReturn = investment.YearlyReturn,
            AccountId = investment.AccountId,
            CreatedAt = investment.CreatedAt
        };
    }

    public async Task<bool> UpdateBankingDetails(UpdateBankingDetailsDto dto)
    {
        // Find the existing banking details
        var banking = await _context.BankingDetails.FirstOrDefaultAsync(b => b.Id == dto.Id);

        if (banking == null)
            return false; // Not found

        // Update IBAN details
        if (dto.BankingDetails.IbanDetails != null)
        {
            banking.BeneficialName = dto.BankingDetails.IbanDetails.BeneficialName;
            banking.IBAN = dto.BankingDetails.IbanDetails.IBAN;
            banking.Reference = dto.BankingDetails.IbanDetails.Reference;
            banking.BIC = dto.BankingDetails.IbanDetails.BIC;
        }

        // Update Card details
        if (dto.BankingDetails.CardDetails != null)
        {
            banking.CardNumber = dto.BankingDetails.CardDetails.CardNumber;
            banking.CardHolderName = dto.BankingDetails.CardDetails.CardHolderName;
            banking.ExpiryDate = dto.BankingDetails.CardDetails.ExpiryDate;
            banking.CVV = dto.BankingDetails.CardDetails.CVV;
        }

        // Update Bank Account details
        if (dto.BankingDetails.BankDetails != null)
        {
            banking.AccountNumber = dto.BankingDetails.BankDetails.AccountNumber;
            banking.RoutingNumber = dto.BankingDetails.BankDetails.RoutingNumber;
            banking.AccountHolderName = dto.BankingDetails.BankDetails.AccountHolderName;
        }

        // Update Cryptocurrency details
        if (dto.BankingDetails.CryptocurrencyDetails != null)
        {
            banking.WalletAddress = dto.BankingDetails.CryptocurrencyDetails.WalletAddress;
            banking.TransactionHash = dto.BankingDetails.CryptocurrencyDetails.TransactionHash;
        }

        // Save changes
        var updated = await _context.SaveChangesAsync();
        return updated > 0;
    }


    public async Task<List<BankingDetailsDto>?> GetClientBankingDetails(Guid userId)
    {
        var bankings = await _context.BankingDetails
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => new BankingDetailsDto
            {
                CardDetails = new CardPaymentDetails
                {
                    CardNumber = b.CardNumber,
                    CardHolderName = b.CardHolderName,
                    ExpiryDate = b.ExpiryDate,
                    CVV = b.CVV
                },
                BankDetails = new BankAccountDetails
                {
                    AccountNumber = b.AccountNumber,
                    RoutingNumber = b.RoutingNumber,
                    AccountHolderName = b.AccountHolderName
                },
                IbanDetails = new IbanDetails
                {
                    BeneficialName = b.BeneficialName,
                    IBAN = b.IBAN,
                    Reference = b.Reference,
                    BIC = b.BIC
                },
                CryptocurrencyDetails = new CryptocurrencyDetails
                {
                    WalletAddress = b.WalletAddress,
                    TransactionHash = b.TransactionHash
                }
            })
            .ToListAsync();

        return bankings;
    }
}
