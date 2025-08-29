using AuthService.Application.Dtos;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;

namespace AuthService.Application.UseCases;

public interface IRegisterUserUseCase
{
    Task<AuthResult> ExecuteAsync(RegisterUserCommand command);
}

public class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public RegisterUserUseCase(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> ExecuteAsync(RegisterUserCommand command)
    {
        // Validate if user already exists
        var existingUser = await _userRepository.ExistsByUsernameOrEmailAsync(command.Username, command.Email);
        if (existingUser)
        {
            return new AuthResult
            {
                Success = false,
                Message = "Username or email already exists"
            };
        }

        // Validate role
        if (!UserRole.IsValid(command.Role))
        {
            command.Role = UserRole.GetDefault();
        }

        // Hash password
        var passwordHash = _passwordService.HashPassword(command.Password);

        // Create user
        var user = User.Create(
            command.Username,
            command.Email,
            passwordHash,
            command.FullName,
            command.Role
        );

        var createdUser = await _userRepository.CreateAsync(user);

        // Generate JWT token
        var token = _jwtService.GenerateToken(createdUser);

        return new AuthResult
        {
            Success = true,
            Message = "User registered successfully",
            Token = token,
            User = createdUser
        };
    }
}
