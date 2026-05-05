using Application.DTOs.CreateDTOs;
using Application.Interfaces;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Services;
using MockQueryable.Moq;
using Moq;

namespace web_api_tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task CreateUserAsync_EmailAlreadyExists_ThrowsInvalidOperationException()
    {
        //arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "TestUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!")
        };

        var createDto = new CreateUserDto
        {
            Email = "test@example.com",
            UserName = "TestUser",
            Password = "Qwerty123!"
        };

        var users = new List<User> { user };
        var mockDbSet = users.BuildMockDbSet();

        var mockContext = new Mock<IAppDbContext>();
        mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        var service = new UserService(mockContext.Object);

        //act
        Func<Task> act = async () => await service.CreateUserAsync(createDto);

        //assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Пользователь с таким email уже существует.");
    }

    [Fact]
    public async Task CreateUserAsync_ValidData_ReturnsUser()
    {
        //arange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "TestUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!")
        };

        var createDto = new CreateUserDto
        {
            Email = "test@example.com",
            UserName = "TestUser",
            Password = "Qwerty123!"
        };

        var users = new List<User>();
        var mockDbSet = users.BuildMockDbSet();

        var mockContext = new Mock<IAppDbContext>();
        mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        var service = new UserService(mockContext.Object);

        //act
        var result = await service.CreateUserAsync(createDto);

        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("TestUser");
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
