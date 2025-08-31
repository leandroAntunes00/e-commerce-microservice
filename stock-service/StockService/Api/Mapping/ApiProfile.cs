using AutoMapper;
using StockService.Api.Dtos;
using StockService.Application.DTOs;

namespace StockService.Api.Mapping;

public class ApiProfile : Profile
{
    public ApiProfile()
    {
        // API -> Application
        CreateMap<CreateProductRequest, CreateProductCommand>();
        CreateMap<UpdateStockRequest, UpdateStockCommand>()
            .ForMember(dest => dest.NewStockQuantity, opt => opt.MapFrom(src => src.StockQuantity));

        // Application -> API (for responses)
        CreateMap<ProductDto, ProductResponse>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.TotalCount, opt => opt.Ignore());
    }
}
