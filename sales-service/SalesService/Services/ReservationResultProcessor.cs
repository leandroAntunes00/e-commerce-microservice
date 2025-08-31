using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Messaging;
using Messaging.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesService.Data;

namespace SalesService.Services;

public interface IReservationResultProcessor
{
    Task ProcessAsync(OrderReservationCompletedEvent evt);
}

public class ReservationResultProcessor : IReservationResultProcessor
{
    private readonly SalesDbContext _context;
    private readonly IMessagePublisher _publisher;
    private readonly IMapper _mapper;
    private readonly ILogger<ReservationResultProcessor> _logger;

    public ReservationResultProcessor(SalesDbContext context, IMessagePublisher publisher, IMapper mapper, ILogger<ReservationResultProcessor> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessAsync(OrderReservationCompletedEvent evt)
    {
        _logger.LogInformation($"Processing reservation result: OrderId={evt.OrderId}, Success={evt.Success}");

        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == evt.OrderId);
        if (order == null)
        {
            _logger.LogWarning($"Order not found: {evt.OrderId}");
            return;
        }

        if (evt.Success)
        {
            order.Status = SalesService.Domain.Enums.OrderStatus.Reserved.ToString();
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Order {evt.OrderId} marked as Reserved");
        }
        else
        {
            order.Status = SalesService.Domain.Enums.OrderStatus.Cancelled.ToString();
            order.Notes = evt.Reason;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Order {evt.OrderId} Cancelled due reservation failure: {evt.Reason}");

            var cancelEvt = _mapper.Map<OrderCancelledEvent>(order);
            cancelEvt.CancelledAt = DateTime.UtcNow;

            await _publisher.PublishAsync(cancelEvt);
        }
    }
}
