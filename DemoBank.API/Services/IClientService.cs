using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using System.Threading.Tasks;
namespace DemoBank.API.Services;

public interface IClientService
{
    Task<List<AdminClientListDto>> GetClientList();
    Task<List<ClientBankSummaryDto>> GetClientListById(Guid? guid);
    Task<bool> ApproveClient(Guid userId);
    Task<bool> RejectClient(Guid userId);
    Task<bool> CreateBankingDetails(CreateBankingDetailsDto createDto);
    Task<bool> GetClientInvestmentSummaryAsync(CreateBankInvestmentDto request);
    Task<List<ClientInvestmentResponse>> GetClientInvestmentAsync(Guid clientId, string? accountId);
    Task<ClientInvestmentResponse?> UpdateInvestmentAsync(UpdateClientInvestmentDto dto);
}
