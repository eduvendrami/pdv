using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class AuthService : IAuthService
{
    private const string Salt = "PDV_SALT_2024";
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public AuthService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + Salt));
        return Convert.ToHexString(bytes).ToLower();
    }

    public async Task<UserDto?> LoginAsync(LoginDto dto)
    {
        var user = await _uow.Users.GetByUsernameAsync(dto.Username);
        if (user == null || !user.IsActive) return null;
        if (user.PasswordHash != HashPassword(dto.Password)) return null;
        user.LastLogin = DateTime.Now;
        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        var user = _mapper.Map<User>(dto);
        user.PasswordHash = HashPassword(dto.Password);
        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = await _uow.Users.FindAsync(u => u.IsActive);
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<UserDto?> UpdateUserAsync(int id, CreateUserDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null) return null;
        user.FullName = dto.FullName;
        user.Username = dto.Username;
        user.Role = dto.Role;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = HashPassword(dto.Password);
        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null) return false;
        user.IsActive = false;
        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int id, string newPassword)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null) return false;
        user.PasswordHash = HashPassword(newPassword);
        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return true;
    }
}
