using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
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

            if (stocks == null || stocks.Count == 0)
            {
                // Return 202 Accepted with a message that data is being loaded
                return Accepted(ResponseDto<object>.SuccessResponse(
                    new { message = "Stock data is being loaded. Please try again in a moment." },
                    "Data initialization in progress"
                ));
            }

            return Ok(ResponseDto<List<StockDto>>.SuccessResponse(
                stocks,
                $"Retrieved {stocks.Count} popular stocks"
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
                // Stock not found or not yet cached
                return NotFound(ResponseDto<object>.SuccessResponse(
                    new
                    {
                        message = $"Stock {symbol} is not currently available. It has been added to tracking and will be available shortly.",
                        symbol = symbol.ToUpper()
                    },
                    "Stock added to tracking"
                ));
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

            // Even if no results, return success with empty list
            return Ok(ResponseDto<StockSearchResultDto>.SuccessResponse(
                new StockSearchResultDto
                {
                    Stocks = stocks,
                    TotalResults = stocks.Count
                },
                stocks.Count > 0
                    ? $"Found {stocks.Count} stocks matching '{query}'"
                    : $"No stocks found matching '{query}'"
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

    // GET: api/Stock/status
    [HttpGet("status")]
    [Authorize] // Available to all authenticated users
    public async Task<IActionResult> GetServiceStatus()
    {
        try
        {
            var status = await _stockService.GetServiceStatusAsync();

            // Prepare a simplified status for regular users
            var simplifiedStatus = new
            {
                DataAvailable = status.CacheStatistics.CachedStockCount > 0,
                CachedStocks = status.CacheStatistics.CachedStockCount,
                LastUpdate = status.WorkerStatus.LastFullUpdate,
                NextUpdateIn = status.WorkerStatus.NextUpdateIn.TotalMinutes,
                RateLimitRemaining = status.RateLimitStatus.RemainingRequests
            };

            return Ok(ResponseDto<object>.SuccessResponse(
                simplifiedStatus,
                "Stock service status"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service status");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching service status"
            ));
        }
    }

    // POST: api/Stock/track
    [HttpPost("track")]
    [Authorize(Roles = "Admin")] // Admin only
    public async Task<IActionResult> TrackStock([FromBody] TrackStockDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.Symbol))
            {
                return BadRequest(ResponseDto<object>.ErrorResponse("Stock symbol is required"));
            }

            var success = await _stockService.AddStockToTrackingAsync(request.Symbol);

            if (success)
            {
                return Ok(ResponseDto<object>.SuccessResponse(
                    new { Symbol = request.Symbol.ToUpper() },
                    $"Stock {request.Symbol} added to tracking"
                ));
            }

            return BadRequest(ResponseDto<object>.ErrorResponse(
                $"Unable to add {request.Symbol} to tracking at this time"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding stock {request?.Symbol} to tracking");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while adding the stock to tracking"
            ));
        }
    }
}


public class TrackStockDto
{
    public string Symbol { get; set; }
}