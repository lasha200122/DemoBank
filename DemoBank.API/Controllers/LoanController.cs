using AutoMapper;
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
public class LoanController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly IMapper _mapper;

    public LoanController(ILoanService loanService, IMapper mapper)
    {
        _loanService = loanService;
        _mapper = mapper;
    }

    // POST: api/Loan/apply
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyForLoan([FromBody] LoanApplicationDto applicationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid loan application data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();
            var result = await _loanService.ApplyForLoanAsync(userId, applicationDto);

            return Ok(ResponseDto<LoanApplicationResultDto>.SuccessResponse(
                result,
                "Loan application submitted successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing loan application"
            ));
        }
    }

    // GET: api/Loan/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoan(Guid id)
    {
        try
        {
            var loan = await _loanService.GetLoanByIdAsync(id);

            if (loan == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Loan not found"));

            // Check if user owns this loan or is admin
            var userId = GetCurrentUserId();
            if (loan.Id != id && !IsAdmin())
                return Forbid();

            return Ok(ResponseDto<LoanDto>.SuccessResponse(loan));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching loan details"
            ));
        }
    }

    // GET: api/Loan
    [HttpGet]
    public async Task<IActionResult> GetMyLoans()
    {
        try
        {
            var userId = GetCurrentUserId();
            var loans = await _loanService.GetUserLoansAsync(userId);

            return Ok(ResponseDto<List<LoanDto>>.SuccessResponse(loans));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching loans"
            ));
        }
    }

    // GET: api/Loan/pending
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingLoans()
    {
        try
        {
            var loans = await _loanService.GetPendingLoansAsync();

            return Ok(ResponseDto<List<LoanDto>>.SuccessResponse(loans));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching pending loans"
            ));
        }
    }

    // POST: api/Loan/{id}/approve
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveLoan(Guid id, [FromBody] LoanApprovalDto approvalDto)
    {
        try
        {
            var result = await _loanService.ApproveLoanAsync(id, approvalDto);

            return Ok(ResponseDto<LoanApprovalResultDto>.SuccessResponse(
                result,
                "Loan approved successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while approving loan"
            ));
        }
    }

    // POST: api/Loan/{id}/reject
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectLoan(Guid id, [FromBody] RejectLoanDto rejectDto)
    {
        try
        {
            var result = await _loanService.RejectLoanAsync(id, rejectDto.Reason);

            return Ok(ResponseDto<LoanApprovalResultDto>.SuccessResponse(
                result,
                "Loan rejected"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while rejecting loan"
            ));
        }
    }

    // GET: api/Loan/{id}/schedule
    [HttpGet("{id}/schedule")]
    public async Task<IActionResult> GetPaymentSchedule(Guid id)
    {
        try
        {
            var schedule = await _loanService.GetPaymentScheduleAsync(id);

            if (schedule == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Loan not found"));

            return Ok(ResponseDto<PaymentScheduleDto>.SuccessResponse(schedule));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching payment schedule"
            ));
        }
    }

    // POST: api/Loan/{id}/pay
    [HttpPost("{id}/pay")]
    public async Task<IActionResult> MakeLoanPayment(Guid id, [FromBody] MakeLoanPaymentDto paymentDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid payment data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();
            var result = await _loanService.MakeLoanPaymentAsync(userId, id, paymentDto.Amount);

            return Ok(ResponseDto<LoanPaymentResultDto>.SuccessResponse(
                result,
                "Payment processed successfully"
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
                "An error occurred while processing payment"
            ));
        }
    }

    // GET: api/Loan/{id}/payments
    [HttpGet("{id}/payments")]
    public async Task<IActionResult> GetPaymentHistory(Guid id)
    {
        try
        {
            var payments = await _loanService.GetPaymentHistoryAsync(id);

            return Ok(ResponseDto<List<LoanPaymentHistoryDto>>.SuccessResponse(payments));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching payment history"
            ));
        }
    }

    // GET: api/Loan/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetLoanSummary()
    {
        try
        {
            var userId = GetCurrentUserId();
            var summary = await _loanService.GetLoanSummaryAsync(userId);

            return Ok(ResponseDto<LoanSummaryDto>.SuccessResponse(summary));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching loan summary"
            ));
        }
    }

    // POST: api/Loan/calculate
    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateLoan([FromBody] CalculateLoanDto calculateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid calculation data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var monthlyPayment = await _loanService.CalculateMonthlyPaymentAsync(
                calculateDto.Amount,
                calculateDto.InterestRate,
                calculateDto.TermMonths
            );

            var result = new
            {
                Amount = calculateDto.Amount,
                InterestRate = calculateDto.InterestRate,
                TermMonths = calculateDto.TermMonths,
                MonthlyPayment = monthlyPayment,
                TotalPayment = monthlyPayment * calculateDto.TermMonths,
                TotalInterest = (monthlyPayment * calculateDto.TermMonths) - calculateDto.Amount
            };

            return Ok(ResponseDto<object>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while calculating loan"
            ));
        }
    }

    // GET: api/Loan/eligibility
    [HttpGet("eligibility")]
    public async Task<IActionResult> CheckEligibility([FromQuery] decimal amount)
    {
        try
        {
            var userId = GetCurrentUserId();
            var eligibility = await _loanService.CheckLoanEligibilityAsync(userId, amount);

            return Ok(ResponseDto<LoanEligibilityDto>.SuccessResponse(eligibility));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while checking eligibility"
            ));
        }
    }

    // POST: api/Loan/{id}/autopay
    [HttpPost("{id}/autopay")]
    public async Task<IActionResult> SetAutoPay(Guid id, [FromBody] SetAutoPaymentDto autoPayDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid auto-pay data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var result = await _loanService.SetAutoPaymentAsync(id, autoPayDto.AccountId, autoPayDto.Enable);

            if (!result)
                return BadRequest(ResponseDto<object>.ErrorResponse("Failed to update auto-payment settings"));

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                autoPayDto.Enable ? "Auto-payment enabled" : "Auto-payment disabled"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating auto-payment settings"
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