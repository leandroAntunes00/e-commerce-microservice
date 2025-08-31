using System;
using Xunit;
using FluentAssertions;
using AuthService.Application.Mappers;
using AuthService.Domain.Entities;

namespace AuthService.UnitTests;

public class DtoMapperTests
{
    [Fact]
    public void ToApplicationUserDto_MapsDomainUserToApplicationDto()
    {
        // Arrange
        var user = User.Create("joe", "joe@example.com", "hash", "Joe Doe", "ADMIN");
        user.Id = 5;
        user.UpdateLastLogin();

        // Act
        var dto = DtoMapper.ToApplicationUserDto(user);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(5);
        dto.Username.Should().Be("joe");
        dto.Email.Should().Be("joe@example.com");
        dto.FullName.Should().Be("Joe Doe");
        dto.Role.Should().Be("ADMIN");
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(1));
        dto.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void ToApiUserDto_MapsApplicationDtoToApiDto()
    {
        // Arrange
        var appDto = new AuthService.Application.Dtos.UserDto
        {
            Id = 10,
            Username = "alice",
            Email = "alice@example.com",
            FullName = "Alice",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var apiDto = DtoMapper.ToApiUserDto(appDto);

        // Assert
        apiDto.Should().NotBeNull();
        apiDto.Id.Should().Be(10);
        apiDto.Username.Should().Be("alice");
        apiDto.Role.Should().Be("USER");
    }

    [Fact]
    public void ToRegisterUserCommand_SetsDefaultRole_WhenRoleEmpty()
    {
        // Arrange
        var req = new AuthService.Api.Dtos.RegisterRequest
        {
            Username = "bob",
            Email = "bob@example.com",
            Password = "secret",
            FullName = "Bob",
            Role = string.Empty
        };

        // Act
        var cmd = DtoMapper.ToRegisterUserCommand(req);

        // Assert
        cmd.Should().NotBeNull();
        cmd.Username.Should().Be("bob");
        cmd.Email.Should().Be("bob@example.com");
        cmd.Password.Should().Be("secret");
        cmd.FullName.Should().Be("Bob");
        cmd.Role.Should().Be("USER");
    }

    [Fact]
    public void ToRegisterUserCommand_PreservesRole_WhenProvided()
    {
        // Arrange
        var req = new AuthService.Api.Dtos.RegisterRequest
        {
            Username = "carol",
            Email = "carol@example.com",
            Password = "pwd",
            FullName = "Carol",
            Role = "ADMIN"
        };

        // Act
        var cmd = DtoMapper.ToRegisterUserCommand(req);

        // Assert
        cmd.Role.Should().Be("ADMIN");
    }
}
