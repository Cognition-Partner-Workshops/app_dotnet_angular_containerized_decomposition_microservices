using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using Product.Domain.Interfaces;
using Product.Infrastructure.Data;

namespace Product.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<ProductEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<IReadOnlyList<ProductEntity>> GetAllAsync()
    {
        return await _context.Products
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProductEntity> AddAsync(ProductEntity product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }
}
