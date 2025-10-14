using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TopUpController : ControllerBase
{
    private readonly ITopUpService _topUpService;
    private readonly IAccountService _accountService;
    private readonly ILogger<TopUpController> _logger;

    public TopUpController(
        ITopUpService topUpService,
        IAccountService accountService,
        ILogger<TopUpController> logger)
    {
        _topUpService = topUpService;
        _accountService = accountService;
        _logger = logger;
    }
    [HttpPost("CreateTopup")]
    public async Task<IActionResult> Create([FromBody] AccountTopUpDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var res = await _topUpService.CreatePendingTopUpAsync(userId, dto, ct);
        return Ok(ResponseDto<TopUpRequestCreatedDto>.SuccessResponse(res, "Top-up request created (Pending)"));
    }

    [HttpGet("GetTopup")]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var isAdmin = User.IsInRole("Admin");
        var userId = GetCurrentUserId();
        var res = await _topUpService.GetTopUpsAsync(userId, isAdmin, status, take, ct);
        return Ok(ResponseDto<List<TopUpListItemDto>>.SuccessResponse(res));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> AdminUpdateStatus([FromRoute] Guid id, [FromQuery] string value, [FromQuery] string? reason, CancellationToken ct = default)
    {
        await _topUpService.AdminUpdateStatusAsync(GetCurrentUserId(), id, value, reason, ct);
        return Ok(new { Message = "Status updated" });
    }
    // POST: api/TopUp
    [HttpPost]
    public async Task<IActionResult> TopUpAccount([FromBody] AccountTopUpDto topUpDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid top-up data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();

            // Verify user owns the account
            if (!await _accountService.UserOwnsAccountAsync(userId, topUpDto.AccountId))
            {
                return Forbid();
            }

            var result = await _topUpService.ProcessTopUpAsync(userId, topUpDto);

            return Ok(ResponseDto<TopUpResultDto>.SuccessResponse(
                result,
                "Account top-up completed successfully"
            ));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized top-up attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid top-up operation");
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing top-up");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing the top-up"
            ));
        }
    }

    // POST: api/TopUp/quote
    [HttpPost("quote")]
    public async Task<IActionResult> GetTopUpQuote([FromBody] TopUpQuoteRequestDto quoteRequest)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid quote request",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var quote = await _topUpService.GetTopUpQuoteAsync(
                quoteRequest.Amount,
                quoteRequest.Currency,
                quoteRequest.PaymentMethod
            );

            return Ok(ResponseDto<TopUpQuoteDto>.SuccessResponse(quote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating top-up quote");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while generating the quote"
            ));
        }
    }

    // GET: api/TopUp/payment-methods
    [HttpGet("payment-methods")]
    public async Task<IActionResult> GetPaymentMethods()
    {
        try
        {
            var methods = await _topUpService.GetAvailablePaymentMethodsAsync();

            return Ok(ResponseDto<List<PaymentMethodInfoDto>>.SuccessResponse(methods));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment methods");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching payment methods"
            ));
        }
    }

    // GET: api/TopUp/history
    [HttpGet("history")]
    public async Task<IActionResult> GetTopUpHistory([FromQuery] int limit = 50)
    {
        try
        {
            if (limit < 1 || limit > 100)
                limit = 50;

            var userId = GetCurrentUserId();
            var history = await _topUpService.GetTopUpHistoryAsync(userId, limit);

            return Ok(ResponseDto<List<TopUpHistoryDto>>.SuccessResponse(history));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top-up history");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching top-up history"
            ));
        }
    }

    // GET: api/TopUp/limits
    [HttpGet("limits")]
    public async Task<IActionResult> GetTopUpLimits()
    {
        try
        {
            var userId = GetCurrentUserId();
            var limits = await _topUpService.GetTopUpLimitsAsync(userId);

            return Ok(ResponseDto<TopUpLimitsDto>.SuccessResponse(limits));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top-up limits");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching top-up limits"
            ));
        }
    }

    // POST: api/TopUp/validate-payment-method
    [HttpPost("validate-payment-method")]
    public async Task<IActionResult> ValidatePaymentMethod([FromBody] ValidatePaymentMethodDto validationDto)
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

            var result = await _topUpService.ValidatePaymentMethodAsync(validationDto);

            if (!result.IsValid)
            {
                return BadRequest(ResponseDto<PaymentValidationResultDto>.ErrorResponse(
                    "Payment method validation failed",
                    result.Errors
                ));
            }

            return Ok(ResponseDto<PaymentValidationResultDto>.SuccessResponse(
                result,
                "Payment method is valid"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payment method");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while validating the payment method"
            ));
        }
    }

    // POST: api/TopUp/validate-amount
    [HttpPost("validate-amount")]
    public async Task<IActionResult> ValidateTopUpAmount([FromBody] ValidateAmountDto validateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isValid = await _topUpService.ValidateTopUpLimitAsync(userId, validateDto.Amount);

            if (!isValid)
            {
                var limits = await _topUpService.GetTopUpLimitsAsync(userId);
                return BadRequest(ResponseDto<TopUpLimitsDto>.ErrorResponse(
                    "Amount exceeds available limits",
                    new List<string>
                    {
                        $"Daily remaining: ${limits.RemainingToday:N2}",
                        $"Monthly remaining: ${limits.RemainingThisMonth:N2}"
                    }
                ));
            }

            return Ok(ResponseDto<bool>.SuccessResponse(
                true,
                "Amount is within limits"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating top-up amount");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while validating the amount"
            ));
        }
    }

    // GET: api/TopUp/accounts
    [HttpGet("accounts")]
    public async Task<IActionResult> GetEligibleAccounts()
    {
        try
        {
            var userId = GetCurrentUserId();
            var accounts = await _accountService.GetActiveUserAccountsAsync(userId);

            // Map to simplified DTO for top-up selection
            var eligibleAccounts = accounts.Select(a => new AccountSelectionDto
            {
                AccountId = a.Id,
                AccountNumber = a.AccountNumber,
                AccountType = a.Type.ToString(),
                Currency = a.Currency,
                Balance = a.Balance,
                IsPriority = a.IsPriority,
                DisplayName = $"{a.Type} Account - {a.AccountNumber}"
            }).ToList();

            return Ok(ResponseDto<List<AccountSelectionDto>>.SuccessResponse(eligibleAccounts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching eligible accounts");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching accounts"
            ));
        }
    }

    // POST: api/TopUp/simulate
    [HttpPost("simulate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SimulateTopUp([FromBody] SimulateTopUpDto simulateDto)
    {
        try
        {
            // This endpoint is for testing/demo purposes only
            // It simulates a top-up without actually processing payment

            var topUpDto = new AccountTopUpDto
            {
                AccountId = simulateDto.AccountId,
                Amount = simulateDto.Amount,
                Currency = simulateDto.Currency,
                PaymentMethod = PaymentMethod.BankTransfer,
                Description = "Simulated top-up (test mode)"
            };

            var userId = GetCurrentUserId();

            // For admin testing, they can top up any account
            var account = await _accountService.GetByIdAsync(simulateDto.AccountId);
            if (account == null)
            {
                return NotFound(ResponseDto<object>.ErrorResponse("Account not found"));
            }

            var result = await _topUpService.ProcessTopUpAsync(account.UserId, topUpDto);

            return Ok(ResponseDto<TopUpResultDto>.SuccessResponse(
                result,
                "Simulated top-up completed (test mode)"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating top-up");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while simulating the top-up"
            ));
        }
    }

    // GET: api/TopUp/statistics
    [HttpGet("statistics")]
    public async Task<IActionResult> GetTopUpStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = GetCurrentUserId();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var history = await _topUpService.GetTopUpHistoryAsync(userId, 1000);
            var filteredHistory = history
                .Where(h => h.CreatedAt >= start && h.CreatedAt <= end)
                .ToList();

            var statistics = new TopUpStatisticsDto
            {
                Period = $"{start:yyyy-MM-dd} to {end:yyyy-MM-dd}",
                TotalTopUps = filteredHistory.Count,
                TotalAmount = filteredHistory.Sum(h => h.Amount),
                TotalFees = filteredHistory.Sum(h => h.ProcessingFee),
                AverageAmount = filteredHistory.Any() ? filteredHistory.Average(h => h.Amount) : 0,
                MostUsedMethod = filteredHistory
                    .GroupBy(h => h.PaymentMethod)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key.ToString() ?? "None",
                TopUpsByMethod = filteredHistory
                    .GroupBy(h => h.PaymentMethod)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                TopUpsByCurrency = filteredHistory
                    .GroupBy(h => h.Currency)
                    .ToDictionary(g => g.Key, g => g.Sum(h => h.Amount))
            };

            return Ok(ResponseDto<TopUpStatisticsDto>.SuccessResponse(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top-up statistics");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching statistics"
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
}