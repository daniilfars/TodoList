using Application.DTOs.CreateDTOs;
using Application.DTOs.Jwt;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Net;
using System.Net.Http.Json;
using web_api_tests.Helpers;

namespace web_api_tests.Controllers;

public class ProjectControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public ProjectControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    [Fact]
    public async Task CreateProject_WhenAuthorized_ReturnsCreated()
    {
        //arange
        var user = new User
        {
            Email = "test@example.com",
            Name = "TestUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!")
        };
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.RemoveRange(db.Users);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Qwerty123!"
        };
        await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        var newProject = new CreateProjectDto
        {
            UserId = user.Id,
            Name = "Test",
            Description = "Test"
        };

        //act
        var response = await _client.PostAsJsonAsync("api/project", newProject);

        //asert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Name == "Test");
            project.Should().NotBeNull();
            project.UserId.Should().Be(user.Id);
        }
    }

    [Fact]
    public async Task DeleteProject_WhenAuthorizedAndProjectExists_ReturnsNoContent()
    {
        //arange
        var user = new User
        {
            Email = "test@example.com",
            Name = "TestUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!"),
        };
        var project = new Project
        {
            Name = "Test",
            Description = "Test",
            User = user
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            db.Projects.Add(project);
            await db.SaveChangesAsync();
        };

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Qwerty123!"
        };
        await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        //act
        var response = await _client.DeleteAsync($"/api/project/{project.Id}");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var result = await db.Projects.FirstOrDefaultAsync(p => p.Name == "Test");
            result.Should().BeNull();
        }
    }
}
