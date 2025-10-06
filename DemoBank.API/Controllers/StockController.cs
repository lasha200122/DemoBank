using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly ILogger<StockController> _logger;

    public StockController(IStockService stockService, ILogger<StockController> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    // GET: api/Stock/popular
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularStocks()
    {
        try
        {
            var stocks = await _stockService.GetPopularStocksAsync();

            return Ok(ResponseDto<List<StockDto>>.SuccessResponse(
                stocks,
                "Popular stocks retrieved successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular stocks");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching popular stocks"
            ));
        }
    }

    // GET: api/Stock/{symbol}
    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetStock(string symbol)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(ResponseDto<object>.ErrorResponse("Stock symbol is required"));
            }

            var stock = await _stockService.GetStockBySymbolAsync(symbol);

            if (stock == null)
            {
                return NotFound(ResponseDto<object>.ErrorResponse($"Stock {symbol} not found"));
            }

            return Ok(ResponseDto<StockDto>.SuccessResponse(
                stock,
                $"Stock {symbol} retrieved successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching stock {symbol}");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching stock data"
            ));
        }
    }

    // GET: api/Stock/search?query=apple
    [HttpGet("search")]
    public async Task<IActionResult> SearchStocks([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(ResponseDto<object>.ErrorResponse("Search query is required"));
            }

            var stocks = await _stockService.SearchStocksAsync(query);

            return Ok(ResponseDto<StockSearchResultDto>.SuccessResponse(
                new StockSearchResultDto
                {
                    Stocks = stocks,
                    TotalResults = stocks.Count
                },
                $"Found {stocks.Count} stocks matching '{query}'"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching stocks for query '{query}'");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while searching stocks"
            ));
        }
    }
}