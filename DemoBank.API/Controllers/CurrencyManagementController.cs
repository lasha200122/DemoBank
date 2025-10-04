using System.Security.Claims;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class CurrencyManagementController : ControllerBase
{
    private readonly ICurrencyManagementService _currencyManagementService;
    private readonly ILogger<CurrencyManagementController> _logger;

    public CurrencyManagementController(
        ICurrencyManagementService currencyManagementService,
        ILogger<CurrencyManagementController> logger)
    {
        _currencyManagementService = currencyManagementService;
        _logger = logger;
    }

    // POST: api/CurrencyManagement
    [HttpPost]
    public async Task<IActionResult> CreateCurrency([FromBody] CreateCurrencyDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid currency data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var username = User.Identity?.Name ?? "System";
            var currency = await _currencyManagementService.CreateCurrencyAsync(dto, username);

            return Ok(ResponseDto<CurrencyDetailsDto>.SuccessResponse(
                currency,
                $"Currency {currency.Code} created successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating currency");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while creating the currency"
            ));
        }
    }

    // PUT: api/CurrencyManagement/{code}
    [HttpPut("{code}")]
    public async Task<IActionResult> UpdateCurrency(string code, [FromBody] UpdateCurrencyDto dto)
    {
        try
        {
            var username = User.Identity?.Name ?? "System";
            var currency = await _currencyManagementService.UpdateCurrencyAsync(code, dto, username);

            return Ok(ResponseDto<CurrencyDetailsDto>.SuccessResponse(
                currency,
                $"Currency {currency.Code} updated successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency {Code}", code);
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating the currency"
            ));
        }
    }

    // DELETE: api/CurrencyManagement/{code}
    [HttpDelete("{code}")]
    public async Task<IActionResult> DeleteCurrency(string code)
    {
        try
        {
            var result = await _currencyManagementService.DeleteCurrencyAsync(code);

            if (!result)
                return NotFound(ResponseDto<object>.ErrorResponse($"Currency {code} not found"));

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                $"Currency {code} deleted successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting currency {Code}", code);
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while deleting the currency"
            ));
        }
    }

    // PUT: api/CurrencyManagement/{code}/toggle-status
    [HttpPut("{code}/toggle-status")]
    public async Task<IActionResult> ToggleCurrencyStatus(string code, [FromQuery] bool isActive)
    {
        try
        {
            var result = await _currencyManagementService.ToggleCurrencyStatusAsync(code, isActive);

            if (!result)
                return NotFound(ResponseDto<object>.ErrorResponse($"Currency {code} not found"));

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                $"Currency {code} status updated to {(isActive ? "active" : "inactive")}"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling currency status for {Code}", code);
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating currency status"
            ));
        }
    }

    // GET: api/CurrencyManagement/all
    [HttpGet("all")]
    [AllowAnonymous] // Allow users to see available currencies
    public async Task<IActionResult> GetAllCurrencies([FromQuery] bool includeInactive = false)
    {
        try
        {
            // Only admins can see inactive currencies
            if (includeInactive && !User.IsInRole("Admin"))
                includeInactive = false;

            var currencies = await _currencyManagementService.GetAllCurrenciesDetailedAsync(includeInactive);

            return Ok(ResponseDto<List<CurrencyDetailsDto>>.SuccessResponse(currencies));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currencies");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching currencies"
            ));
        }
    }

    // GET: api/CurrencyManagement/{code}
    [HttpGet("{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrencyDetails(string code)
    {
        try
        {
            var currency = await _currencyManagementService.GetCurrencyDetailsAsync(code);

            if (currency == null)
                return NotFound(ResponseDto<object>.ErrorResponse($"Currency {code} not found"));

            return Ok(ResponseDto<CurrencyDetailsDto>.SuccessResponse(currency));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currency details for {Code}", code);
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching currency details"
            ));
        }
    }

    // GET: api/CurrencyManagement/crypto
    [HttpGet("crypto")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCryptoCurrencies()
    {
        try
        {
            var currencies = await _currencyManagementService.GetCryptoCurrenciesAsync();

            return Ok(ResponseDto<List<CurrencyDetailsDto>>.SuccessResponse(currencies));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching crypto currencies");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching crypto currencies"
            ));
        }
    }

    // GET: api/CurrencyManagement/fiat
    [HttpGet("fiat")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFiatCurrencies()
    {
        try
        {
            var currencies = await _currencyManagementService.GetFiatCurrenciesAsync();

            return Ok(ResponseDto<List<CurrencyDetailsDto>>.SuccessResponse(currencies));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fiat currencies");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching fiat currencies"
            ));
        }
    }

    // POST: api/CurrencyManagement/update-rates
    [HttpPost("update-rates")]
    public async Task<IActionResult> UpdateExchangeRates([FromBody] Dictionary<string, decimal> rates)
    {
        try
        {
            var result = await _currencyManagementService.UpdateExchangeRatesAsync(rates);

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Exchange rates updated successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exchange rates");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating exchange rates"
            ));
        }
    }

    // POST: api/CurrencyManagement/update-crypto-prices
    [HttpPost("update-crypto-prices")]
    public async Task<IActionResult> UpdateCryptoPrices()
    {
        try
        {
            var result = await _currencyManagementService.UpdateCryptoPricesAsync();

            if (!result)
                return BadRequest(ResponseDto<object>.ErrorResponse("Failed to update crypto prices"));

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Crypto prices updated successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating crypto prices");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating crypto prices"
            ));
        }
    }

    // GET: api/CurrencyManagement/{code}/statistics
    [HttpGet("{code}/statistics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrencyStatistics(string code)
    {
        try
        {
            var statistics = await _currencyManagementService.GetCurrencyStatisticsAsync(code);

            if (statistics == null)
                return NotFound(ResponseDto<object>.ErrorResponse($"Currency {code} not found"));

            return Ok(ResponseDto<CurrencyStatisticsDto>.SuccessResponse(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currency statistics for {Code}", code);
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching currency statistics"
            ));
        }
    }

    // GET: api/CurrencyManagement/{code}/price-history
    [HttpGet("{code}/price-history")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPriceHistory(string code, [FromQuery] int days = 7)
    {
        try
        {
            if (days < 1 || days > 365)
                return BadRequest(ResponseDto<object>.ErrorResponse("Days must be between 1 and 365"));

            var history = await _currencyManagementService.GetPriceHistoryAsync(code, days);

            if (history == null)
                return NotFound(ResponseDto<object>.ErrorResponse($"Currency {code} not found"));

            return Ok(ResponseDto<CurrencyPriceHistoryDto>.SuccessResponse(history));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price history for {Code}", code);
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching price history"
            ));
        }
    }
}

