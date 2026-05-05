using Application.DTOs.CreateDTOs;
using Application.DTOs.UpdateDTOs;
using Application.DTOs.ResponseDTOs;
using Application.RequestFeatures;

namespace Application.Interfaces;

public interface IProjectService
{
    Task<ResponsePaged<ResponseProjectDto>> GetAllProjectsAsync(ProjectQueryParameters parameters);
    Task<ResponsePaged<ResponseProjectDto>> GetProjectsAsync(ProjectQueryParameters parameters, int userId);
    Task<ResponseProjectDto?> GetProjectByIdAsync(int id, int userId);
    Task<ResponseProjectDto> CreateProjectAsync(CreateProjectDto createProject);
    Task<ResponseProjectDto?> UpdateProjectAsync(int id, int userId, UpdateProjectDto updateProject);
    Task<bool> DeleteProjectAsync(int id, int userId);
}