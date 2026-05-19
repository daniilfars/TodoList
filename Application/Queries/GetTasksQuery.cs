using MediatR;
using Application.DTOs.ResponseDTOs;

namespace Application.Queries;

public record GetTasksQuery : IRequest<ResponsePaged<ResponseTaskDto>>
{
    public int UserId { get; init; }
    public string? SearchTerm { get; init; }
    public bool? IsCompleted { get; init; }
    public int? ProjectId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? SortBy { get; init; }
    public string SortOrder { get; init; } = "desc";
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}