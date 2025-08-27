using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetGatewayInfo()
    {
        var info = new
        {
            Service = "API Gateway",
            Version = "1.0.0",
            Description = "Microservices API Gateway using YARP",
            Services = new[]
            {
                new { Name = "Auth Service", Url = "http://localhost:5051", Path = "/api/auth" },
                new { Name = "Stock Service", Url = "http://localhost:5126", Path = "/api/stock" },
                new { Name = "Sales Service", Url = "http://localhost:5082", Path = "/api/sales" }
            },
            Status = "Running",
            Timestamp = DateTime.UtcNow
        };

        return Ok(info);
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "API Gateway"
        });
    }
}
