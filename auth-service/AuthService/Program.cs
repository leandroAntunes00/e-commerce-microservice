using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService.Data;
using Microsoft.AspNetCore.Http;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
using AuthService.Application.UseCases;
using AuthService.Application.Dtos;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// Register Clean Architecture Dependencies

// Infrastructure Layer
builder.Services.AddScoped<AuthService.Domain.Interfaces.IUserRepository, AuthService.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<AuthService.Domain.Interfaces.IJwtService, AuthService.Infrastructure.Services.JwtService>();
builder.Services.AddScoped<AuthService.Domain.Interfaces.IPasswordService, AuthService.Infrastructure.Services.PasswordService>();

// Application Layer
builder.Services.AddScoped<AuthService.Application.UseCases.IRegisterUserUseCase, AuthService.Application.UseCases.RegisterUserUseCase>();
builder.Services.AddScoped<AuthService.Application.UseCases.ILoginUserUseCase, AuthService.Application.UseCases.LoginUserUseCase>();
builder.Services.AddScoped<AuthService.Application.UseCases.IValidateTokenUseCase, AuthService.Application.UseCases.ValidateTokenUseCase>();

// HttpContext accessor for token validation
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Diagnostic middleware: log incoming requests (method + path)
app.Use(async (context, next) =>
{
    Console.WriteLine($"Incoming request: {context.Request.Method} {context.Request.Path}");
    await next();
});

// Health endpoint: respond directly on HTTP without redirect and also support HTTPS
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health")
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync("Auth Service is running!");
        return;
    }

    await next();
});

app.UseHttpsRedirection();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Keep a mapped endpoint for health to support HTTPS endpoint routing as well
app.MapGet("/health", () => Results.Ok("Auth Service is running!"));

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
