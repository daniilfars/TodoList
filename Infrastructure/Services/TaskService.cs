using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Domain.Models;
using Application.DTOs.CreateDTOs;
using Application.DTOs.ResponseDTOs;
using Application.DTOs.UpdateDTOs;
using Application.RequestFeatures;
using Application.Interfaces;
using System.Threading.Channels;

namespace Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly ChannelWriter<string> channelWriter;
    private readonly IAppDbContext db;

    public TaskService(ChannelWriter<string> _channelWriter, IAppDbContext context)
    {
        channelWriter = _channelWriter;
        db = context;
    }

    public async Task<ResponseTaskDto> CreateTaskAsync(CreateTaskDto createTask)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == createTask.ProjectId);
        if (project == null)
            throw new InvalidOperationException("Проект с указанным id не существует.");

        if (project.UserId != createTask.UserId)
            throw new UnauthorizedAccessException("У вас нет прав на добавление задачи в этот проект.");

        TaskItem task = new TaskItem
        {
            Title = createTask.Title,
            Description = createTask.Description,
            CreatedAt = DateTime.UtcNow,
            ProjectId = createTask.ProjectId,
            IsCompleted = false
        };

        if (createTask.TagIds != null && createTask.TagIds.Any())
        {
            List<Tag> tags = await db.Tags.Where(t => createTask.TagIds.Contains(t.Id)).ToListAsync();

            if (tags.Count != createTask.TagIds.Count)
                throw new InvalidOperationException("Один или несколько тегов не найдены.");

            task.Tags = tags;
        }

        await db.Tasks.AddAsync(task);
        await db.SaveChangesAsync();
        await channelWriter.WriteAsync($"Началось добавление задачи #{task.Id}: {task.Title}");

        List<ResponseTagDto> tagDtos = new List<ResponseTagDto>();

        if (task.Tags != null && task.Tags.Any())
        {
            tagDtos = task.Tags.Select(t => new ResponseTagDto
            {
                Id = t.Id,
                Name = t.Name
            }).ToList();
        }

        return new ResponseTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            ProjectId = task.ProjectId,
            Tags = tagDtos
        };
    }

    public async Task<bool> DeleteTaskAsync(int id, int userId)
    {
        TaskItem? task = await db.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return false;

        if (task.Project.UserId != userId)
            throw new UnauthorizedAccessException("У вас нет прав на удаление этой задачи.");

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        await channelWriter.WriteAsync($"Началось удаление задачи #{task.Id}: {task.Title}");

        return true;
    }

    public async Task<ResponseTaskDto?> GetTaskByIdAsync(int id, int userId)
    {
        TaskItem? task = await db.Tasks.AsNoTracking().Include(t => t.Tags).Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return null;

        if (task.Project.UserId != userId)
            throw new UnauthorizedAccessException("У вас нет прав на просмотр этой задачи.");

        return new ResponseTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            ProjectId = task.ProjectId,
            Tags = task.Tags.Select(t => new ResponseTagDto
            {
                Id = t.Id,
                Name = t.Name
            }).ToList()
        };
    }

    public async Task<ResponseTaskDto?> UpdateTaskAsync(int id, int userId, UpdateTaskDto updateTask)
    {
        TaskItem? task = await db.Tasks
            .Include(t => t.Tags)
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return null;

        if (task.Project.UserId != userId)
            throw new UnauthorizedAccessException("У вас нет прав на изменение этой задачи.");

        if (!string.IsNullOrWhiteSpace(updateTask.Title))
            task.Title = updateTask.Title;

        if (!string.IsNullOrWhiteSpace(updateTask.Description))
            task.Description = updateTask.Description;

        if (updateTask.IsCompleted.HasValue)
            task.IsCompleted = updateTask.IsCompleted.Value;

        if (updateTask.ProjectId.HasValue)
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == updateTask.ProjectId.Value);
            if (project == null)
                throw new InvalidOperationException("Проект с указанным ID не существует.");

            if (project.UserId != userId)
                throw new UnauthorizedAccessException("У вас нет прав на перенос задачи в этот проект.");

            task.ProjectId = updateTask.ProjectId.Value;
        }

        if (updateTask.TagIds != null)
        {
            if (!updateTask.TagIds.Any())
            {
                task.Tags.Clear();
            }
            else
            {
                var newTags = await db.Tags
                    .Where(t => updateTask.TagIds.Contains(t.Id))
                    .ToListAsync();

                if (newTags.Count != updateTask.TagIds.Count)
                    throw new InvalidOperationException("Один или несколько тегов не найдены.");

                task.Tags.Clear();
                foreach (var tag in newTags)
                {
                    task.Tags.Add(tag);
                }
            }
        }

        await db.SaveChangesAsync();
        await channelWriter.WriteAsync($"Началось обновление задачи #{task.Id}: {task.Title}");

        return new ResponseTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            IsCompleted = task.IsCompleted,
            ProjectId = task.ProjectId,
            Tags = task.Tags.Select(t => new ResponseTagDto
            {
                Id = t.Id,
                Name = t.Name
            }).ToList()
        };
    }

    public async Task<ResponsePaged<ResponseTaskDto>> GetTasksAsync(TaskQueryParameters parameters, int userId)
    {
        IQueryable<TaskItem> query = db.Tasks.Include(t => t.Tags).Include(t => t.Project).Where(t => t.Project.UserId == userId).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term) || (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        if (parameters.IsCompleted.HasValue)
            query = query.Where(t => t.IsCompleted == parameters.IsCompleted.Value);

        if (parameters.ProjectId.HasValue)
            query = query.Where(t => t.ProjectId == parameters.ProjectId.Value);

        if (parameters.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= parameters.FromDate.Value);

        if (parameters.ToDate.HasValue)
        {
            var toDateEnd = parameters.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.CreatedAt <= toDateEnd);
        }

        var totalCount = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(parameters.SortBy))
        {
            string orderBy = $"{parameters.SortBy} {parameters.SortOrder}";
            query = query.OrderBy(orderBy);
        }
        else
            query = query.OrderByDescending(t => t.CreatedAt);

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(t => new ResponseTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                IsCompleted = t.IsCompleted,
                ProjectId = t.ProjectId,
                Tags = t.Tags.Select(tag => new ResponseTagDto
                {
                    Id = tag.Id,
                    Name = tag.Name
                }).ToList()
            })
            .ToListAsync();

        return new ResponsePaged<ResponseTaskDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }
}