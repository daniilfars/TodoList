using Application.DTOs.ResponseDTOs;

namespace Application.DTOs.Jwt;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set;  } = string.Empty;
    public ResponseUserDto? User { get; set; }
}
