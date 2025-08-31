using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using StockService.Api.Controllers;
using StockService.Api.Dtos;
using StockService.Application.DTOs;
using StockService.Application.UseCases;
using StockService.Domain.Entities;
using StockService.Application.Mapping;
using Messaging;

namespace StockService.UnitTests.Controllers;

public class StockControllerTests
{
    private readonly Mock<IGetProductsUseCase> _getProductsMock = new();
    private readonly Mock<IGetProductUseCase> _getProductMock = new();
    private readonly Mock<ICreateProductUseCase> _createProductMock = new();
    private readonly Mock<IUpdateStockUseCase> _updateStockMock = new();
    private readonly Mock<IMessagePublisher> _messagePublisherMock = new();
    private readonly IMapper _mapper;

    public StockControllerTests()
    {
        var cfg = new MapperConfiguration(c => {
            c.AddProfile(new ProductProfile());
            c.AddProfile(new StockService.Api.Mapping.ApiProfile());
        });
        _mapper = cfg.CreateMapper();
    }

    [Fact]
    public async Task GetProducts_ReturnsOk_WithProducts()
    {
        // Arrange
        _getProductsMock.Setup(x => x.ExecuteAsync(It.IsAny<GetProductsQuery>()))
            .ReturnsAsync(new GetProductsResult { Products = new System.Collections.Generic.List<ProductDto> { new ProductDto { Id = 1, Name = "P" } }, TotalCount = 1 });

        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        // Act
        var result = await controller.GetProducts(null, null);

        // Assert
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var body = ok!.Value as ProductResponse;
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Products.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProducts_OnException_Returns500()
    {
        _getProductsMock.Setup(x => x.ExecuteAsync(It.IsAny<GetProductsQuery>())).ThrowsAsync(new System.Exception("boom"));

        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        var result = await controller.GetProducts(null, null);

        var status = result as ObjectResult;
        status!.StatusCode.Should().Be(500);
        var body = status.Value as ProductResponse;
        body!.Success.Should().BeFalse();
        body.Message.Should().Contain("Failed to retrieve products");
    }

    [Fact]
    public async Task GetProduct_NotFound_Returns404()
    {
        _getProductMock.Setup(x => x.ExecuteAsync(It.IsAny<GetProductQuery>()))
            .ReturnsAsync(new GetProductResult { Success = false, Message = "not found" });

        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        var result = await controller.GetProduct(10);

        var notFound = result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        var body = notFound!.Value as ProductResponse;
        body!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetProduct_Found_ReturnsOk()
    {
        var dto = new ProductDto { Id = 5, Name = "X" };
        _getProductMock.Setup(x => x.ExecuteAsync(It.IsAny<GetProductQuery>()))
            .ReturnsAsync(new GetProductResult { Success = true, Product = dto, Message = "ok" });

        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        var result = await controller.GetProduct(5);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var body = ok!.Value as ProductResponse;
        body!.Product.Should().NotBeNull();
        body.Product!.Id.Should().Be(5);
    }

    [Fact]
    public async Task CreateProduct_WhenUseCaseFails_ReturnsBadRequest()
    {
        var request = new CreateProductRequest { Name = "n", Price = 1m };
        _createProductMock.Setup(x => x.ExecuteAsync(It.IsAny<CreateProductCommand>()))
            .ReturnsAsync(new CreateProductResult { Success = false, Message = "err" });

        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        var result = await controller.CreateProduct(request);

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        var body = bad!.Value as CreateProductResponse;
        body!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProduct_OnSuccess_ReturnsCreated()
    {
        var request = new CreateProductRequest { Name = "n", Price = 1m };
        _createProductMock.Setup(x => x.ExecuteAsync(It.IsAny<CreateProductCommand>()))
            .ReturnsAsync(new CreateProductResult { Success = true, ProductId = 99, Message = "created" });

        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        var result = await controller.CreateProduct(request);

        var created = result as CreatedAtActionResult;
        created.Should().NotBeNull();
        var body = created!.Value as CreateProductResponse;
        body!.ProductId.Should().Be(99);
    }

    [Fact]
    public async Task UpdateStock_WhenUseCaseFails_ReturnsBadRequest()
    {
        var request = new UpdateStockRequest { StockQuantity = 5 };
        _updateStockMock.Setup(x => x.ExecuteAsync(It.IsAny<UpdateStockCommand>()))
            .ReturnsAsync(new UpdateStockResult { Success = false, Message = "err" });

        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        var result = await controller.UpdateStock(7, request);

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        var body = bad!.Value as StockUpdateResponse;
        body!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStock_OnSuccess_ReturnsOk()
    {
        var request = new UpdateStockRequest { StockQuantity = 3 };
        _updateStockMock.Setup(x => x.ExecuteAsync(It.IsAny<UpdateStockCommand>()))
            .ReturnsAsync(new UpdateStockResult { Success = true, Message = "ok", PreviousStock = 1, NewStock = 4 });

        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        var result = await controller.UpdateStock(7, request);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        var body = ok!.Value as StockUpdateResponse;
        body!.NewStock.Should().Be(4);
    }

    [Fact]
    public void HealthCheck_ReturnsOk()
    {
        var controller = new StockController(_getProductsMock.Object, _getProductMock.Object, _createProductMock.Object, _updateStockMock.Object, _messagePublisherMock.Object, _mapper);

        var result = controller.HealthCheck();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().Be("Stock Service is running!");
    }
}
