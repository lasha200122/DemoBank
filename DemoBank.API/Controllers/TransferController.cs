using AutoMapper;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransferController : ControllerBase
{
    private readonly ITransferService _transferService;
    private readonly IAccountService _accountService;
    private readonly IMapper _mapper;

    public TransferController(
        ITransferService transferService,
        IAccountService accountService,
        IMapper mapper)
    {
        _transferService = transferService;
        _accountService = accountService;
        _mapper = mapper;
    }

    // POST: api/Transfer/internal
    [HttpPost("internal")]
    public async Task<IActionResult> TransferBetweenOwnAccounts([FromBody] InternalTransferDto transferDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid transfer data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();

            // Verify user owns the source account
            if (!await _accountService.UserOwnsAccountAsync(userId, transferDto.FromAccountId))
            {
                return Forbid();
            }

            var result = await _transferService.TransferBetweenOwnAccountsAsync(userId, transferDto);

            return Ok(ResponseDto<TransferResultDto>.SuccessResponse(
                result,
                "Transfer between your accounts completed successfully"
            ));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing the transfer"
            ));
        }
    }

    // POST: api/Transfer/external
    [HttpPost("external")]
    public async Task<IActionResult> TransferToAnotherUser([FromBody] ExternalTransferDto transferDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid transfer data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();

            // Verify user owns the source account
            if (!await _accountService.UserOwnsAccountAsync(userId, transferDto.FromAccountId))
            {
                return Forbid();
            }

            var result = await _transferService.TransferToAnotherUserAsync(userId, transferDto);

            return Ok(ResponseDto<TransferResultDto>.SuccessResponse(
                result,
                "Transfer completed successfully"
            ));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing the transfer"
            ));
        }
    }

    // POST: api/Transfer/validate
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateTransfer([FromBody] ValidateTransferDto validateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid validation data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();

            // Verify user owns the source account
            if (!await _accountService.UserOwnsAccountAsync(userId, validateDto.FromAccountId))
            {
                return Forbid();
            }

            var result = await _transferService.ValidateTransferAsync(
                validateDto.FromAccountId,
                validateDto.ToAccountNumber,
                validateDto.Amount
            );

            return Ok(ResponseDto<TransferValidationResult>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred during validation"
            ));
        }
    }

    // GET: api/Transfer/history
    [HttpGet("history")]
    public async Task<IActionResult> GetTransferHistory([FromQuery] int limit = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            var history = await _transferService.GetTransferHistoryAsync(userId, limit);

            return Ok(ResponseDto<List<TransferHistoryDto>>.SuccessResponse(history));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching transfer history"
            ));
        }
    }

    // GET: api/Transfer/account/{accountId}
    [HttpGet("account/{accountId}")]
    public async Task<IActionResult> GetAccountTransfers(Guid accountId, [FromQuery] int limit = 50)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify user owns the account
            if (!await _accountService.UserOwnsAccountAsync(userId, accountId) && !IsAdmin())
            {
                return Forbid();
            }

            var history = await _transferService.GetAccountTransfersAsync(accountId, limit);

            return Ok(ResponseDto<List<TransferHistoryDto>>.SuccessResponse(history));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching account transfers"
            ));
        }
    }

    // GET: api/Transfer/statistics
    [HttpGet("statistics")]
    public async Task<IActionResult> GetTransferStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = GetCurrentUserId();
            var stats = await _transferService.GetTransferStatisticsAsync(userId, startDate, endDate);

            return Ok(ResponseDto<TransferStatisticsDto>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching transfer statistics"
            ));
        }
    }

    // GET: api/Transfer/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransferDetails(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var details = await _transferService.GetTransferDetailsAsync(id, userId);

            if (details == null)
            {
                return NotFound(ResponseDto<object>.ErrorResponse("Transfer not found"));
            }

            return Ok(ResponseDto<TransferDetailsDto>.SuccessResponse(details));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching transfer details"
            ));
        }
    }

    // POST: api/Transfer/quick
    [HttpPost("quick")]
    public async Task<IActionResult> QuickTransfer([FromBody] QuickTransferDto quickTransferDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid transfer data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();

            // Get source account by account number
            var fromAccount = await _accountService.GetByAccountNumberAsync(quickTransferDto.FromAccountNumber);
            if (fromAccount == null)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse("Source account not found"));
            }

            if (fromAccount.UserId != userId)
            {
                return Forbid();
            }

            // Check if it's internal or external transfer
            var toAccount = await _accountService.GetByAccountNumberAsync(quickTransferDto.ToAccountNumber);
            if (toAccount == null)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse("Destination account not found"));
            }

            TransferResultDto result;

            if (toAccount.UserId == userId)
            {
                // Internal transfer
                var internalDto = new InternalTransferDto
                {
                    FromAccountId = fromAccount.Id,
                    ToAccountNumber = quickTransferDto.ToAccountNumber,
                    Amount = quickTransferDto.Amount,
                    Description = quickTransferDto.Description
                };

                result = await _transferService.TransferBetweenOwnAccountsAsync(userId, internalDto);
            }
            else
            {
                // External transfer
                var externalDto = new ExternalTransferDto
                {
                    FromAccountId = fromAccount.Id,
                    ToAccountNumber = quickTransferDto.ToAccountNumber,
                    Amount = quickTransferDto.Amount,
                    Description = quickTransferDto.Description
                };

                result = await _transferService.TransferToAnotherUserAsync(userId, externalDto);
            }

            return Ok(ResponseDto<TransferResultDto>.SuccessResponse(
                result,
                "Quick transfer completed successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing the quick transfer"
            ));
        }
    }

    // PUT: api/Transfer/{id}/cancel
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelTransfer(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _transferService.CancelPendingTransferAsync(id, userId);

            if (!result)
            {
                return NotFound(ResponseDto<object>.ErrorResponse("Transfer not found"));
            }

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Transfer cancelled successfully"
            ));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while cancelling the transfer"
            ));
        }
    }

    // GET: api/Transfer/recent-recipients
    [HttpGet("recent-recipients")]
    public async Task<IActionResult> GetRecentRecipients([FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transfers = await _transferService.GetTransferHistoryAsync(userId, 50);

            // Get unique recipients
            var recipients = transfers
                .Where(t => t.Direction == "Outgoing")
                .GroupBy(t => t.ToAccount)
                .Select(g => new RecentRecipientDto
                {
                    AccountNumber = g.Key,
                    Name = g.First().CounterpartyName,
                    LastTransferDate = g.Max(t => t.Timestamp),
                    TransferCount = g.Count()
                })
                .OrderByDescending(r => r.LastTransferDate)
                .Take(limit)
                .ToList();

            return Ok(ResponseDto<List<RecentRecipientDto>>.SuccessResponse(recipients));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching recent recipients"
            ));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim);
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }
}