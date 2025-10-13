using DemoBank.Core.DTOs;
namespace DemoBank.API.Services;

public interface IClientService
{
    Task<List<AdminClientListDto>> GetClientList();
    Task<List<BankingDetailsDto>> GetClientListById(Guid? guid);
    Task<bool> ApproveClient(Guid userId);
    Task<bool> RejectClient(Guid userId);
    Task<bool> CreateBankingDetails(CreateBankingDetailsDto createDto);
}
