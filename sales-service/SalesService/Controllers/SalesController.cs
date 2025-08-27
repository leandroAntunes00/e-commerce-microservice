using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.Models;
using SalesService.Services;
using Messaging;
using Messaging.Events;
using System.Security.Claims;

namespace SalesService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly SalesDbContext _context;
    private readonly IStockServiceClient _stockServiceClient;
    private readonly IMessagePublisher _messagePublisher;

    public SalesController(SalesDbContext context, IStockServiceClient stockServiceClient, IMessagePublisher messagePublisher)
    {
        _context = context;
        _stockServiceClient = stockServiceClient;
        _messagePublisher = messagePublisher;
    }

    // GET: api/sales/orders - Lista pedidos do usuário logado
    [HttpGet("orders")]
    public IActionResult GetUserOrders()
    {
        try
        {
            var userId = GetCurrentUserId();
            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return Ok(new
            {
                Success = true,
                Message = "Orders retrieved successfully",
                Orders = orders
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Failed to retrieve orders: {ex.Message}"
            });
        }
    }

    // GET: api/sales/orders/{id} - Obtém pedido específico
    [HttpGet("orders/{id}")]
    public IActionResult GetOrder(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var order = _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Order not found"
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Order retrieved successfully",
                Order = order
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Failed to retrieve order: {ex.Message}"
            });
        }
    }

    // POST: api/sales/orders - Cria novo pedido
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verificar estoque para todos os itens antes de criar o pedido
            foreach (var item in request.Items)
            {
                var stockAvailable = await _stockServiceClient.CheckStockAvailability(item.ProductId, item.Quantity);
                if (!stockAvailable)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Insufficient stock for product {item.ProductName} (ID: {item.ProductId}). Requested: {item.Quantity}"
                    });
                }
            }

            // Calcular o total
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                var itemTotal = item.UnitPrice * item.Quantity;
                totalAmount += itemTotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = itemTotal
                });
            }

            var order = new Order
            {
                UserId = userId,
                Status = "Pending",
                TotalAmount = totalAmount,
                Notes = request.Notes,
                Items = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Publicar evento de pedido criado
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                UserId = userId,
                Items = order.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt
            };

            await _messagePublisher.PublishAsync(orderCreatedEvent);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id },
                new
                {
                    Success = true,
                    Message = "Order created successfully",
                    Order = order
                });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Failed to create order: {ex.Message}"
            });
        }
    }

    // PUT: api/sales/orders/{id}/cancel - Cancela pedido
    [HttpPut("orders/{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Order not found"
                });
            }

            if (order.Status != "Pending")
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Only pending orders can be cancelled"
                });
            }

            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Order cancelled successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Failed to cancel order: {ex.Message}"
            });
        }
    }

    // GET: api/sales/health - Health check
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        return Ok("Sales Service is running!");
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

// Request/Response models
public class CreateOrderRequest
{
    public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
    public string? Notes { get; set; }
}

public class OrderItemRequest
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
