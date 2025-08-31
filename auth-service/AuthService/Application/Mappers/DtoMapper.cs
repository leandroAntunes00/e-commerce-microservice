using AuthService.Domain.Entities;
using AuthService.Application.Dtos;
using ApiDto = AuthService.Api.Dtos;

namespace AuthService.Application.Mappers;

public static class DtoMapper
{
    public static AuthService.Application.Dtos.UserDto ToApplicationUserDto(User user)
    {
        return new AuthService.Application.Dtos.UserDto
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

    public static ApiDto.UserDto ToApiUserDto(AuthService.Application.Dtos.UserDto user)
    {
        return new AuthService.Api.Dtos.UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role
        };
    }

    public static RegisterUserCommand ToRegisterUserCommand(ApiDto.RegisterRequest req)
    {
        return new RegisterUserCommand
        {
            Username = req.Username,
            Email = req.Email,
            Password = req.Password,
            FullName = req.FullName,
            Role = string.IsNullOrWhiteSpace(req.Role) ? "USER" : req.Role
        };
    }
}
