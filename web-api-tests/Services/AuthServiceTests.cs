using Application.DTOs.Jwt;
using Application.Interfaces;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace web_api_tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "TestUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!")
        };

        var users = new List<User> { user };
        var mockDbSet = users.BuildMockDbSet();

        var mockContext = new Mock<IAppDbContext>();
        mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        var mockTokenService = new Mock<ITokenService>();
        var expectedToken = "fake-jwt-token";
        var expectedRefreshToken = "fake-refresh-token";

        mockTokenService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(expectedToken);
        mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns(expectedRefreshToken);

        var authService = new AuthService(mockContext.Object, mockTokenService.Object, Mock.Of<IUserService>());

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Qwerty123!"
        };
        //Act
        var result = await authService.LoginAsync(loginDto);
        //Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(expectedToken);
        result.RefreshToken.Should().Be(expectedRefreshToken);
        result.User.Id.Should().Be(user.Id);
        result.User.Email.Should().Be(user.Email);

        mockTokenService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Once);
        mockTokenService.Verify(x => x.GenerateRefreshToken(), Times.Once);
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "TestUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!")
        };
        
        var users = new List<User> { user };
        var mockDbSet = users.BuildMockDbSet();

        var mockContext = new Mock<IAppDbContext>();
        mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        var mockTokenService = new Mock<ITokenService>();

        var authService = new AuthService(mockContext.Object, mockTokenService.Object, Mock.Of<IUserService>());

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Qwertyyy"
        };

        //act
        var result = await authService.LoginAsync(loginDto);

        //assert
        result.Should().BeNull();

        mockTokenService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
        mockTokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        // arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "TestUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!")
        };

        var users = new List<User> { user };
        var mockDbSet = users.BuildMockDbSet();

        var mockContext = new Mock<IAppDbContext>();
        mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        var mockTokenService = new Mock<ITokenService>();

        var authService = new AuthService(mockContext.Object, mockTokenService.Object, Mock.Of<IUserService>());

        var loginDto = new LoginDto
        {
            Email = "test123@example.com",
            Password = "Qwerty123!"
        };

        //act
        var result = await authService.LoginAsync(loginDto);

        //assert
        result.Should().BeNull();

        mockTokenService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
        mockTokenService.Verify(x => x.GenerateRefreshToken(), Times.Never);
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsAuthResponse()
    {
        // arrange
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        /*using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }*/

        await using var realContext = new AppDbContext(options);
        await realContext.Database.EnsureCreatedAsync();

        var mockTokenService = new Mock<ITokenService>();
        var expectedToken = "fake-jwt-token";
        var expectedRefreshToken = "fake-refresh-token";

        mockTokenService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(expectedToken);
        mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns(expectedRefreshToken);

        var userService = new UserService(realContext);

        var authService = new AuthService(realContext, mockTokenService.Object, userService);

        var registerDto = new RegisterDto
        {
            Email = "integration@example.com",
            Password = "Qwerty123!",
            UserName = "IntegrationUser"
        };

        //act
        var result = await authService.RegisterAsync(registerDto);

        //assert
        result.Should().NotBeNull();
        result.Token.Should().Be(expectedToken);
        result.RefreshToken.Should().Be(expectedRefreshToken);
        result.User.Id.Should().BeGreaterThan(0);
        result.User.Email.Should().Be(registerDto.Email);

        var savedUser = await realContext.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        savedUser.Should().NotBeNull();
        savedUser.Name.Should().Be(registerDto.UserName);

        mockTokenService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Once);
        mockTokenService.Verify(x => x.GenerateRefreshToken(), Times.Once);
    }
}