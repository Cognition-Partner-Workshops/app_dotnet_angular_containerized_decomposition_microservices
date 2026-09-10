using Microsoft.EntityFrameworkCore;
using Product.Domain.Interfaces;
using Product.Infrastructure.Data;
using ProductEntity = Product.Domain.Entities.Product;

namespace Product.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductCategory)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductCategory)
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<ProductEntity> AddAsync(ProductEntity product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(product.Id, cancellationToken) ?? product;
    }

    public async Task<ProductEntity?> UpdateAsync(ProductEntity product, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Products.SingleOrDefaultAsync(p => p.Id == product.Id, cancellationToken);
        if (existing is null)
            return null;

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Icon = product.Icon;
        existing.BuyingPrice = product.BuyingPrice;
        existing.SellingPrice = product.SellingPrice;
        existing.UnitsInStock = product.UnitsInStock;
        existing.IsActive = product.IsActive;
        existing.IsDiscontinued = product.IsDiscontinued;
        existing.ParentId = product.ParentId;
        existing.ProductCategoryId = product.ProductCategoryId;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(existing.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Products.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (existing is null)
            return false;

        _context.Products.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> CategoryExistsAsync(int productCategoryId, CancellationToken cancellationToken = default)
    {
        return _context.ProductCategories.AnyAsync(c => c.Id == productCategoryId, cancellationToken);
    }
}
