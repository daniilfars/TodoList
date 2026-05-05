using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UpdateDTOs;

public class UpdateUserDto
{
    [StringLength(50)]
    public string? UserName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [MinLength(6)]
    public string? Password { get; set; }
}
