using StockService.Domain.Entities;

namespace StockService.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllActiveAsync();
    Task<IEnumerable<Product>> GetByCategoryAsync(string category);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
}
