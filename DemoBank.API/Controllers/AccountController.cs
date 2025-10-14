using AutoMapper;
using DemoBank.API.Data;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IMapper _mapper;
    private readonly DemoBankContext _context;

    public AccountController(IAccountService accountService, IMapper mapper, DemoBankContext context)
    {
        _accountService = accountService;
        _mapper = mapper;
        _context = context;
    }

    // GET: api/Account
    [HttpGet]
    public async Task<IActionResult> GetMyAccounts()
    {
        try
        {
            var userId = GetCurrentUserId();
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            var accountDtos = _mapper.Map<List<AccountDto>>(accounts);

            return Ok(ResponseDto<List<AccountDto>>.SuccessResponse(accountDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching accounts"
            ));
        }
    }

    // GET: api/Account/active
    [HttpGet("active")]
    public async Task<IActionResult> GetMyActiveAccounts()
    {
        try
        {
            var userId = GetCurrentUserId();
            var accounts = await _accountService.GetActiveUserAccountsAsync(userId);
            var accountDtos = _mapper.Map<List<AccountDto>>(accounts);

            return Ok(ResponseDto<List<AccountDto>>.SuccessResponse(accountDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching active accounts"
            ));
        }
    }

    // GET: api/Account/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var account = await _accountService.GetByIdAsync(id);

            if (account == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Account not found"));

            // Check if user owns this account or is admin
            if (account.UserId != userId && !IsAdmin())
                return Forbid();

            var accountDto = _mapper.Map<AccountDto>(account);

            return Ok(ResponseDto<AccountDto>.SuccessResponse(accountDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching account details"
            ));
        }
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetAccountsByUserId(Guid userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var accounts = await _accountService.GetAccountByUserIdAsync(userId);

            if (accounts == null || !accounts.Any())
                return NotFound(ResponseDto<object>.ErrorResponse("No accounts found for this user."));

            if (userId != currentUserId && !IsAdmin())
                return Forbid();

            var accountDtos = _mapper.Map<List<AccountDto>>(accounts);

            return Ok(ResponseDto<List<AccountDto>>.SuccessResponse(accountDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching account list"
            ));
        }
    }


    // GET: api/Account/{id}/transactions
    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetAccountTransactions(Guid id, [FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify user owns this account
            if (!await _accountService.UserOwnsAccountAsync(userId, id) && !IsAdmin())
                return Forbid();

            var transactions = await _accountService.GetAccountTransactionsAsync(id, limit);
            var transactionDtos = _mapper.Map<List<TransactionDto>>(transactions);

            return Ok(ResponseDto<List<TransactionDto>>.SuccessResponse(transactionDtos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching transactions"
            ));
        }
    }

    // POST: api/Account
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid account data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = createDto.UserId != null ? createDto.UserId : GetCurrentUserId();
            var account = await _accountService.CreateAccountAsync((Guid)userId, createDto);
            var accountDto = _mapper.Map<AccountDto>(account);

            return CreatedAtAction(
                nameof(GetAccount),
                new { id = account.Id },
                ResponseDto<AccountDto>.SuccessResponse(accountDto, "Account created successfully")
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while creating the account"
            ));
        }
    }

    // PUT: api/Account/{id}/priority
    [HttpPut("{id}/priority")]
    public async Task<IActionResult> SetPriorityAccount(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var account = await _accountService.GetByIdAsync(id);

            if (account == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Account not found"));

            if (account.UserId != userId)
                return Forbid();

            var result = await _accountService.SetPriorityAccountAsync(userId, id, account.Currency);

            if (!result)
                return BadRequest(ResponseDto<object>.ErrorResponse("Failed to set priority account"));

            return Ok(ResponseDto<object>.SuccessResponse(null, "Priority account updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while updating priority account"
            ));
        }
    }

    // PUT: api/Account/{id}/activate
    [HttpPut("{id}/activate")]
    public async Task<IActionResult> ActivateAccount(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var account = await _accountService.GetByIdAsync(id);

            if (account == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Account not found"));

            if (account.UserId != userId && !IsAdmin())
                return Forbid();

            var result = await _accountService.ActivateAccountAsync(id);

            if (!result)
                return BadRequest(ResponseDto<object>.ErrorResponse("Failed to activate account"));

            return Ok(ResponseDto<object>.SuccessResponse(null, "Account activated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while activating the account"
            ));
        }
    }

    // PUT: api/Account/{id}/deactivate
    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivateAccount(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var account = await _accountService.GetByIdAsync(id);

            if (account == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Account not found"));

            if (account.UserId != userId && !IsAdmin())
                return Forbid();

            var result = await _accountService.DeactivateAccountAsync(id);

            if (!result)
                return BadRequest(ResponseDto<object>.ErrorResponse("Failed to deactivate account"));

            return Ok(ResponseDto<object>.SuccessResponse(null, "Account deactivated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while deactivating the account"
            ));
        }
    }

    // GET: api/Account/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetAccountSummary()
    {
        try
        {
            var userId = GetCurrentUserId();

            var totalInUSD = await _accountService.GetTotalBalanceInUSDAsync(userId);
            var balancesByCurrency = await _accountService.GetBalancesByCurrencyAsync(userId);
            var accounts = await _accountService.GetActiveUserAccountsAsync(userId);

            var clientInvestments = await _context.ClientInvestment
              .Where(ci => ci.UserId == userId)
              .ToListAsync();

            // Monthly and yearly returns
            var monthlyReturnsUSD = accounts
                .Where(a => a.Currency == "USD")
                .Join(clientInvestments,
                      a => a.Id.ToString(),
                      ci => ci.AccountId,
                      (a, ci) => (a.Balance * ci.MonthlyReturn) / 100m)
                .Sum();

            var yearlyReturnsUSD = accounts
                .Where(a => a.Currency == "USD")
                .Join(clientInvestments,
                      a => a.Id.ToString(),
                      ci => ci.AccountId,
                      (a, ci) => (a.Balance * ci.YearlyReturn) / 100m)
                .Sum();

            var monthlyReturnsEUR = accounts
                .Where(a => a.Currency == "EUR")
                .Join(clientInvestments,
                      a => a.Id.ToString(),
                      ci => ci.AccountId,
                      (a, ci) => (a.Balance * ci.MonthlyReturn) / 100m)
                .Sum();

            var yearlyReturnsEUR = accounts
                .Where(a => a.Currency == "EUR")
                .Join(clientInvestments,
                      a => a.Id.ToString(),
                      ci => ci.AccountId,
                      (a, ci) => (a.Balance * ci.YearlyReturn) / 100m)
                .Sum();


            var summary = new AccountSummaryDto
            {
                TotalBalanceUSD = totalInUSD,
                BalancesByCurrency = balancesByCurrency,
                TotalAccounts = accounts.Count,
                ActiveAccounts = accounts.Count(a => a.IsActive),
                Accounts = _mapper.Map<List<AccountDto>>(accounts),
                MonthlyReturnsEUR = monthlyReturnsEUR,
                MonthlyReturnsUSD = monthlyReturnsUSD,
                YearlyReturnsEUR = yearlyReturnsEUR,
                YearlyReturnsUSD = yearlyReturnsUSD
            };

            return Ok(ResponseDto<AccountSummaryDto>.SuccessResponse(summary));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching account summary"
            ));
        }
    }

    // GET: api/Account/by-number/{accountNumber}
    [HttpGet("by-number/{accountNumber}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccountByNumber(string accountNumber)
    {
        try
        {
            var account = await _accountService.GetByAccountNumberAsync(accountNumber);

            if (account == null)
                return NotFound(ResponseDto<object>.ErrorResponse("Account not found"));

            var accountDto = _mapper.Map<AccountDto>(account);

            return Ok(ResponseDto<AccountDto>.SuccessResponse(accountDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching account"
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