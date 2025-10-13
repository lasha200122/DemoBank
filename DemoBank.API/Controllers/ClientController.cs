using DemoBank.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetClientList()
    {
        var clients = await _clientService.GetClientList();
        return Ok(clients);
    }

    [HttpPut("approve")]
    public async Task<IActionResult> ApproveClient([FromQuery] Guid clientId)
    {
        var success = await _clientService.ApproveClient(clientId);
        if (!success) return NotFound(new { Message = "Client not found" });
        return Ok(new { Message = "Client approved successfully" });
    }

    [HttpPut("reject")]
    public async Task<IActionResult> RejectClient([FromQuery] Guid clientId)
    {
        var success = await _clientService.RejectClient(clientId);
        if (!success) return NotFound(new { Message = "Client not found" });
        return Ok(new { Message = "Client rejected successfully" });
    }
}
