
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SalesService.Data;
using SalesService.Services;
using Messaging;
using Microsoft.Extensions.Options;

namespace SalesService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add Entity Framework
        builder.Services.AddDbContext<SalesDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Add JWT Authentication
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

        builder.Services.AddAuthorization();

        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

    // Add RabbitMQ messaging using shared extension (registers connection manager and publisher)
    builder.Services.AddRabbitMqMessaging(builder.Configuration);

    // Register RabbitMQ consumer implementation so hosted services can resolve it
    builder.Services.AddTransient<IMessageConsumer, RabbitMqConsumer>();

        // Add HttpClient for service-to-service communication
        builder.Services.AddHttpClient("StockService", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Services:StockService"] ?? "http://localhost:5126");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Register StockServiceClient
        builder.Services.AddScoped<IStockServiceClient, StockServiceClient>();
    // Register reservation result consumer
    builder.Services.AddHostedService<ReservationResultConsumerService>();

        var app = builder.Build();

        // Ensure database is created in development/local environment
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            db.Database.EnsureCreated();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
