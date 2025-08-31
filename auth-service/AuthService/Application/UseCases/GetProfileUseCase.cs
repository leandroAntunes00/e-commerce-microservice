using AuthService.Application.Dtos;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Application.Mappers;

namespace AuthService.Application.UseCases;

public interface IGetProfileUseCase
{
    Task<AuthResult> ExecuteAsync(int userId);
}

public class GetProfileUseCase : IGetProfileUseCase
{
    private readonly IUserRepository _userRepository;

    public GetProfileUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AuthResult> ExecuteAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new AuthResult { Success = false, Message = "User not found" };
        }

    return new AuthResult { Success = true, Message = "Profile fetched", User = DtoMapper.ToApplicationUserDto(user) };
    }
}
