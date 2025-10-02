using AutoMapper;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;
    private readonly IMapper _mapper;

    public CurrencyController(ICurrencyService currencyService, IMapper mapper)
    {
        _currencyService = currencyService;
        _mapper = mapper;
    }

    // GET: api/Currency
    [HttpGet]
    public async Task<IActionResult> GetAllCurrencies()
    {
        try
        {
            var currencies = await _currencyService.GetAllCurrenciesAsync();
            var currencyDtos = _mapper.Map<List<CurrencyDto>>(currencies);

            return Ok(ResponseDto<List<CurrencyDto>>.SuccessResponse(currencyDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching currencies"
            ));
        }
    }

    // GET: api/Currency/{code}
    [HttpGet("{code}")]
    public async Task<IActionResult> GetCurrency(string code)
    {
        try
        {
            var currency = await _currencyService.GetCurrencyAsync(code);

            if (currency == null)
                return NotFound(ResponseDto<object>.ErrorResponse($"Currency {code} not found"));

            var currencyDto = _mapper.Map<CurrencyDto>(currency);

            return Ok(ResponseDto<CurrencyDto>.SuccessResponse(currencyDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching currency"
            ));
        }
    }

    // GET: api/Currency/rate
    [HttpGet("rate")]
    public async Task<IActionResult> GetExchangeRate([FromQuery] string from, [FromQuery] string to)
    {
        try
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Both 'from' and 'to' currency codes are required"
                ));
            }

            var rate = await _currencyService.GetExchangeRateAsync(from, to);

            var result = new ExchangeRateDto
            {
                FromCurrency = from.ToUpper(),
                ToCurrency = to.ToUpper(),
                Rate = rate,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ResponseDto<ExchangeRateDto>.SuccessResponse(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching exchange rate"
            ));
        }
    }

    // POST: api/Currency/convert
    [HttpPost("convert")]
    public async Task<IActionResult> ConvertCurrency([FromBody] CurrencyConversionDto conversionDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid conversion data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var convertedAmount = await _currencyService.ConvertCurrencyAsync(
                conversionDto.Amount,
                conversionDto.FromCurrency,
                conversionDto.ToCurrency
            );

            var rate = await _currencyService.GetExchangeRateAsync(
                conversionDto.FromCurrency,
                conversionDto.ToCurrency
            );

            var result = new CurrencyConversionResultDto
            {
                OriginalAmount = conversionDto.Amount,
                ConvertedAmount = convertedAmount,
                FromCurrency = conversionDto.FromCurrency.ToUpper(),
                ToCurrency = conversionDto.ToCurrency.ToUpper(),
                ExchangeRate = rate,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ResponseDto<CurrencyConversionResultDto>.SuccessResponse(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while converting currency"
            ));
        }
    }

    // GET: api/Currency/rates
    [HttpGet("rates")]
    public async Task<IActionResult> GetAllExchangeRates()
    {
        try
        {
            var rates = await _currencyService.GetAllExchangeRatesAsync();

            return Ok(ResponseDto<Dictionary<string, decimal>>.SuccessResponse(rates));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching exchange rates"
            ));
        }
    }

    // PUT: api/Currency/{code}/rate
    [HttpPut("{code}/rate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateExchangeRate(string code, [FromBody] UpdateExchangeRateDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid rate data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var result = await _currencyService.UpdateExchangeRateAsync(code, updateDto.NewRate);

            if (!result)
                return NotFound(ResponseDto<object>.ErrorResponse($"Currency {code} not found"));

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                $"Exchange rate for {code.ToUpper()} updated successfully"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating exchange rate"
            ));
        }
    }
}