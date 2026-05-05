using Application.DTOs.CreateDTOs;
using Application.DTOs.UpdateDTOs;
using Application.DTOs.ResponseDTOs;

namespace Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<ResponseTagDto>> GetAllTagsAsync();
    Task<ResponseTagDto?> GetTagByIdAsync(int id);
    Task<ResponseTagDto> CreateTagAsync(CreateTagDto createTag);
    Task<ResponseTagDto?> UpdateTagAsync(int id, UpdateTagDto updateTag);
    Task<bool> DeleteTagAsync(int id);
    Task<List<ResponseTaskDto>> GetTasksByTagAsync(int tagId);
}
