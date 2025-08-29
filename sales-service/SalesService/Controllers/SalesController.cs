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
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.Notes,
                    o.CreatedAt,
                    o.UpdatedAt,
                    Items = o.Items.Select(i => new
                    {
                        i.Id,
                        i.ProductId,
                        i.ProductName,
                        i.UnitPrice,
                        i.Quantity,
                        i.TotalPrice,
                        i.CreatedAt
                    }).ToList()
                })
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
                .Where(o => o.Id == id && o.UserId == userId)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.Notes,
                    o.CreatedAt,
                    o.UpdatedAt,
                    Items = o.Items.Select(i => new
                    {
                        i.Id,
                        i.ProductId,
                        i.ProductName,
                        i.UnitPrice,
                        i.Quantity,
                        i.TotalPrice,
                        i.CreatedAt
                    }).ToList()
                })
                .FirstOrDefault();

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

            // Async-first: não reservar sincronamente. Persistir pedido como Pending e publicar OrderCreatedEvent.

            // Calcular o total e montar order items usando dados do stock-service
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                // obter dados do produto do stock service (nome, preço)
                var product = await _stockServiceClient.GetProductAsync(item.ProductId);
                if (product == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Product not found (ID: {item.ProductId})"
                    });
                }

                var itemTotal = product.Price * item.Quantity;
                totalAmount += itemTotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
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

            // Reload saved order from database to avoid EF navigation cycles during JSON serialization
            var savedOrder = _context.Orders
                .Where(o => o.Id == order.Id)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.Notes,
                    o.CreatedAt,
                    o.UpdatedAt,
                    Items = o.Items.Select(i => new
                    {
                        i.Id,
                        i.ProductId,
                        i.ProductName,
                        i.UnitPrice,
                        i.Quantity,
                        i.TotalPrice,
                        i.CreatedAt
                    }).ToList()
                })
                .FirstOrDefault();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id },
                new
                {
                    Success = true,
                    Message = "Order created successfully",
                    Order = savedOrder
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

    // POST: api/sales/pay/card - Pagar por cartão (simulado)
    [HttpPost("pay/card")]
    public async Task<IActionResult> PayWithCard([FromBody] PaymentRequest request)
    {
        return await ProcessPayment(request, "Card");
    }

    // POST: api/sales/pay/pix - Pagar por PIX (simulado)
    [HttpPost("pay/pix")]
    public async Task<IActionResult> PayWithPix([FromBody] PaymentRequest request)
    {
        return await ProcessPayment(request, "Pix");
    }

    // POST: api/sales/pay/boleto - Pagar por boleto bancário (simulado)
    [HttpPost("pay/boleto")]
    public async Task<IActionResult> PayWithBoleto([FromBody] PaymentRequest request)
    {
        return await ProcessPayment(request, "Boleto");
    }

    private async Task<IActionResult> ProcessPayment(PaymentRequest request, string method)
    {
        try
        {
            if (!request.Confirm)
            {
                return BadRequest(new { Success = false, Message = "Payment not confirmed" });
            }

            var userId = GetCurrentUserId();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);
            if (order == null)
            {
                return NotFound(new { Success = false, Message = "Order not found" });
            }

            if (order.Status != "Pending")
            {
                return BadRequest(new { Success = false, Message = "Only pending orders can be confirmed" });
            }

            order.Status = "Confirmed";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // publicar evento de confirmação
            var evt = new OrderConfirmedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                ConfirmedAt = order.UpdatedAt ?? DateTime.UtcNow,
                Method = method
            };

            await _messagePublisher.PublishAsync(evt);

            return Ok(new { Success = true, Message = "Payment confirmed and order updated" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = $"Payment processing failed: {ex.Message}" });
        }
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
    public int Quantity { get; set; }
}

public class PaymentRequest
{
    public int OrderId { get; set; }
    public bool Confirm { get; set; }
}

