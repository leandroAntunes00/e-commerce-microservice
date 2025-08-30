using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StockService.Data;
using Messaging;
using StockService.Domain.Interfaces;
using StockService.Domain.Entities;
using StockService.Application.UseCases;
using StockService.Application.DTOs;
using StockService.Infrastructure.Repositories;
using StockService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL
builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("USER", "ADMIN"));
});

// Configure RabbitMQ using shared extension
builder.Services.AddRabbitMqMessaging(builder.Configuration);

// Register consumer as transient so we can create one instance per queue
builder.Services.AddTransient<IMessageConsumer, RabbitMqConsumer>();
builder.Services.AddHostedService<OrderEventConsumerService>();

// Register Clean Architecture Dependencies

// Infrastructure Layer
builder.Services.AddScoped<StockService.Domain.Interfaces.IProductRepository, StockService.Infrastructure.Repositories.ProductRepository>();

// Application Layer
builder.Services.AddScoped<StockService.Application.UseCases.IGetProductsUseCase, StockService.Application.UseCases.GetProductsUseCase>();
builder.Services.AddScoped<StockService.Application.UseCases.IGetProductUseCase, StockService.Application.UseCases.GetProductUseCase>();
builder.Services.AddScoped<StockService.Application.UseCases.ICreateProductUseCase, StockService.Application.UseCases.CreateProductUseCase>();
builder.Services.AddScoped<StockService.Application.UseCases.IUpdateStockUseCase, StockService.Application.UseCases.UpdateStockUseCase>();

var app = builder.Build();

// Run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => "Stock Service is running!");

app.Run();
