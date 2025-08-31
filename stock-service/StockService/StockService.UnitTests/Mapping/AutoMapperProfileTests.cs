using Xunit;
using AutoMapper;
using FluentAssertions;
using StockService.Application.Mapping;
using StockService.Api.Mapping;
using StockService.Domain.Entities;
using StockService.Application.DTOs;
using StockService.Api.Dtos;

namespace StockService.UnitTests.Mapping;

public class AutoMapperProfileTests
{
    private readonly IMapper _mapper;

    public AutoMapperProfileTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ProductProfile());
            cfg.AddProfile(new ApiProfile());
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Product_To_ProductDto_Mapping_IsValid()
    {
        var product = new Product
        {
            Id = 5,
            Name = "P",
            Description = "D",
            Price = 1.5m,
            Category = "C",
            StockQuantity = 3,
            ImageUrl = "u",
            CreatedAt = System.DateTime.UtcNow
        };

        var dto = _mapper.Map<ProductDto>(product);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(5);
        dto.Name.Should().Be("P");
    }

    [Fact]
    public void CreateProductRequest_To_CreateProductCommand_Mapping_IsValid()
    {
        var req = new CreateProductRequest
        {
            Name = "P",
            Description = "D",
            Price = 2m,
            Category = "C",
            StockQuantity = 4,
            ImageUrl = "u"
        };

        var cmd = _mapper.Map<CreateProductCommand>(req);

        cmd.Should().NotBeNull();
        cmd.Name.Should().Be("P");
        cmd.Price.Should().Be(2m);
    }
}
