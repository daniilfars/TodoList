using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Application.DTOs.CreateDTOs;
using Application.DTOs.ResponseDTOs;
using Application.DTOs.UpdateDTOs;
using Application.Interfaces;

namespace Infrastructure.Services;

public class TagService : ITagService
{
    private readonly IAppDbContext db;

    public TagService(IAppDbContext context)
    {
        db = context;
    }
    public async Task<ResponseTagDto> CreateTagAsync(CreateTagDto createTag)
    {
        if (string.IsNullOrWhiteSpace(createTag.Name))
            throw new ArgumentException("Название тега не может быть пустым.");

        var tagName = createTag.Name.Trim();

        var existingTag = await db.Tags.AnyAsync(t => t.Name == tagName);
        if (existingTag)
            throw new InvalidOperationException($"Тег '{tagName}' уже существует.");

        Tag tag = new Tag { Name = tagName };

        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        return new ResponseTagDto 
        {   
            Id = tag.Id,
            Name = tag.Name  
        };
    }

    public async Task<bool> DeleteTagAsync(int id)
    {
        Tag? tag = await db.Tags.Include(t => t.Tasks).FirstOrDefaultAsync(t => t.Id == id);

        if (tag == null)
            return false;

        if (tag.Tasks.Any())
            throw new InvalidOperationException("Нельзя удалить тег, который используется в задачах.");

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<ResponseTagDto>> GetAllTagsAsync()
    {
        return await db.Tags.AsNoTracking().OrderByDescending(t => t.Name).Select(tag => new ResponseTagDto
        {
            Id = tag.Id,
            Name = tag.Name
        }).ToListAsync();
    }

    public async Task<ResponseTagDto?> GetTagByIdAsync(int id)
    {
        Tag? tag = await db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        if (tag == null)
            return null;

        return new ResponseTagDto
        {
            Id = tag.Id,
            Name = tag.Name
        };
    }

    public async Task<List<ResponseTaskDto>> GetTasksByTagAsync(int tagId)
    {
        return await db.Tags
        .Where(t => t.Id == tagId)
        .SelectMany(t => t.Tasks)
        .Select(task => new ResponseTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            IsCompleted = task.IsCompleted,
            ProjectId = task.ProjectId,
            Tags = task.Tags.Select(tag => new ResponseTagDto
            {
                Id = tag.Id,
                Name = tag.Name
            }).ToList()
        })
        .ToListAsync();
    }

    public async Task<ResponseTagDto?> UpdateTagAsync(int id, UpdateTagDto updateTag)
    {
        Tag? tag = await db.Tags.FindAsync(id);

        if (tag == null)
            return null;

        if (!string.IsNullOrWhiteSpace(updateTag.Name))
        {
            var newName = updateTag.Name.Trim();

            var existingTag = await db.Tags
                .AnyAsync(t => t.Name == newName && t.Id != id);

            if (existingTag)
                throw new InvalidOperationException($"Тег с именем '{newName}' уже существует.");

            tag.Name = newName;
        }

        await db.SaveChangesAsync();

        return new ResponseTagDto
        {
            Id = tag.Id,
            Name = tag.Name
        };
    }
}