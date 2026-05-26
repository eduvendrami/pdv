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
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IMapper _mapper;

    public AuthService(IUnitOfWorkFactory uowFactory, IMapper mapper)
    {
        _uowFactory = uowFactory;
        _mapper = mapper;
    }

    public string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + Salt));
        return Convert.ToHexString(bytes).ToLower();
    }

    public async Task<UserDto?> LoginAsync(LoginDto dto)
    {
        using var uow = _uowFactory.Create();
        var user = await uow.Users.GetByUsernameAsync(dto.Username);
        if (user == null || !user.IsActive) return null;
        if (user.PasswordHash != HashPassword(dto.Password)) return null;
        user.LastLogin = DateTime.Now;
        await uow.Users.UpdateAsync(user);
        await uow.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        using var uow = _uowFactory.Create();
        var user = _mapper.Map<User>(dto);
        user.PasswordHash = HashPassword(dto.Password);
        await uow.Users.AddAsync(user);
        await uow.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        using var uow = _uowFactory.Create();
        var users = await uow.Users.FindAsync(u => u.IsActive);
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<UserDto?> UpdateUserAsync(int id, CreateUserDto dto)
    {
        using var uow = _uowFactory.Create();
        var user = await uow.Users.GetByIdAsync(id);
        if (user == null) return null;
        user.FullName = dto.FullName;
        user.Username = dto.Username;
        user.Role = dto.Role;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = HashPassword(dto.Password);
        await uow.Users.UpdateAsync(user);
        await uow.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        using var uow = _uowFactory.Create();
        var user = await uow.Users.GetByIdAsync(id);
        if (user == null) return false;
        user.IsActive = false;
        await uow.Users.UpdateAsync(user);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int id, string newPassword)
    {
        using var uow = _uowFactory.Create();
        var user = await uow.Users.GetByIdAsync(id);
        if (user == null) return false;
        user.PasswordHash = HashPassword(newPassword);
        await uow.Users.UpdateAsync(user);
        await uow.SaveChangesAsync();
        return true;
    }
}
