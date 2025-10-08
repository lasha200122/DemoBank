using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class InvestmentAdminController : ControllerBase
{
    private readonly IInvestmentAdminService _adminService;
    private readonly ILogger<InvestmentAdminController> _logger;

    public InvestmentAdminController(
        IInvestmentAdminService adminService,
        ILogger<InvestmentAdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    // GET: api/InvestmentAdmin/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        try
        {
            var dashboard = await _adminService.GetAdminDashboardAsync();

            return Ok(ResponseDto<AdminInvestmentDashboardDto>.SuccessResponse(dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin dashboard");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching dashboard"
            ));
        }
    }

    // GET: api/InvestmentAdmin/pending
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingInvestments()
    {
        try
        {
            var pending = await _adminService.GetPendingInvestmentsAsync();

            return Ok(ResponseDto<List<PendingInvestmentDto>>.SuccessResponse(pending));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending investments");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching pending investments"
            ));
        }
    }

    // POST: api/InvestmentAdmin/approve/{id}
    [HttpPost("approve/{id}")]
    public async Task<IActionResult> ApproveInvestment(Guid id, [FromBody] InvestmentApprovalDto dto)
    {
        try
        {
            var approvedBy = User.Identity?.Name ?? "System";
            var investment = await _adminService.ApproveInvestmentAsync(id, dto, approvedBy);

            return Ok(ResponseDto<InvestmentDto>.SuccessResponse(
                investment,
                "Investment approved successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving investment");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while approving investment"
            ));
        }
    }

    // POST: api/InvestmentAdmin/reject/{id}
    [HttpPost("reject/{id}")]
    public async Task<IActionResult> RejectInvestment(Guid id, [FromBody] RejectInvestmentDto dto)
    {
        try
        {
            var rejectedBy = User.Identity?.Name ?? "System";
            var investment = await _adminService.RejectInvestmentAsync(id, dto.Reason, rejectedBy);

            return Ok(ResponseDto<InvestmentDto>.SuccessResponse(
                investment,
                "Investment rejected"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting investment");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while rejecting investment"
            ));
        }
    }

    // PUT: api/InvestmentAdmin/rates/investment
    [HttpPut("rates/investment")]
    public async Task<IActionResult> UpdateInvestmentRate([FromBody] UpdateInvestmentRateDto dto)
    {
        try
        {
            var updatedBy = User.Identity?.Name ?? "System";
            var result = await _adminService.UpdateInvestmentRateAsync(dto, updatedBy);

            return Ok(ResponseDto<bool>.SuccessResponse(
                result,
                "Investment rate updated successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating investment rate");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating rate"
            ));
        }
    }

    // POST: api/InvestmentAdmin/rates/user
    [HttpPost("rates/user")]
    public async Task<IActionResult> SetUserRate([FromBody] UserInvestmentRateDto dto)
    {
        try
        {
            var createdBy = User.Identity?.Name ?? "System";
            var result = await _adminService.SetUserRateAsync(dto, createdBy);

            return Ok(ResponseDto<bool>.SuccessResponse(
                result,
                "User rate set successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting user rate");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while setting user rate"
            ));
        }
    }

    // PUT: api/InvestmentAdmin/rates/bulk
    [HttpPut("rates/bulk")]
    public async Task<IActionResult> BulkUpdateRates([FromBody] BulkRateUpdateDto dto)
    {
        try
        {
            var updatedBy = User.Identity?.Name ?? "System";
            var result = await _adminService.BulkUpdateRatesAsync(dto, updatedBy);

            return Ok(ResponseDto<bool>.SuccessResponse(
                result,
                "Rates updated successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rates in bulk");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating rates"
            ));
        }
    }

    // GET: api/InvestmentAdmin/rates/user/{userId}
    [HttpGet("rates/user/{userId}")]
    public async Task<IActionResult> GetUserRates(Guid userId)
    {
        try
        {
            var rates = await _adminService.GetUserRatesAsync(userId);

            return Ok(ResponseDto<object>.SuccessResponse(rates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user rates");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching rates"
            ));
        }
    }

    // GET: api/InvestmentAdmin/rates/all-users
    [HttpGet("rates/all-users")]
    public async Task<IActionResult> GetAllUserROIs()
    {
        try
        {
            var rates = await _adminService.GetAllUserROIsAsync();

            return Ok(ResponseDto<Dictionary<Guid, decimal>>.SuccessResponse(rates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all user ROIs");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching ROIs"
            ));
        }
    }

    // POST: api/InvestmentAdmin/plans
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateInvestmentPlanDto dto)
    {
        try
        {
            var createdBy = User.Identity?.Name ?? "System";
            var plan = await _adminService.CreatePlanAsync(dto, createdBy);

            return Ok(ResponseDto<InvestmentPlanDto>.SuccessResponse(
                plan,
                "Investment plan created successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating investment plan");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while creating plan"
            ));
        }
    }

    // PUT: api/InvestmentAdmin/plans/{id}
    [HttpPut("plans/{id}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateInvestmentPlanDto dto)
    {
        try
        {
            var updatedBy = User.Identity?.Name ?? "System";
            var plan = await _adminService.UpdatePlanAsync(id, dto, updatedBy);

            return Ok(ResponseDto<InvestmentPlanDto>.SuccessResponse(
                plan,
                "Investment plan updated successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating investment plan");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating plan"
            ));
        }
    }

    // DELETE: api/InvestmentAdmin/plans/{id}
    [HttpDelete("plans/{id}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        try
        {
            var result = await _adminService.DeletePlanAsync(id);

            if (!result)
                return NotFound(ResponseDto<object>.ErrorResponse("Plan not found"));

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Investment plan deleted successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting investment plan");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while deleting plan"
            ));
        }
    }

    // GET: api/InvestmentAdmin/users/{userId}/overview
    [HttpGet("users/{userId}/overview")]
    public async Task<IActionResult> GetUserInvestmentOverview(Guid userId)
    {
        try
        {
            var overview = await _adminService.GetUserInvestmentOverviewAsync(userId);

            return Ok(ResponseDto<UserInvestmentOverviewDto>.SuccessResponse(overview));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user investment overview");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching overview"
            ));
        }
    }

    // GET: api/InvestmentAdmin/investors
    [HttpGet("investors")]
    public async Task<IActionResult> GetAllInvestors()
    {
        try
        {
            var investors = await _adminService.GetAllInvestorsAsync();

            return Ok(ResponseDto<List<UserInvestmentOverviewDto>>.SuccessResponse(investors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investors");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching investors"
            ));
        }
    }

    // POST: api/InvestmentAdmin/payouts/process
    [HttpPost("payouts/process")]
    public async Task<IActionResult> ProcessScheduledPayouts()
    {
        try
        {
            var result = await _adminService.ProcessScheduledPayoutsAsync();

            return Ok(ResponseDto<bool>.SuccessResponse(
                result,
                "Scheduled payouts processed successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled payouts");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing payouts"
            ));
        }
    }

    // POST: api/InvestmentAdmin/payouts/manual
    [HttpPost("payouts/manual")]
    public async Task<IActionResult> ProcessManualPayout([FromBody] ManualPayoutDto dto)
    {
        try
        {
            var processedBy = User.Identity?.Name ?? "System";
            var result = await _adminService.ManualPayoutAsync(dto.InvestmentId, dto.Amount, processedBy);

            return Ok(ResponseDto<bool>.SuccessResponse(
                result,
                "Manual payout processed successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing manual payout");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing payout"
            ));
        }
    }

    // GET: api/InvestmentAdmin/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetFundSummary()
    {
        try
        {
            var summary = await _adminService.GetFundSummaryAsync();

            return Ok(ResponseDto<AdminInvestmentSummaryDto>.SuccessResponse(summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fund summary");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching summary"
            ));
        }
    }

    // GET: api/InvestmentAdmin/report
    [HttpGet("report")]
    public async Task<IActionResult> GenerateReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var report = await _adminService.GenerateInvestmentReportAsync(startDate, endDate);

            return Ok(ResponseDto<Dictionary<string, object>>.SuccessResponse(
                report,
                "Report generated successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while generating report"
            ));
        }
    }

    // GET: api/InvestmentAdmin/alerts
    [HttpGet("alerts")]
    public async Task<IActionResult> GetInvestmentAlerts()
    {
        try
        {
            var alerts = await _adminService.GetInvestmentAlertsAsync();

            return Ok(ResponseDto<List<InvestmentAlertDto>>.SuccessResponse(alerts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching investment alerts");
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching alerts"
            ));
        }
    }
}

public class RejectInvestmentDto
{
    public string Reason { get; set; }
}

public class ManualPayoutDto
{
    public Guid InvestmentId { get; set; }
    public decimal Amount { get; set; }
}