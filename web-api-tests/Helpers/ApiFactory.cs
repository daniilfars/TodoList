using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace web_api_tests.Helpers;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. ЖЕСТКО ЗАДАЕМ НАСТРОЙКИ (JWT и др.)
        // Это перекроет любые значения из appsettings.json
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "A_Very_Long_Secret_Key_For_Testing_Purposes_32_Chars_Minimum",
                ["JwtSettings:Issuer"] = "TodoListAPI",
                ["JwtSettings:Audience"] = "TodoListAPI",
                ["JwtSettings:ExpiryMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            // 2. Вычищаем старые регистрации БД
            var descriptors = services.Where(
                d => d.ServiceType.Name.Contains("DbContextOptions") ||
                     d.ServiceType == typeof(AppDbContext) ||
                     d.ServiceType == typeof(IAppDbContext)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // 3. Регистрируем InMemory БД
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.UseInternalServiceProvider(null);
            });

            // 4. Привязываем интерфейс
            services.AddScoped<IAppDbContext>(provider =>
                provider.GetRequiredService<AppDbContext>());
        });
    }
}


