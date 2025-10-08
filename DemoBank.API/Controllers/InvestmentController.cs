using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InvestmentController : ControllerBase
{
    private readonly IInvestmentService _investmentService;
    private readonly IInvestmentCalculatorService _calculatorService;
    private readonly ILogger<InvestmentController> _logger;

    public InvestmentController(
        IInvestmentService investmentService,
        IInvestmentCalculatorService calculatorService,
        ILogger<InvestmentController> logger)
    {
        _investmentService = investmentService;
        _calculatorService = calculatorService;
        _logger = logger;
    }

    // POST: api/Investment/apply
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyForInvestment([FromBody] CreateInvestmentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid investment application",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();
            var investment = await _investmentService.ApplyForInvestmentAsync(userId, dto);

            return Ok(ResponseDto<InvestmentDto>.SuccessResponse(
                investment,
                "Investment application submitted successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying for investment");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing your investment application"
            ));
        }
    }

    // GET: api/Investment
    [HttpGet]
    public async Task<IActionResult> GetMyInvestments()
    {
        try
        {
            var userId = GetCurrentUserId();
            var investments = await _investmentService.GetUserInvestmentsAsync(userId);

            return Ok(ResponseDto<List<InvestmentDto>>.SuccessResponse(investments));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user investments");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching your investments"
            ));
        }
    }

    // GET: api/Investment/active
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveInvestments()
    {
        try
        {
            var userId = GetCurrentUserId();
            var investments = await _investmentService.GetActiveInvestmentsAsync(userId);

            return Ok(ResponseDto<List<InvestmentDto>>.SuccessResponse(investments));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active investments");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching active investments"
            ));
        }
    }

    // GET: api/Investment/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvestmentDetails(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var investment = await _investmentService.GetInvestmentDetailsAsync(id, userId);

            if (investment == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Investment not found"));

            return Ok(ResponseDto<InvestmentDetailsDto>.SuccessResponse(investment));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment details");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching investment details"
            ));
        }
    }

    // GET: api/Investment/analytics
    [HttpGet("analytics")]
    public async Task<IActionResult> GetInvestmentAnalytics()
    {
        try
        {
            var userId = GetCurrentUserId();
            var analytics = await _investmentService.GetUserAnalyticsAsync(userId);

            return Ok(ResponseDto<InvestmentAnalyticsDto>.SuccessResponse(analytics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment analytics");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching analytics"
            ));
        }
    }

    // POST: api/Investment/calculate
    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateReturns([FromBody] InvestmentCalculatorDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid calculation parameters",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var result = await _investmentService.CalculateReturnsAsync(dto);

            return Ok(ResponseDto<InvestmentCalculatorResultDto>.SuccessResponse(
                result,
                "Investment returns calculated successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating investment returns");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while calculating returns"
            ));
        }
    }

    // GET: api/Investment/projections
    [HttpGet("projections")]
    public async Task<IActionResult> GetProjections()
    {
        try
        {
            var userId = GetCurrentUserId();
            var projections = await _investmentService.GetProjectionsAsync(userId);

            return Ok(ResponseDto<ProjectionsDto>.SuccessResponse(projections));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching projections");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching projections"
            ));
        }
    }

    // GET: api/Investment/plans
    [HttpGet("plans")]
    public async Task<IActionResult> GetAvailablePlans()
    {
        try
        {
            var plans = await _investmentService.GetAvailablePlansAsync();

            return Ok(ResponseDto<List<InvestmentPlanDto>>.SuccessResponse(plans));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment plans");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching investment plans"
            ));
        }
    }

    // GET: api/Investment/plans/{id}
    [HttpGet("plans/{id}")]
    public async Task<IActionResult> GetPlanDetails(Guid id)
    {
        try
        {
            var plan = await _investmentService.GetPlanDetailsAsync(id);

            if (plan == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Investment plan not found"));

            return Ok(ResponseDto<InvestmentPlanDto>.SuccessResponse(plan));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching plan details");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching plan details"
            ));
        }
    }

    // POST: api/Investment/withdraw
    [HttpPost("withdraw")]
    public async Task<IActionResult> WithdrawInvestment([FromBody] WithdrawInvestmentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid withdrawal request",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();
            var result = await _investmentService.WithdrawInvestmentAsync(userId, dto);

            if (!result.Success)
            {
                return BadRequest(ResponseDto<InvestmentWithdrawalResultDto>.ErrorResponse(result.Message));
            }

            return Ok(ResponseDto<InvestmentWithdrawalResultDto>.SuccessResponse(
                result,
                "Withdrawal processed successfully"
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
            _logger.LogError(ex, "Error processing withdrawal");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing your withdrawal"
            ));
        }
    }

    // GET: api/Investment/{id}/returns
    [HttpGet("{id}/returns")]
    public async Task<IActionResult> GetInvestmentReturns(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var returns = await _investmentService.GetInvestmentReturnsAsync(id, userId);

            return Ok(ResponseDto<List<InvestmentReturnDto>>.SuccessResponse(returns));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment returns");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching returns"
            ));
        }
    }

    // POST: api/Investment/calculator/compare
    [HttpPost("calculator/compare")]
    public async Task<IActionResult> ComparePlans([FromBody] ComparePlansDto dto)
    {
        try
        {
            var result = await _calculatorService.CompareInvestmentPlansAsync(
                dto.Amount,
                dto.TermMonths,
                dto.PlanIds
            );

            return Ok(ResponseDto<Dictionary<string, InvestmentCalculatorResultDto>>.SuccessResponse(
                result,
                "Comparison generated successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing investment plans");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while comparing plans"
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

public class ComparePlansDto
{
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }
    public List<Guid> PlanIds { get; set; }
}
