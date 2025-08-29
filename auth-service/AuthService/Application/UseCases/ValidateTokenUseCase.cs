using AuthService.Application.Dtos;

namespace AuthService.Application.UseCases;

public interface IValidateTokenUseCase
{
    Task<AuthResult> ExecuteAsync();
}

public class ValidateTokenUseCase : IValidateTokenUseCase
{
    public async Task<AuthResult> ExecuteAsync()
    {
        // In a real implementation, you might want to validate the token
        // against a database or perform additional checks
        // For now, just return success since the token was already validated by middleware

        return await Task.FromResult(new AuthResult
        {
            Success = true,
            Message = "Token is valid"
        });
    }
}
