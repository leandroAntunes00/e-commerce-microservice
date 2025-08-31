using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using AuthService.Api.Controllers;
using AuthService.Application.UseCases;
using AuthService.Application.Dtos;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Services;

namespace AuthService.UnitTests;

public class AuthControllerUnitTests
{
    [Fact]
    public async Task LoginController_ShouldReturnOk_WhenUseCaseSucceeds()
    {
        // Arrange
        var useCaseMock = new Mock<ILoginUserUseCase>();
        var authResult = new AuthResult
        {
            Success = true,
            Message = "Login successful",
            Token = "jwt-token",
            User = new AuthService.Application.Dtos.UserDto
            {
                Id = 0,
                Username = "user",
                Email = "user@example.com",
                FullName = "User Name",
                Role = "USER",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        useCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<LoginUserCommand>()))
            .ReturnsAsync(authResult);

        var controller = new AuthController(
            Mock.Of<IRegisterUserUseCase>(),
            useCaseMock.Object,
            Mock.Of<IValidateTokenUseCase>(),
            Mock.Of<IGetProfileUseCase>());

        var request = new Api.Dtos.LoginRequest { Email = "user@example.com", Password = "password" };

        // Act
        var result = await controller.Login(request);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Api.Dtos.AuthResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task LoginController_ShouldReturnUnauthorized_WhenUseCaseFails()
    {
        // Arrange
        var useCaseMock = new Mock<ILoginUserUseCase>();
        var authResult = new AuthResult
        {
            Success = false,
            Message = "Invalid credentials"
        };

        useCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<LoginUserCommand>()))
            .ReturnsAsync(authResult);

        var controller = new AuthController(
            Mock.Of<IRegisterUserUseCase>(),
            useCaseMock.Object,
            Mock.Of<IValidateTokenUseCase>(),
            Mock.Of<IGetProfileUseCase>());

        var request = new Api.Dtos.LoginRequest { Email = "user@example.com", Password = "wrong" };

        // Act
        var result = await controller.Login(request);

        // Assert
        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorized.Value.Should().BeOfType<Api.Dtos.AuthResponse>().Subject;
        response.Success.Should().BeFalse();
    }
}

public class LoginUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenCredentialsValid()
    {
        // Arrange
        var user = User.Create("user", "user@example.com", "hashed", "User", "USER");

        var userRepo = new Mock<Domain.Interfaces.IUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        userRepo.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask).Verifiable();

        var passwordService = new Mock<Domain.Interfaces.IPasswordService>();
        passwordService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var jwtService = new Mock<Domain.Interfaces.IJwtService>();
        jwtService.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("token123");

        var useCase = new LoginUserUseCase(userRepo.Object, passwordService.Object, jwtService.Object);

        var command = new LoginUserCommand { Email = "user@example.com", Password = "password" };

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be("token123");
        userRepo.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Email == "user@example.com")), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPasswordInvalid()
    {
        // Arrange
        var user = User.Create("user", "user@example.com", "hashed", "User", "USER");

        var userRepo = new Mock<Domain.Interfaces.IUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

        var passwordService = new Mock<Domain.Interfaces.IPasswordService>();
        passwordService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var jwtService = new Mock<Domain.Interfaces.IJwtService>();

        var useCase = new LoginUserUseCase(userRepo.Object, passwordService.Object, jwtService.Object);

        var command = new LoginUserCommand { Email = "user@example.com", Password = "bad" };

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password");
    }
}

public class RegisterUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var userRepo = new Mock<Domain.Interfaces.IUserRepository>();
        userRepo.Setup(x => x.ExistsByUsernameOrEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        userRepo.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => { u.Id = 1; return u; });

        var passwordService = new Mock<Domain.Interfaces.IPasswordService>();
        passwordService.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");

        var jwtService = new Mock<Domain.Interfaces.IJwtService>();
        jwtService.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("token-created");

        var useCase = new RegisterUserUseCase(userRepo.Object, passwordService.Object, jwtService.Object);

        var command = new RegisterUserCommand
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "pass",
            FullName = "New User",
            Role = "USER"
        };

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be("token-created");
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUserExists()
    {
        // Arrange
        var userRepo = new Mock<Domain.Interfaces.IUserRepository>();
        userRepo.Setup(x => x.ExistsByUsernameOrEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var passwordService = new Mock<Domain.Interfaces.IPasswordService>();
        var jwtService = new Mock<Domain.Interfaces.IJwtService>();

        var useCase = new RegisterUserUseCase(userRepo.Object, passwordService.Object, jwtService.Object);

        var command = new RegisterUserCommand
        {
            Username = "existing",
            Email = "exist@example.com",
            Password = "pass",
            FullName = "Exist",
            Role = "USER"
        };

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Username or email already exists");
    }
}

