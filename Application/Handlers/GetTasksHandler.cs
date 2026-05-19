using Application.DTOs.ResponseDTOs;
using Application.Interfaces;
using Application.Queries;
using Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace Application.Handlers;

public class GetTasksHandler : IRequestHandler<GetTasksQuery, ResponsePaged<ResponseTaskDto>>
{
    private readonly IAppDbContext db;

    public GetTasksHandler(IAppDbContext _db)
    {
        db = _db;
    }

    public async Task<ResponsePaged<ResponseTaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        IQueryable<TaskItem> query = db.Tasks.Include(t => t.Tags).Include(t => t.Project).Where(t => t.Project.UserId == request.UserId).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term) || (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        if (request.IsCompleted.HasValue)
            query = query.Where(t => t.IsCompleted == request.IsCompleted.Value);

        if (request.ProjectId.HasValue)
            query = query.Where(t => t.ProjectId == request.ProjectId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
        {
            var toDateEnd = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.CreatedAt <= toDateEnd);
        }

        var totalCount = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            string orderBy = $"{request.SortBy} {request.SortOrder}";
            query = query.OrderBy(orderBy);
        }
        else
            query = query.OrderByDescending(t => t.CreatedAt);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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

        return new ResponsePaged<ResponseTaskDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}