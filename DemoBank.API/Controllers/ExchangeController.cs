using AutoMapper;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExchangeController : ControllerBase
{
    private readonly IExchangeService _exchangeService;
    private readonly IAccountService _accountService;
    private readonly IMapper _mapper;

    public ExchangeController(
        IExchangeService exchangeService,
        IAccountService accountService,
        IMapper mapper)
    {
        _exchangeService = exchangeService;
        _accountService = accountService;
        _mapper = mapper;
    }

    // POST: api/Exchange
    [HttpPost]
    public async Task<IActionResult> ExchangeCurrency([FromBody] ExchangeRequestDto exchangeDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid exchange data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();

            // Verify user owns the source account
            if (!await _accountService.UserOwnsAccountAsync(userId, exchangeDto.FromAccountId))
            {
                return Forbid();
            }

            var result = await _exchangeService.ExchangeCurrencyAsync(userId, exchangeDto);

            return Ok(ResponseDto<ExchangeResultDto>.SuccessResponse(
                result,
                "Currency exchange completed successfully"
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
                "An error occurred while processing the exchange"
            ));
        }
    }

    // POST: api/Exchange/quote
    [HttpPost("quote")]
    public async Task<IActionResult> GetExchangeQuote([FromBody] GetExchangeQuoteDto quoteDto)
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

            var quote = await _exchangeService.GetExchangeQuoteAsync(
                quoteDto.FromCurrency,
                quoteDto.ToCurrency,
                quoteDto.Amount
            );

            return Ok(ResponseDto<ExchangeQuoteDto>.SuccessResponse(quote));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while generating quote"
            ));
        }
    }

    // GET: api/Exchange/history/{fromCurrency}/{toCurrency}
    [HttpGet("history/{fromCurrency}/{toCurrency}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRateHistory(
        string fromCurrency,
        string toCurrency,
        [FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Days must be between 1 and 365"
                ));
            }

            var history = await _exchangeService.GetRateHistoryAsync(
                fromCurrency,
                toCurrency,
                days
            );

            return Ok(ResponseDto<List<ExchangeRateHistoryDto>>.SuccessResponse(history));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching rate history"
            ));
        }
    }

    // GET: api/Exchange/favorites
    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavoritePairs()
    {
        try
        {
            var userId = GetCurrentUserId();
            var pairs = await _exchangeService.GetFavoritePairsAsync(userId);

            return Ok(ResponseDto<List<CurrencyPairDto>>.SuccessResponse(pairs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching favorite pairs"
            ));
        }
    }

    // POST: api/Exchange/favorites
    [HttpPost("favorites")]
    public async Task<IActionResult> AddFavoritePair([FromBody] AddFavoritePairDto pairDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid pair data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();
            var result = await _exchangeService.AddFavoritePairAsync(
                userId,
                pairDto.FromCurrency,
                pairDto.ToCurrency
            );

            if (!result)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "This currency pair is already in your favorites"
                ));
            }

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Currency pair added to favorites"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while adding favorite pair"
            ));
        }
    }

    // DELETE: api/Exchange/favorites/{id}
    [HttpDelete("favorites/{id}")]
    public async Task<IActionResult> RemoveFavoritePair(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _exchangeService.RemoveFavoritePairAsync(userId, id);

            if (!result)
            {
                return NotFound(ResponseDto<object>.ErrorResponse(
                    "Favorite pair not found"
                ));
            }

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Currency pair removed from favorites"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while removing favorite pair"
            ));
        }
    }

    // GET: api/Exchange/alerts
    [HttpGet("alerts")]
    public async Task<IActionResult> GetRateAlerts()
    {
        try
        {
            var userId = GetCurrentUserId();
            var alerts = await _exchangeService.GetRateAlertsAsync(userId);

            return Ok(ResponseDto<List<RateAlertDto>>.SuccessResponse(alerts));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching rate alerts"
            ));
        }
    }

    // POST: api/Exchange/alerts
    [HttpPost("alerts")]
    public async Task<IActionResult> CreateRateAlert([FromBody] CreateRateAlertDto alertDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid alert data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();
            var alert = await _exchangeService.CreateRateAlertAsync(userId, alertDto);

            return Ok(ResponseDto<RateAlertDto>.SuccessResponse(
                alert,
                "Rate alert created successfully"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while creating rate alert"
            ));
        }
    }

    // DELETE: api/Exchange/alerts/{id}
    [HttpDelete("alerts/{id}")]
    public async Task<IActionResult> DeleteRateAlert(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _exchangeService.DeleteRateAlertAsync(userId, id);

            if (!result)
            {
                return NotFound(ResponseDto<object>.ErrorResponse(
                    "Rate alert not found"
                ));
            }

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Rate alert deleted successfully"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while deleting rate alert"
            ));
        }
    }

    // GET: api/Exchange/transactions
    [HttpGet("transactions")]
    public async Task<IActionResult> GetExchangeHistory([FromQuery] int limit = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            var history = await _exchangeService.GetExchangeHistoryAsync(userId, limit);

            return Ok(ResponseDto<List<ExchangeTransactionDto>>.SuccessResponse(history));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching exchange history"
            ));
        }
    }

    // GET: api/Exchange/trends/{currency}
    [HttpGet("trends/{currency}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrencyTrends(string currency, [FromQuery] int days = 7)
    {
        try
        {
            if (days < 1 || days > 30)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Days must be between 1 and 30"
                ));
            }

            var trends = await _exchangeService.GetCurrencyTrendsAsync(currency, days);

            return Ok(ResponseDto<CurrencyTrendsDto>.SuccessResponse(trends));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching currency trends"
            ));
        }
    }

    // GET: api/Exchange/popular
    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPopularPairs()
    {
        try
        {
            var pairs = await _exchangeService.GetPopularPairsAsync();

            return Ok(ResponseDto<List<PopularCurrencyPairDto>>.SuccessResponse(pairs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching popular pairs"
            ));
        }
    }

    // POST: api/Exchange/calculate-fee
    [HttpPost("calculate-fee")]
    public async Task<IActionResult> CalculateExchangeFee([FromBody] GetExchangeQuoteDto feeDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid fee calculation request",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var fee = await _exchangeService.CalculateExchangeFeeAsync(
                feeDto.Amount,
                feeDto.FromCurrency,
                feeDto.ToCurrency
            );

            return Ok(ResponseDto<ExchangeFeeDto>.SuccessResponse(fee));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while calculating fee"
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