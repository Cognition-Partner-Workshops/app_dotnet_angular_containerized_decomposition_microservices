using Microsoft.EntityFrameworkCore;
using Product.Domain.Interfaces;
using Product.Domain.Entities;
using Product.Infrastructure.Data;

namespace Product.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Domain.Entities.Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.ProductCategory)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Domain.Entities.Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.ProductCategory)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Domain.Entities.Product> AddAsync(Domain.Entities.Product product)
    {
        product.CreatedDate = DateTime.UtcNow;
        product.UpdatedDate = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Domain.Entities.Product product)
    {
        product.UpdatedDate = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<ProductCategory>> GetAllCategoriesAsync()
    {
        return await _context.ProductCategories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ProductCategory?> GetCategoryByIdAsync(int id)
    {
        return await _context.ProductCategories.FindAsync(id);
    }

    public async Task<ProductCategory> AddCategoryAsync(ProductCategory category)
    {
        category.CreatedDate = DateTime.UtcNow;
        category.UpdatedDate = DateTime.UtcNow;
        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(ProductCategory category)
    {
        category.UpdatedDate = DateTime.UtcNow;
        _context.ProductCategories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.ProductCategories.FindAsync(id);
        if (category != null)
        {
            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
