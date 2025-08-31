using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuthService.Api.Dtos;
using AuthService.Application.Dtos;
using AuthService.Application.UseCases;
using AuthService.Application.Mappers;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRegisterUserUseCase _registerUserUseCase;
    private readonly ILoginUserUseCase _loginUserUseCase;
    private readonly IValidateTokenUseCase _validateTokenUseCase;
    private readonly IGetProfileUseCase _getProfileUseCase;

    public AuthController(
        IRegisterUserUseCase registerUserUseCase,
        ILoginUserUseCase loginUserUseCase,
    IValidateTokenUseCase validateTokenUseCase,
    IGetProfileUseCase getProfileUseCase)
    {
        _registerUserUseCase = registerUserUseCase;
        _loginUserUseCase = loginUserUseCase;
        _validateTokenUseCase = validateTokenUseCase;
    _getProfileUseCase = getProfileUseCase;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var command = DtoMapper.ToRegisterUserCommand(request);

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

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new AuthResponse { Success = false, Message = "Invalid token: missing user id" });
            }

            var result = await _getProfileUseCase.ExecuteAsync(userId);
            if (!result.Success)
            {
                return NotFound(new AuthResponse { Success = false, Message = result.Message });
            }

            // Map full user info to a DTO for profile
            var user = result.User!;
            var profile = new Api.Dtos.FullUserDto
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

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse { Success = false, Message = ex.Message });
        }
    }

    // Map an Application-level UserDto to an API-level UserDto
    private Api.Dtos.UserDto MapToUserDto(AuthService.Application.Dtos.UserDto user)
    {
        return new Api.Dtos.UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role
        };
    }
}
