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

                MonthlyReturns = 0m,
                YearlyReturns = 0m,

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

        return result; // თუ იუზერს ბანკინგ-დეტალი არ აქვს, BankingDetails იქნება []
    }

}