public class AuthControllerRegisterTests
{
    [Fact]
    public async Task Register_ShouldReturnOk_WhenUseCaseSucceeds()
    {
        // Arrange
        var registerUseCase = new Mock<IRegisterUserUseCase>();
        var resultUseCase = new AuthResult
        {
            Success = true,
            Message = "User registered successfully",
            Token = "tk",
            User = new AuthService.Application.Dtos.UserDto
            {
                Id = 1,
                Username = "u",
                Email = "u@example.com",
                FullName = "U",
                Role = "USER",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        registerUseCase.Setup(x => x.ExecuteAsync(It.IsAny<RegisterUserCommand>())).ReturnsAsync(resultUseCase);

    var controller = new AuthController(registerUseCase.Object, Mock.Of<ILoginUserUseCase>(), Mock.Of<IValidateTokenUseCase>(), Mock.Of<IGetProfileUseCase>());

        var request = new Api.Dtos.RegisterRequest
        {
            Username = "u",
            Email = "u@example.com",
            Password = "p",
            FullName = "U",
            Role = "USER"
        };

        // Act
        var actionResult = await controller.Register(request);

        // Assert
        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Api.Dtos.AuthResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Token.Should().Be("tk");
    }
}

public class ValidateTokenUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenNoHttpContext()
    {
        // Arrange
        var httpAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        httpAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var jwtService = new Mock<Domain.Interfaces.IJwtService>();

        var useCase = new ValidateTokenUseCase(httpAccessor.Object, jwtService.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("No HttpContext available");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenNoAuthorizationHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var httpAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        httpAccessor.Setup(x => x.HttpContext).Returns(context);

        var jwtService = new Mock<Domain.Interfaces.IJwtService>();

        var useCase = new ValidateTokenUseCase(httpAccessor.Object, jwtService.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("No Authorization header");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenTokenValid()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer valid.token";

        var httpAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        httpAccessor.Setup(x => x.HttpContext).Returns(context);

        var jwtService = new Mock<Domain.Interfaces.IJwtService>();
        jwtService.Setup(x => x.ValidateToken("valid.token")).Returns(true);

        var useCase = new ValidateTokenUseCase(httpAccessor.Object, jwtService.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Token is valid");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenTokenInvalid()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer invalid.token";

        var httpAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        httpAccessor.Setup(x => x.HttpContext).Returns(context);

        var jwtService = new Mock<Domain.Interfaces.IJwtService>();
        jwtService.Setup(x => x.ValidateToken("invalid.token")).Returns(false);

        var useCase = new ValidateTokenUseCase(httpAccessor.Object, jwtService.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Token is invalid");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenJwtServiceThrows()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer some.token";

        var httpAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        httpAccessor.Setup(x => x.HttpContext).Returns(context);

        var jwtService = new Mock<Domain.Interfaces.IJwtService>();
        jwtService.Setup(x => x.ValidateToken(It.IsAny<string>())).Throws(new Exception("boom"));

        var useCase = new ValidateTokenUseCase(httpAccessor.Object, jwtService.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Token validation error: boom");
    }
}

public class AuthControllerValidateTests
{
    [Fact]
    public async Task ValidateToken_Controller_ReturnsOk()
    {
        // Arrange
        var validateUseCase = new Mock<IValidateTokenUseCase>();
        validateUseCase.Setup(x => x.ExecuteAsync()).ReturnsAsync(new AuthResult { Success = true, Message = "Token is valid" });

    var controller = new AuthController(Mock.Of<IRegisterUserUseCase>(), Mock.Of<ILoginUserUseCase>(), validateUseCase.Object, Mock.Of<IGetProfileUseCase>());

        // Act
        var result = await controller.ValidateToken();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Api.Dtos.AuthResponse>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToken_Controller_ReturnsFailure_WhenUseCaseReportsError()
    {
        // Arrange
        var validateUseCase = new Mock<IValidateTokenUseCase>();
        validateUseCase.Setup(x => x.ExecuteAsync()).ReturnsAsync(new AuthResult { Success = false, Message = "Token validation error: boom" });

    var controller = new AuthController(Mock.Of<IRegisterUserUseCase>(), Mock.Of<ILoginUserUseCase>(), validateUseCase.Object, Mock.Of<IGetProfileUseCase>());

        // Act
        var result = await controller.ValidateToken();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Api.Dtos.AuthResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Token validation error: boom");
    }
}

public class GetProfileUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = User.Create("u", "u@example.com", "h", "U", "USER");
        user.Id = 42;

        var userRepo = new Mock<Domain.Interfaces.IUserRepository>();
        userRepo.Setup(x => x.GetByIdAsync(42)).ReturnsAsync(user);

        var useCase = new GetProfileUseCase(userRepo.Object);

        // Act
        var result = await useCase.ExecuteAsync(42);

        // Assert
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        var userRepo = new Mock<Domain.Interfaces.IUserRepository>();
        userRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Domain.Entities.User?)null);

        var useCase = new GetProfileUseCase(userRepo.Object);

        // Act
        var result = await useCase.ExecuteAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User not found");
    }
}

public class AuthControllerProfileTests
{
    [Fact]
    public async Task Profile_ReturnsProfile_WhenAuthenticated()
    {
        // Arrange
        var profileUser = User.Create("u", "u@example.com", "h", "U", "USER");
        profileUser.Id = 7;

        var getProfile = new Mock<IGetProfileUseCase>();
        getProfile.Setup(x => x.ExecuteAsync(7)).ReturnsAsync(new AuthResult { Success = true, User = new AuthService.Application.Dtos.UserDto
        {
            Id = profileUser.Id,
            Username = profileUser.Username,
            Email = profileUser.Email,
            FullName = profileUser.FullName,
            Role = profileUser.Role,
            IsActive = profileUser.IsActive,
            CreatedAt = profileUser.CreatedAt,
            LastLoginAt = profileUser.LastLoginAt
        } });

        var controller = new AuthController(Mock.Of<IRegisterUserUseCase>(), Mock.Of<ILoginUserUseCase>(), Mock.Of<IValidateTokenUseCase>(), getProfile.Object);

        // Set user principal with NameIdentifier claim
        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "7") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) } };

