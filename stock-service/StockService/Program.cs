using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StockService.Data;
using StockService.Controllers;
using StockService.Services;
using Messaging;

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

var app = builder.Build();

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

// Seed DB quando o RabbitMQ estiver disponível (apenas em Development por padrão)
if (app.Environment.IsDevelopment())
{
    _ = Task.Run(async () =>
    {
        // Tentativas para esperar RabbitMQ subir (para que os testes E2E possam usar dados seed)
        var maxRetries = 15;
        var delay = TimeSpan.FromSeconds(2);
        int tryCount = 0;

        while (tryCount < maxRetries)
        {
            try
            {
                // Tenta resolver IMessagePublisher para verificar se infra de mensagens está pronta
                using var scope = app.Services.CreateScope();
                var publisher = scope.ServiceProvider.GetService<Messaging.IMessagePublisher>();
                if (publisher != null)
                {
                    await StockService.Data.DbSeeder.SeedAsync(app.Services);
                    break;
                }
            }
            catch
            {
                // ignorar e tentar novamente
            }

            tryCount++;
            await Task.Delay(delay);
        }
    });
}

app.Run();
