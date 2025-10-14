using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
    [HttpGet("ID")]
    public async Task<IActionResult> GetClientListById(Guid? guid)
    {
        var clients = await _clientService.GetClientListById(guid);
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

    [HttpPost("GetClientInvestement")]
    public async Task<IActionResult> GetClientInvestement([FromBody] CreateBankInvestmentDto request)
    {
        try
        {
            var created = await _clientService.GetClientInvestmentSummaryAsync(request);

            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    [HttpGet("GetInvestment")]
    public async Task<IActionResult> GetClientInvestment(Guid clientId, string? accountId)
    {
        var clients = await _clientService.GetClientInvestmentAsync(clientId, accountId);
        return Ok(clients);
    }

    [HttpPut("UpdateInvestment")]
    public async Task<IActionResult> UpdateInvestment([FromBody] UpdateClientInvestmentDto request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountId))
            return BadRequest("AccountId is required.");

        var result = await _clientService.UpdateInvestmentAsync(request);

        if (result == null)
            return NotFound($"Investment record with AccountId {request.AccountId} not found.");

        return Ok(ResponseDto<ClientInvestmentResponse>.SuccessResponse(
            result,
            "Client investment updated successfully"
        ));
    }


    [HttpPost("Banking-details")]
    public async Task<IActionResult> BankingDetails([FromBody] CreateBankingDetailsDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid account data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }
            var account = await _clientService.CreateBankingDetails(createDto);
            if (!account) return NotFound(new { Message = "BankingDetails not found" });
            return Ok(new { Message = "BankingDetails Created Successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching account details"
            ));

        }
    }
}
