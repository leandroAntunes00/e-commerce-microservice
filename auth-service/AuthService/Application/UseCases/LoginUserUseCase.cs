using AuthService.Application.Dtos;
using AuthService.Domain.Interfaces;
using AuthService.Application.Mappers;

namespace AuthService.Application.UseCases;

public interface ILoginUserUseCase
{
    Task<AuthResult> ExecuteAsync(LoginUserCommand command);
}

public class LoginUserUseCase : ILoginUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public LoginUserUseCase(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> ExecuteAsync(LoginUserCommand command)
    {
        // Find user by email
        var user = await _userRepository.GetByEmailAsync(command.Email);
        if (user == null)
        {
            return new AuthResult
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        // Verify password
        var isPasswordValid = _passwordService.VerifyPassword(command.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return new AuthResult
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return new AuthResult
            {
                Success = false,
                Message = "Account is deactivated"
            };
        }

        // Update last login
    user.UpdateLastLogin();
    await _userRepository.UpdateAsync(user);

    // Generate JWT token
    var token = _jwtService.GenerateToken(user);

        return new AuthResult
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            User = DtoMapper.ToApplicationUserDto(user)
        };
    }
}