// Crypto specific controller for users
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CryptoController : ControllerBase
{
    private readonly ICurrencyManagementService _currencyManagementService;
    private readonly IExchangeService _exchangeService;
    private readonly IAccountService _accountService;
    private readonly ILogger<CryptoController> _logger;

    public CryptoController(
        ICurrencyManagementService currencyManagementService,
        IExchangeService exchangeService,
        IAccountService accountService,
        ILogger<CryptoController> logger)
    {
        _currencyManagementService = currencyManagementService;
        _exchangeService = exchangeService;
        _accountService = accountService;
        _logger = logger;
    }

    // GET: api/Crypto/wallets
    [HttpGet("wallets")]
    public async Task<IActionResult> GetMyCryptoWallets()
    {
        try
        {
            var userId = GetCurrentUserId();
            var wallets = await _currencyManagementService.GetUserCryptoWalletsAsync(userId);

            return Ok(ResponseDto<List<CryptoWalletDto>>.SuccessResponse(wallets));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching crypto wallets");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching crypto wallets"
            ));
        }
    }

    // GET: api/Crypto/wallet/{currency}
    [HttpGet("wallet/{currency}")]
    public async Task<IActionResult> GetOrCreateCryptoWallet(string currency)
    {
        try
        {
            var userId = GetCurrentUserId();
            var wallet = await _currencyManagementService.GetOrCreateCryptoWalletAsync(userId, currency);

            return Ok(ResponseDto<CryptoWalletDto>.SuccessResponse(wallet));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting/creating crypto wallet for {Currency}", currency);
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while getting/creating crypto wallet"
            ));
        }
    }

    // POST: api/Crypto/exchange
    [HttpPost("exchange")]
    public async Task<IActionResult> ExchangeCrypto([FromBody] CryptoExchangeDto exchangeDto)
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

            // Map to standard exchange request
            var exchangeRequest = new ExchangeRequestDto
            {
                FromAccountId = exchangeDto.FromAccountId,
                ToAccountId = exchangeDto.ToAccountId,
                Amount = exchangeDto.Amount,
                ToCurrency = exchangeDto.ToCurrency,
                Description = $"Exchange {exchangeDto.FromCurrency} to {exchangeDto.ToCurrency}"
            };

            var result = await _exchangeService.ExchangeCurrencyAsync(userId, exchangeRequest);

            return Ok(ResponseDto<ExchangeResultDto>.SuccessResponse(
                result,
                "Crypto exchange completed successfully"
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
            _logger.LogError(ex, "Error processing crypto exchange");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing the crypto exchange"
            ));
        }
    }

    // POST: api/Crypto/validate-address
    [HttpPost("validate-address")]
    public async Task<IActionResult> ValidateCryptoAddress([FromBody] ValidateCryptoAddressDto dto)
    {
        try
        {
            var isValid = await _currencyManagementService.ValidateCryptoAddressAsync(
                dto.Address,
                dto.Currency,
                dto.Network
            );

            return Ok(ResponseDto<bool>.SuccessResponse(
                isValid,
                isValid ? "Valid address" : "Invalid address"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating crypto address");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while validating the address"
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