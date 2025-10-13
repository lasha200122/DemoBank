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
                Passkey = "",
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin,
                ActiveAccounts = u.Accounts.Count(a => a.IsActive),
                ActiveInvestments = u.Investments.Count(i => i.Status == InvestmentStatus.Active),
                ActiveLoans = u.Loans.Count(l => l.Status == LoanStatus.Active),
                TotalBalanceUSD = u.Accounts.Where(a => a.IsActive).Sum(a => (decimal?)a.Balance) ?? 0,
            })
            .ToListAsync();

        return clients;
    }
}
