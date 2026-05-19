using Application.DTOs.ResponseDTOs;
using Application.DTOs.UpdateDTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService) => _userService = userService;

    // GET: /api/user/all
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<ResponseUserDto>>> GetAll()
    {
        var result = await _userService.GetAllUsersAsync();
        return Ok(result);
    }

    // GET: /api/user/me
    [HttpGet("me")]
    public async Task<ActionResult<ResponseUserDto?>> GetUserById()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized(new { message = "ID пользователя не найден в токене" });

        var result = await _userService.GetUserByIdAsync(userId.Value);
        if (result == null)
            return NotFound(new { message = "Пользователь не найден" });

        return Ok(result);
    }

    // DELETE: /api/user/me
    [HttpDelete("me")]
    public async Task<IActionResult> Delete()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized(new { message = "ID пользователя не найден в токене" });

        var result = await _userService.DeleteUserAsync(userId.Value);
        if (!result)
            return NotFound(new { message = "Пользователь не найден" });

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Аккаунт успешно удалён" });
    }

    // PUT: /api/user/me
    [HttpPut("me")]
    public async Task<ActionResult<ResponseUserDto>> Update(UpdateUserDto updateUser)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized(new { message = "ID пользователя не найден в токене" });

        try
        {
            var result = await _userService.UpdateUserAsync(userId.Value, updateUser);
            if (result == null)
                return NotFound(new { message = "Пользователь не найден" });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private int? GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;
        return userId;
    }
}