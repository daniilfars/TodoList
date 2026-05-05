using Moq;
using MockQueryable.Moq;
using Xunit;
using FluentAssertions;
using Infrastructure.Services;
using Application.Interfaces;
using Application.DTOs.CreateDTOs;
using Domain.Models;

namespace web_api_tests.Services;

public class TagServiceTests
{
    [Fact]
    public async Task CreateTag_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        var mockContext = new Mock<IAppDbContext>();
        var service = new TagService(mockContext.Object);

        var dto = new CreateTagDto { Name = "" };

        Func<Task> act = async () => await service.CreateTagAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Название тега не может быть пустым.");
    }
    /*[Fact]
    public async Task GetTagById_WhenTagExists_ShouldReturnCorrectDto()
    {
    }*/
}
