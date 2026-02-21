using InvServer.Api.Filters;
using InvServer.Api.Middleware;
using InvServer.Core.Interfaces;
using InvServer.Infrastructure;
using InvServer.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth_refresh", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("workflow_mutation", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 10;
        opt.QueueLimit = 2;
    });

    options.AddFixedWindowLimiter("fulfillment", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 20;
    });
});


// Add services to the container.
builder.Services.AddCors(options => 
{
    options.AddPolicy("Default", p => p
        .WithOrigins("http://localhost:3000", "http://localhost:3001")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});
builder.Services.AddControllers(options => 
{
    options.Filters.Add<GlobalExceptionFilter>();
})
.AddJsonOptions(options => 
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<InvDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auth & RBAC
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<PermissionFilter>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IAuditService, AuditService>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Auto-Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InvDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    // Check if database exists, create if not
    try
    {
        await context.Database.MigrateAsync();
    }
    catch (Npgsql.PostgresException ex) when (ex.SqlState == "08P01" || ex.Message.Contains("does not exist"))
    {
        // Database doesn't exist, create it
        var connectionString = config.GetConnectionString("DefaultConnection");
        var builder2 = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder2.Database;
        builder2.Database = "postgres"; // Connect to system database
        
        using var conn = new Npgsql.NpgsqlConnection(builder2.ToString());
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        await cmd.ExecuteNonQueryAsync();
        
        // Now run migrations
        await context.Database.MigrateAsync();
    }
    
    await DbSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles(); // Enable serving swagger.json
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger.json", "InvServer API v1.1");
        c.RoutePrefix = "swagger"; 
    });
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
