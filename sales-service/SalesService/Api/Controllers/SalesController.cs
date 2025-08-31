using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesService.Api.Dtos;
using SalesService.Application.Dtos;
using SalesService.Application.Services;
using SalesService.Application.UseCases;
using System.Security.Claims;
using AutoMapper;

namespace SalesService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly IOrderQueryService _orderQueryService;
    private readonly ICreateOrderUseCase _createOrderUseCase;
    private readonly IProcessPaymentUseCase _processPaymentUseCase;
    private readonly ICancelOrderUseCase _cancelOrderUseCase;
    private readonly IMapper _mapper;

    public SalesController(
    IOrderQueryService orderQueryService,
    ICreateOrderUseCase createOrderUseCase,
    IProcessPaymentUseCase processPaymentUseCase,
    ICancelOrderUseCase cancelOrderUseCase,
    IMapper mapper)
    {
        _orderQueryService = orderQueryService;
        _createOrderUseCase = createOrderUseCase;
        _processPaymentUseCase = processPaymentUseCase;
        _cancelOrderUseCase = cancelOrderUseCase;
        _mapper = mapper;
    }

    // GET: api/sales/orders - Lista pedidos do usuário logado
    [HttpGet("orders")]
    public async Task<IActionResult> GetUserOrders()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _orderQueryService.GetOrdersByUserIdAsync(userId);

            return Ok(new
            {
                Success = true,
                Message = "Orders retrieved successfully",
                Orders = result.Orders
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
    public async Task<IActionResult> GetOrder(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var order = await _orderQueryService.GetOrderByIdAsync(id, userId);

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

            var command = _mapper.Map<CreateOrderCommand>(request);
            command.UserId = userId;

            var orderId = await _createOrderUseCase.ExecuteAsync(command);

            return CreatedAtAction(nameof(GetOrder), new { id = orderId },
                new
                {
                    Success = true,
                    Message = "Order created successfully",
                    OrderId = orderId
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
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

            var command = new CancelOrderCommand
            {
                UserId = userId,
                OrderId = id
            };

            await _cancelOrderUseCase.ExecuteAsync(command);

            return Ok(new
            {
                Success = true,
                Message = "Order cancelled successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
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
        try
        {
            var userId = GetCurrentUserId();

            var command = _mapper.Map<ProcessPaymentCommand>(request);
            command.UserId = userId;
            command.PaymentMethod = "Card";

            await _processPaymentUseCase.ExecuteAsync(command);

            return Ok(new
            {
                Success = true,
                Message = "Payment processed successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            // Erro previsível (por exemplo: ordem não está em status Pending)
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Payment processing failed: {ex.Message}"
            });
        }
    }

    // POST: api/sales/pay/pix - Pagar por PIX (simulado)
    [HttpPost("pay/pix")]
    public async Task<IActionResult> PayWithPix([FromBody] PaymentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var command = _mapper.Map<ProcessPaymentCommand>(request);
            command.UserId = userId;
            command.PaymentMethod = "Pix";

            await _processPaymentUseCase.ExecuteAsync(command);

            return Ok(new
            {
                Success = true,
                Message = "Payment processed successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            // Erro previsível (por exemplo: ordem não está em status Pending)
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Payment processing failed: {ex.Message}"
            });
        }
    }

    // PUT: api/sales/pay/confirm - Confirmar pagamento (simulado)
    [HttpPut("pay/confirm")]
    public IActionResult ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Simular confirmação de pagamento
            // Por enquanto, apenas retornar sucesso
            return Ok(new { Success = true, Message = "Payment confirmed successfully" });
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