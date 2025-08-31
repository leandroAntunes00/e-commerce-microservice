using AutoMapper;
using StockService.Domain.Entities;
using StockService.Application.DTOs;

namespace StockService.Application.Mapping;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        // Domain -> Application DTO
        CreateMap<Product, ProductDto>();

        // Application Command -> Domain
        CreateMap<CreateProductCommand, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<UpdateStockCommand, Product>()
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.NewStockQuantity))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}
