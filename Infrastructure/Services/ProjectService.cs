using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Domain.Models;
using Application.DTOs.CreateDTOs;
using Application.DTOs.ResponseDTOs;
using Application.DTOs.UpdateDTOs;
using Application.Interfaces;
using Application.RequestFeatures;

namespace Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly IAppDbContext db;

    public ProjectService(IAppDbContext context)
    {
        db = context;
    }

    public async Task<ResponseProjectDto> CreateProjectAsync(CreateProjectDto createProject)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == createProject.UserId);
        if (!userExists)
            throw new InvalidOperationException("Пользователь с указанным id не существует.");

        Project project = new Project
        {
            Name = createProject.Name,
            Description = createProject.Description,
            CreatedAt = DateTime.UtcNow,
            UserId = createProject.UserId
        };

        await db.Projects.AddAsync(project);
        await db.SaveChangesAsync();

        return new ResponseProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UserId = project.UserId
        };
    }

    public async Task<bool> DeleteProjectAsync(int id, int userId)
    {
        Project? project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
            return false;

        if (project.UserId != userId)
            throw new UnauthorizedAccessException("У вас нет прав на удаление этого проекта.");

        db.Projects.Remove(project);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<ResponsePaged<ResponseProjectDto>> GetAllProjectsAsync(ProjectQueryParameters parameters)
    {
        IQueryable<Project> query = db.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

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
            .Select(p => new ResponseProjectDto
            {
                CreatedAt = p.CreatedAt,
                Description = p.Description,
                Id = p.Id,
                Name = p.Name,
                UserId = p.UserId
            })
            .ToListAsync();

        return new ResponsePaged<ResponseProjectDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    public async Task<ResponsePaged<ResponseProjectDto>> GetProjectsAsync(ProjectQueryParameters parameters, int userId)
    {
        IQueryable<Project> query = db.Projects.Where(p => p.UserId == userId).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

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
            .Select(p => new ResponseProjectDto
            {
                CreatedAt = p.CreatedAt,
                Description = p.Description,
                Id = p.Id,
                Name = p.Name,
                UserId = p.UserId
            })
            .ToListAsync();

        return new ResponsePaged<ResponseProjectDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    public async Task<ResponseProjectDto?> GetProjectByIdAsync(int id, int userId)
    {
        Project? project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
            return null;

        if (project.UserId != userId)
            throw new UnauthorizedAccessException("У вас нет прав на просмотр этого проекта.");

        return new ResponseProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UserId = project.UserId
        };
    }

    public async Task<ResponseProjectDto?> UpdateProjectAsync(int id, int userId, UpdateProjectDto updateProject)
    {
        Project? project = await db.Projects.FindAsync(id);

        if (project == null)
            return null;

        if (project.UserId != userId)
            throw new UnauthorizedAccessException("У вас нет прав на изменение этого проекта.");

        if (!string.IsNullOrWhiteSpace(updateProject.Name))
            project.Name = updateProject.Name;

        if (!string.IsNullOrWhiteSpace(updateProject.Description))
            project.Description = updateProject.Description;

        await db.SaveChangesAsync();

        return new ResponseProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UserId = project.UserId
        };
    }
}