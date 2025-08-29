using Microsoft.EntityFrameworkCore;
using StockService.Data;
using StockService.Domain.Entities;
using StockService.Domain.Interfaces;

namespace StockService.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly StockDbContext _context;

    public ProductRepository(StockDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Product>> GetAllActiveAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        return await _context.Products
            .Where(p => p.IsActive && p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await GetByIdAsync(id);
        if (product != null)
        {
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(product);
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Products.AnyAsync(p => p.Id == id && p.IsActive);
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
    {
        return await _context.Products
            .Where(p => p.IsActive &&
                       (p.Name.Contains(searchTerm) ||
                        p.Description.Contains(searchTerm) ||
                        p.Category.Contains(searchTerm)))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
