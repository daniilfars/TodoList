using Application.Commands;
using Application.DTOs.ResponseDTOs;
using Application.Interfaces;
using Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, ResponseTaskDto>
{
    private readonly IAppDbContext db;

    public CreateTaskHandler(IAppDbContext _db)
    {
        db = _db;
    }

    public async Task<ResponseTaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var createTask = request.createTask;

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == createTask.ProjectId, cancellationToken);

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
}