using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using Product.Domain.Interfaces;
using Product.Infrastructure.Data;

namespace Product.Infrastructure.Repositories;

public class ProductCategoryRepository : IProductCategoryRepository
{
    private readonly ProductDbContext _context;

    public ProductCategoryRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductCategories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ProductCategory> AddAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<ProductCategory?> UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ProductCategories.SingleOrDefaultAsync(c => c.Id == category.Id, cancellationToken);
        if (existing is null)
            return null;

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.Icon = category.Icon;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ProductCategories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (existing is null)
            return false;

        _context.ProductCategories.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
