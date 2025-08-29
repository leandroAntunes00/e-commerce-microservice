using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuthService.Api.Dtos;
using AuthService.Application.Dtos;
using AuthService.Application.UseCases;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRegisterUserUseCase _registerUserUseCase;
    private readonly ILoginUserUseCase _loginUserUseCase;
    private readonly IValidateTokenUseCase _validateTokenUseCase;

    public AuthController(
        IRegisterUserUseCase registerUserUseCase,
        ILoginUserUseCase loginUserUseCase,
        IValidateTokenUseCase validateTokenUseCase)
    {
        _registerUserUseCase = registerUserUseCase;
        _loginUserUseCase = loginUserUseCase;
        _validateTokenUseCase = validateTokenUseCase;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var command = new RegisterUserCommand
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                FullName = request.FullName,
                Role = request.Role
            };

            var result = await _registerUserUseCase.ExecuteAsync(command);

            if (!result.Success)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(new AuthResponse
            {
                Success = true,
                Message = result.Message,
                Token = result.Token,
                User = MapToUserDto(result.User!)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = $"Registration failed: {ex.Message}"
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var command = new LoginUserCommand
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await _loginUserUseCase.ExecuteAsync(command);

            if (!result.Success)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(new AuthResponse
            {
                Success = true,
                Message = result.Message,
                Token = result.Token,
                User = MapToUserDto(result.User!)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = $"Login failed: {ex.Message}"
            });
        }
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateToken()
    {
        try
        {
            var result = await _validateTokenUseCase.ExecuteAsync();

            return Ok(new AuthResponse
            {
                Success = result.Success,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        return Ok("Auth Service is running!");
    }

    private UserDto MapToUserDto(Domain.Entities.User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
