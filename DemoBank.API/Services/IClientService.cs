using DemoBank.Core.DTOs;
namespace DemoBank.API.Services;

public interface IClientService
{
    Task<List<AdminClientListDto>> GetClientList();
}
