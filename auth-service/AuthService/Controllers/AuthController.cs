using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Models;
using AuthService.Data;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AuthDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Verificar se usuário já existe
            if (_context.Users.Any(u => u.Username == request.Username || u.Email == request.Email))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Username or email already exists"
                });
            }

            // Validar role
            if (request.Role != UserRoles.User && request.Role != UserRoles.Admin)
            {
                request.Role = UserRoles.User; // Default to USER if invalid
            }

            // Criar novo usuário
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Gerar token JWT
            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "User registered successfully",
                Token = token,
                User = user
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
            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Account is deactivated"
                });
            }

            // Atualizar último login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Gerar token JWT
            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = user
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
    public IActionResult ValidateToken()
    {
        try
        {
            // Este endpoint pode ser usado para validar tokens
            // Em um cenário real, você poderia implementar lógica adicional aqui
            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Token is valid"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = $"Token validation failed: {ex.Message}"
            });
        }
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok("Auth Service is running!");
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("users")]
    public IActionResult GetAllUsers()
    {
        try
        {
            var users = _context.Users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.FullName,
                u.Role,
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt
            }).ToList();

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to retrieve users: {ex.Message}" });
        }
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (request.Role != UserRoles.User && request.Role != UserRoles.Admin)
            {
                return BadRequest(new { message = "Invalid role. Must be USER or ADMIN" });
            }

            user.Role = request.Role;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User role updated successfully",
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.Role,
                    user.IsActive
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to update user role: {ex.Message}" });
        }
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User status updated successfully",
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.Role,
                    user.IsActive
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to update user status: {ex.Message}" });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiryInMinutes = int.Parse(jwtSettings["ExpiryInMinutes"]!);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fullName", user.FullName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class UpdateRoleRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;
}

public class UpdateStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}
