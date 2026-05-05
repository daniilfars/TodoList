using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UpdateDTOs;

public class UpdateProjectDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
