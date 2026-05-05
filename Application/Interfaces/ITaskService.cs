using Application.DTOs.CreateDTOs;
using Application.DTOs.UpdateDTOs;
using Application.DTOs.ResponseDTOs;
using Application.RequestFeatures;

namespace Application.Interfaces;

public interface ITaskService
{
    Task<ResponseTaskDto?> GetTaskByIdAsync(int id, int userId);
    Task<ResponseTaskDto> CreateTaskAsync(CreateTaskDto createTask);
    Task<ResponseTaskDto?> UpdateTaskAsync(int id, int userId, UpdateTaskDto updateTask);
    Task<bool> DeleteTaskAsync(int id, int userId);
    Task<ResponsePaged<ResponseTaskDto>> GetTasksAsync(TaskQueryParameters parameters, int userId);
}