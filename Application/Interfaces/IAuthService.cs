using Application.DTOs.Jwt;
using Domain.Models;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponse?> LoginAsync(LoginDto loginDto);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
}