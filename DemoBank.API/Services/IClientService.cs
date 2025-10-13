using DemoBank.Core.DTOs;
namespace DemoBank.API.Services;

public interface IClientService
{
    Task<List<AdminClientListDto>> GetClientList();
    Task<bool> ApproveClient(Guid userId);
    Task<bool> RejectClient(Guid userId);
}
