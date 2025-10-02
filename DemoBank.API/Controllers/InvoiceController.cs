using System.Security.Claims;
using AutoMapper;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IMapper _mapper;

    public InvoiceController(IInvoiceService invoiceService, IMapper mapper)
    {
        _invoiceService = invoiceService;
        _mapper = mapper;
    }

    // POST: api/Invoice
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto invoiceDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid invoice data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();
            var invoice = await _invoiceService.CreateInvoiceAsync(userId, invoiceDto);

            return CreatedAtAction(
                nameof(GetInvoice),
                new { id = invoice.Id },
                ResponseDto<InvoiceDto>.SuccessResponse(invoice, "Invoice created successfully")
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while creating invoice"
            ));
        }
    }

    // GET: api/Invoice/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);

            if (invoice == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Invoice not found"));

            return Ok(ResponseDto<InvoiceDto>.SuccessResponse(invoice));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching invoice"
            ));
        }
    }

    // GET: api/Invoice/number/{number}
    [HttpGet("number/{number}")]
    public async Task<IActionResult> GetInvoiceByNumber(string number)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByNumberAsync(number);

            if (invoice == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Invoice not found"));

            return Ok(ResponseDto<InvoiceDto>.SuccessResponse(invoice));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching invoice"
            ));
        }
    }

    // GET: api/Invoice
    [HttpGet]
    public async Task<IActionResult> GetMyInvoices()
    {
        try
        {
            var userId = GetCurrentUserId();
            var invoices = await _invoiceService.GetUserInvoicesAsync(userId);

            return Ok(ResponseDto<List<InvoiceDto>>.SuccessResponse(invoices));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching invoices"
            ));
        }
    }

    // GET: api/Invoice/pending
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingInvoices()
    {
        try
        {
            var userId = GetCurrentUserId();
            var invoices = await _invoiceService.GetPendingInvoicesAsync(userId);

            return Ok(ResponseDto<List<InvoiceDto>>.SuccessResponse(invoices));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching pending invoices"
            ));
        }
    }

    // POST: api/Invoice/{id}/pay
    [HttpPost("{id}/pay")]
    public async Task<IActionResult> PayInvoice(Guid id, [FromBody] PayInvoiceDto paymentDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _invoiceService.PayInvoiceAsync(userId, id, paymentDto);

            return Ok(ResponseDto<InvoicePaymentResultDto>.SuccessResponse(
                result,
                "Invoice paid successfully"
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
                "An error occurred while paying invoice"
            ));
        }
    }

    // PUT: api/Invoice/{id}/send
    [HttpPut("{id}/send")]
    public async Task<IActionResult> SendInvoice(Guid id)
    {
        try
        {
            var result = await _invoiceService.SendInvoiceAsync(id);

            if (!result)
                return NotFound(ResponseDto<object>.ErrorResponse("Invoice not found"));

            return Ok(ResponseDto<object>.SuccessResponse(null, "Invoice sent successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while sending invoice"
            ));
        }
    }

    // PUT: api/Invoice/{id}/cancel
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelInvoice(Guid id)
    {
        try
        {
            var result = await _invoiceService.CancelInvoiceAsync(id);

            if (!result)
                return NotFound(ResponseDto<object>.ErrorResponse("Invoice not found"));

            return Ok(ResponseDto<object>.SuccessResponse(null, "Invoice cancelled successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while cancelling invoice"
            ));
        }
    }

    // PUT: api/Invoice/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceDto updateDto)
    {
        try
        {
            var invoice = await _invoiceService.UpdateInvoiceAsync(id, updateDto);

            return Ok(ResponseDto<InvoiceDto>.SuccessResponse(
                invoice,
                "Invoice updated successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating invoice"
            ));
        }
    }

    // POST: api/Invoice/search
    [HttpPost("search")]
    public async Task<IActionResult> SearchInvoices([FromBody] InvoiceSearchDto searchDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var invoices = await _invoiceService.SearchInvoicesAsync(userId, searchDto);

            return Ok(ResponseDto<List<InvoiceDto>>.SuccessResponse(invoices));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while searching invoices"
            ));
        }
    }

    // GET: api/Invoice/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetInvoiceSummary()
    {
        try
        {
            var userId = GetCurrentUserId();
            var summary = await _invoiceService.GetInvoiceSummaryAsync(userId);

            return Ok(ResponseDto<InvoiceSummaryDto>.SuccessResponse(summary));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching invoice summary"
            ));
        }
    }

    // GET: api/Invoice/overdue
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueInvoices()
    {
        try
        {
            var userId = GetCurrentUserId();
            var invoices = await _invoiceService.GetOverdueInvoicesAsync(userId);

            return Ok(ResponseDto<List<InvoiceDto>>.SuccessResponse(invoices));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching overdue invoices"
            ));
        }
    }

    // POST: api/Invoice/process-overdue
    [HttpPost("process-overdue")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessOverdueInvoices()
    {
        try
        {
            await _invoiceService.ProcessOverdueInvoicesAsync();

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Overdue invoices processed successfully"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while processing overdue invoices"
            ));
        }
    }

    // POST: api/Invoice/bulk
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateBulkInvoices([FromBody] List<CreateInvoiceDto> invoices)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid invoice data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = GetCurrentUserId();
            var createdInvoices = new List<InvoiceDto>();

            foreach (var invoiceDto in invoices)
            {
                var invoice = await _invoiceService.CreateInvoiceAsync(userId, invoiceDto);
                createdInvoices.Add(invoice);
            }

            return Ok(ResponseDto<List<InvoiceDto>>.SuccessResponse(
                createdInvoices,
                $"{createdInvoices.Count} invoices created successfully"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while creating bulk invoices"
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