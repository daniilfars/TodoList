using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UpdateDTOs;

public class UpdateTagDto
{
    [StringLength(50)]
    public string? Name { get; set; }
}