        // Act
        var result = await controller.Profile();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var profile = ok.Value.Should().BeOfType<Api.Dtos.FullUserDto>().Subject;
        profile.Id.Should().Be(7);
        profile.Email.Should().Be("u@example.com");
    }

    [Fact]
    public async Task Profile_ReturnsUnauthorized_WhenNoClaim()
    {
        // Arrange
        var getProfile = new Mock<IGetProfileUseCase>();
        var controller = new AuthController(Mock.Of<IRegisterUserUseCase>(), Mock.Of<ILoginUserUseCase>(), Mock.Of<IValidateTokenUseCase>(), getProfile.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await controller.Profile();

        // Assert
        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorized.Value.Should().BeOfType<Api.Dtos.AuthResponse>().Subject;
        response.Success.Should().BeFalse();
    }
}

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "testpassword";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password); // Hash should be different from plain password
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        // Arrange
        var password = "testpassword";
        var hash = _passwordService.HashPassword(password);

        // Act
        var isValid = _passwordService.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var password = "testpassword";
        var wrongPassword = "wrongpassword";
        var hash = _passwordService.HashPassword(password);

        // Act
        var isValid = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        isValid.Should().BeFalse();
    }
}

public class JwtServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("supersecretkeythatislongenoughforhmacsha256");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("testissuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("testaudience");

        _jwtService = new JwtService(_configurationMock.Object);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", "Test User", "USER");
        user.Id = 1;

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        // Token should be a JWT (three parts separated by dots)
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void ValidateToken_ShouldReturnTrue_WhenTokenIsValid()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", "Test User", "USER");
        user.Id = 1;
        var token = _jwtService.GenerateToken(user);

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_WhenTokenIsInvalid()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var isValid = _jwtService.ValidateToken(invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_WhenTokenIsExpired()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", "Test User", "USER");
        user.Id = 1;

        // Create a token that expires immediately
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configurationMock.Object["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiredToken = new JwtSecurityToken(
            issuer: _configurationMock.Object["Jwt:Issuer"],
            audience: _configurationMock.Object["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(-1), // Already expired
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        // Act
        var isValid = _jwtService.ValidateToken(tokenString);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_WhenTokenHasWrongIssuer()
    {
        // Arrange
        var user = User.Create("testuser", "test@example.com", "hash", "Test User", "USER");
        user.Id = 1;

        // Create token with wrong issuer
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configurationMock.Object["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var wrongIssuerToken = new JwtSecurityToken(
            issuer: "wrongissuer",
            audience: _configurationMock.Object["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(wrongIssuerToken);

        // Act
        var isValid = _jwtService.ValidateToken(tokenString);

        // Assert
        isValid.Should().BeFalse();
    }
}