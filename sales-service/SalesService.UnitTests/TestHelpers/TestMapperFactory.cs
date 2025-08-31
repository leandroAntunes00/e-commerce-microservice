using AutoMapper;

namespace SalesService.UnitTests.TestHelpers;

public static class TestMapperFactory
{
    public static IMapper CreateMapper()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile(new SalesService.Application.Mappings.OrderProfile()));
        return cfg.CreateMapper();
    }
}
