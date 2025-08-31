using AuthService.Application.Dtos;
using AuthService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AuthService.Application.UseCases;

public interface IValidateTokenUseCase
{
    Task<AuthResult> ExecuteAsync();
}

public class ValidateTokenUseCase : IValidateTokenUseCase
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtService _jwtService;

    public ValidateTokenUseCase(IHttpContextAccessor httpContextAccessor, IJwtService jwtService)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> ExecuteAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return new AuthResult { Success = false, Message = "No HttpContext available" };
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeaders))
        {
            return new AuthResult { Success = false, Message = "No Authorization header" };
        }

        var bearer = authHeaders.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(bearer))
        {
            return new AuthResult { Success = false, Message = "Empty Authorization header" };
        }

        const string prefix = "Bearer ";
        var token = bearer.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? bearer.Substring(prefix.Length).Trim()
            : bearer.Trim();

        if (string.IsNullOrEmpty(token))
        {
            return new AuthResult { Success = false, Message = "No token provided" };
        }

        var valid = false;
        try
        {
            valid = _jwtService.ValidateToken(token);
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, Message = $"Token validation error: {ex.Message}" };
        }

        return await Task.FromResult(new AuthResult
        {
            Success = valid,
            Message = valid ? "Token is valid" : "Token is invalid"
        });
    }
}
