using Application.Handlers;
using Application.Interfaces;
using Infrastructure.Configurations;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Seq;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .CreateLogger();

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    // JwtSettings
    var jwtSection = builder.Configuration.GetSection("JwtSettings");
    var jwtSettings = jwtSection.Get<JwtSettings>();

    // если настроек нет (например, в тестах CI/CD), создаются дефолтные, чтобы приложение не упало
    if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
    {
        jwtSettings = new JwtSettings
        {
            SecretKey = "A_Very_Long_Secret_Key_For_Testing_Purposes_32_Chars_Minimum",
            Issuer = "TodoListAPI",
            Audience = "TodoListAPI",
            ExpiryMinutes = 15
        };
    }

    builder.Services.AddSingleton<JwtSettings>(jwtSettings);

    builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(GetTasksHandler).Assembly));

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IAppDbContext>(provider =>
        provider.GetRequiredService<AppDbContext>());

    // Services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<ITaskService, TaskService>();
    builder.Services.AddScoped<ITagService, TagService>();

    builder.Services.AddHttpContextAccessor();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Authentication
    builder.Services.AddAuthorization();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["accessToken"];

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddControllers();

    builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.Seq("http://localhost:5341")
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning));


    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    //app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            db.Database.Migrate();
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}