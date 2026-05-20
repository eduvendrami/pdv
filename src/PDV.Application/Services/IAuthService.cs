using PDV.Application.DTOs;

namespace PDV.Application.Services;

public interface IAuthService
{
    Task<UserDto?> LoginAsync(LoginDto dto);
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<UserDto?> UpdateUserAsync(int id, CreateUserDto dto);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> ChangePasswordAsync(int id, string newPassword);
    string HashPassword(string password);
}
