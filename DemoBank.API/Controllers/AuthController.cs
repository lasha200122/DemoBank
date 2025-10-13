using AutoMapper;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using DemoBank.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;

    public AuthController(
        IUserService userService,
        IJwtService jwtService,
        IMapper mapper)
    {
        _userService = userService;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
    {
        try
        {
            if (!ModelState.IsValid || registrationDto.PotentialInvestmentRange == null)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid registration data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            // Create user as Client by default
            var user = await _userService.CreateUserAsync(registrationDto, UserRole.Client);

            // Generate token
            var token = _jwtService.GenerateToken(user);

            // Map user to DTO
            var userDto = _mapper.Map<UserDto>(user);

            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = userDto
            };

            return Ok(ResponseDto<LoginResponseDto>.SuccessResponse(
                response,
                "Registration successful"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred during registration"
            ));
        }
    }

    [HttpPost("register-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] UserRegistrationDto registrationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid registration data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            // Create user as Admin
            var user = await _userService.CreateUserAsync(registrationDto, UserRole.Admin);

            // Map user to DTO
            var userDto = _mapper.Map<UserDto>(user);

            return Ok(ResponseDto<UserDto>.SuccessResponse(
                userDto,
                "Admin user created successfully"
            ));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred during admin registration"
            ));
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid login data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            // Find user by username
            var user = await _userService.GetByUsernameAsync(loginDto.Username);

            if (user == null || !await _userService.ValidatePasswordAsync(user, loginDto.Password))
            {
                return Unauthorized(ResponseDto<object>.ErrorResponse(
                    "Invalid username or password"
                ));
            }

            if (user.Status == Status.Rejected)
            {
                return Unauthorized(ResponseDto<object>.ErrorResponse(
                    "Account is deactivated. Please contact support."
                ));
            }

            user.LastLogin = DateTime.Now;
            await _userService.UpdateUserAsync(user);

            // Generate token
            var token = _jwtService.GenerateToken(user);

            // Map user to DTO
            var userDto = _mapper.Map<UserDto>(user);

            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = userDto
            };

            return Ok(ResponseDto<LoginResponseDto>.SuccessResponse(
                response,
                "Login successful"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred during login"
            ));
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Invalid data",
                    ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                ));
            }

            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var result = await _userService.ChangePasswordAsync(
                userId,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword
            );

            if (!result)
            {
                return BadRequest(ResponseDto<object>.ErrorResponse(
                    "Failed to change password. Please check your current password."
                ));
            }

            return Ok(ResponseDto<object>.SuccessResponse(
                null,
                "Password changed successfully"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while changing password"
            ));
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var user = await _userService.GetByIdAsync(userId);

            if (user == null)
            {
                return NotFound(ResponseDto<object>.ErrorResponse("User not found"));
            }

            var userDto = _mapper.Map<UserDto>(user);

            return Ok(ResponseDto<UserDto>.SuccessResponse(userDto));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseDto<object>.ErrorResponse(
                "An error occurred while fetching user data"
            ));
        }
    }
}