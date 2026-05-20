using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Application.DTOs.CreateDTOs;
using Application.DTOs.ResponseDTOs;
using Application.DTOs.UpdateDTOs;
using Application.Interfaces;
using BCrypt.Net;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Infrastructure.Services;

public class UserService : IUserService
{
    readonly IAppDbContext db;
    readonly IDistributedCache cache;

    public UserService(IAppDbContext context, IDistributedCache _cache)
    {
        db = context;
        cache = _cache;
    }
    public async Task<User> CreateUserAsync(CreateUserDto createUserDto)
    {
        User? existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == createUserDto.Email);

        if (existingUser != null)
            throw new InvalidOperationException("Пользователь с таким email уже существует.");

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

        User user = new User
        {
            Name = createUserDto.UserName,
            Email = createUserDto.Email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        if (user.Email == "admin@example.com")
            user.Role = "Admin";

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return false;

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<ResponseUserDto>> GetAllUsersAsync()
    {
        return await db.Users.AsNoTracking().Select(u => new ResponseUserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            CreatedAt = u.CreatedAt
        }).ToListAsync();
    }

    public async Task<ResponseUserDto?> GetUserByIdAsync(int id)
    {
        User? user = null;

        var userString = await cache.GetStringAsync(id.ToString());
        if(userString != null)
            user = JsonSerializer.Deserialize<User>(userString);


        if (user == null) {
            user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

            if(user != null)
            {
                Console.WriteLine($"{user.Name} извлечен из БД");
                userString = JsonSerializer.Serialize(user);
                await cache.SetStringAsync(user.Id.ToString(), userString, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                });
            }
            else
                return null;
        }
        else
        {
            Console.WriteLine($"{user.Name} извлечен из кэша");
        }

        return new ResponseUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<ResponseUserDto?> UpdateUserAsync(int id, UpdateUserDto updateUser)
    {
        User? user = await db.Users.FindAsync(id);

        if (user == null)
            return null;

        if (!string.IsNullOrWhiteSpace(updateUser.UserName))
            user.Name = updateUser.UserName;

        if (!string.IsNullOrWhiteSpace(updateUser.Email))
            user.Email = updateUser.Email;

        if (!string.IsNullOrWhiteSpace(updateUser.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUser.Password);

        await db.SaveChangesAsync();

        return new ResponseUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}