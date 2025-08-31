using AutoMapper;
using SalesService.Api.Dtos;
using SalesService.Domain.Entities;
using Messaging.Events;

namespace SalesService.Application.Mappings;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderResponse>();
        CreateMap<OrderItem, OrderItemResponse>();

        // From API requests to application commands
        CreateMap<CreateOrderRequest, SalesService.Application.Dtos.CreateOrderCommand>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));
        CreateMap<OrderItemRequest, SalesService.Application.Dtos.OrderItemCommand>();
        // Payment mapping
        CreateMap<SalesService.Api.Dtos.PaymentRequest, SalesService.Application.Dtos.ProcessPaymentCommand>()
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.OrderId))
            .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount));

        // Domain -> Events
        CreateMap<OrderItem, OrderItemEvent>();

        CreateMap<Order, OrderCreatedEvent>()
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items))
            .ForMember(d => d.TotalAmount, o => o.MapFrom(s => s.TotalAmount))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));

        CreateMap<Order, OrderCancelledEvent>()
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));

        CreateMap<Order, OrderConfirmedEvent>()
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.ConfirmedAt, o => o.MapFrom(s => s.UpdatedAt ?? DateTime.UtcNow))
            .ForMember(d => d.Method, o => o.Ignore());
    }
}
