using Application.DTOs.CreateDTOs;
using Application.DTOs.ResponseDTOs;
using MediatR;

namespace Application.Commands;

public record CreateTaskCommand(CreateTaskDto createTask) : IRequest<ResponseTaskDto>;