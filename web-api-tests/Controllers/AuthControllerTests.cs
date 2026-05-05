using Application.DTOs.Jwt;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using web_api_tests.Helpers;

namespace web_api_tests.Controllers;

public class AuthControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public AuthControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsCookiesAndReturnsUser()
    {
        //arange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Users.RemoveRange(db.Users);

            db.Users.Add(new User
            {
                Email = "test@example.com",
                Name = "TestUser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!")
            });
            await db.SaveChangesAsync();
        }

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Qwerty123!"
        };

        //act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
    
        //asert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("Set-Cookie");

        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();

        setCookieHeaders.Should().Contain(header => header.Contains("accessToken"));
        setCookieHeaders.Should().Contain(header => header.Contains("refreshToken"));
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        //arange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Users.RemoveRange(db.Users);

            db.Users.Add(new User
            {
                Email = "test@example.com",
                Name = "TestUser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Qwerty123!")
            });
            await db.SaveChangesAsync();
        };

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "invalid!"
        };

        //act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().NotContainKey("Set-Cookie");
    }

    [Fact]
    public async Task Logout_ShouldDeleteCookies()
    {
        //act
        var response = await _client.PostAsync("/api/auth/logout", null);

        //asert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();

        setCookieHeaders.Should().Contain(header => header.Contains("accessToken") && header.Contains("1970"));
        setCookieHeaders.Should().Contain(header => header.Contains("refreshToken") && header.Contains("1970"));
    }
}