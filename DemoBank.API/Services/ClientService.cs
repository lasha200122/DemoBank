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
        // TODO
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
                ActiveInvestments = u.Investments.Count(i => i.Status == InvestmentStatus.Active),
                ActiveLoans = u.Loans.Count(l => l.Status == LoanStatus.Active),
                TotalBalanceUSD = u.Accounts.Where(a => a.IsActive).Sum(a => (decimal?)a.Balance) ?? 0
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
            BeneficialName = createDto.BeneficialName,
            IBAN = createDto.IBAN,
            Reference = createDto.Reference,
            BIC = createDto.BIC
        };

        _context.BankingDetails.Add(bankingDetails);

        return await _context.SaveChangesAsync() > 0;

    }

    public async Task<List<ClientBankSummaryDto>> GetClientListById(Guid? guid)
    {
        if (guid is null)
            return new List<ClientBankSummaryDto>();

        var result = await _context.Users
            .Where(u => (u.Role == UserRole.Client || u.Role == UserRole.Admin) && u.Id == guid)
            .Select(u => new ClientBankSummaryDto
            {
                ClientId = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                Username = u.Username,
                Email = u.Email,
                InvestmentRange = (int)(u.PotentialInvestmentRange ?? 0),
                Status = (int)u.Status,
                EmailStatus = false,
                Passkey = u.Passkey,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin,
                ActiveAccounts = u.Accounts.Count(a => a.IsActive),
                ActiveInvestments = u.Investments.Count(i => i.Status == InvestmentStatus.Active),
                ActiveLoans = u.Loans.Count(l => l.Status == LoanStatus.Active),
                TotalBalanceUSD = u.Accounts.Where(a => a.IsActive)
                                            .Sum(a => (decimal?)a.Balance) ?? 0m,
                BankingDetails = u.BankingDetails
                    .Select(b => new BankingDetailsItemDto
                    {
                        UserId = b.UserId,
                        BeneficialName = b.BeneficialName,
                        IBAN = b.IBAN,
                        Reference = b.Reference,
                        BIC = b.BIC
                    })
                    .ToList()
            })
            .AsNoTracking()
            .ToListAsync();

        return result;
    }

    public async Task<List<ClientBankSummaryDto>> GetClientInvestmentSummaryAsync(CreateBankInvestmentDto request)
    {
        if (request.Id == null)
            return new List<ClientBankSummaryDto>();

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Accounts)
            .Include(u => u.Investments)
            .Include(u => u.Loans)
            .Include(u => u.BankingDetails)
            .Where(u => (u.Role == UserRole.Client || u.Role == UserRole.Admin) && u.Id == request.Id)
            .FirstOrDefaultAsync();

        if (user is null)
            return new List<ClientBankSummaryDto>();

        var existingInvestment = await _context.ClientInvestment
         .FirstOrDefaultAsync(ci => ci.AccountId == request.AccountId);

        if (existingInvestment != null)
        {
            throw new InvalidOperationException($"An investment already exists for AccountId {request.AccountId}.");
        }

        var activeAccounts = user.Accounts?.Where(a => a.IsActive && a.Id.ToString() == request.AccountId).ToList() ?? new List<Account>();
        var bankingDetails = user.BankingDetails?.Select(b => new BankingDetailsItemDto
        {
            UserId = b.UserId,
            BeneficialName = b.BeneficialName,
            IBAN = b.IBAN,
            Reference = b.Reference,
            BIC = b.BIC
        }).ToList() ?? new List<BankingDetailsItemDto>();

        var activeBalance = activeAccounts.Sum(a => a.Balance);

        var result = new List<ClientBankSummaryDto>
{
    new ClientBankSummaryDto
    {
        ClientId = user.Id,
        FullName = $"{user.FirstName} {user.LastName}",
        Username = user.Username,
        Email = user.Email,
        InvestmentRange = (int)(user.PotentialInvestmentRange ?? 0),
        Status = (int)user.Status,
        EmailStatus = false,
        Passkey = user.Passkey,
        CreatedAt = user.CreatedAt,
        LastLogin = user.LastLogin,
        ActiveAccounts = activeAccounts.Count,
        ActiveInvestments = user.Investments?.Count(i => i.Status == InvestmentStatus.Active) ?? 0,
        ActiveLoans = user.Loans?.Count(l => l.Status == LoanStatus.Active) ?? 0,
        TotalBalanceUSD = activeBalance,
        MonthlyReturns = request.MonthlyPercent,
        YearlyReturns = request.YearlyPercent,
        BankingDetails = bankingDetails
    }
};

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
        return result;
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


}
