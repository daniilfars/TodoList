using Application.DTOs.CreateDTOs;
using Application.DTOs.UpdateDTOs;
using Application.DTOs.ResponseDTOs;
using Domain.Models;

namespace Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<ResponseUserDto>> GetAllUsersAsync();
    Task<ResponseUserDto?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(CreateUserDto createUser);
    Task<ResponseUserDto?> UpdateUserAsync(int id, UpdateUserDto updateUser);
    Task<bool> DeleteUserAsync(int id);
}
