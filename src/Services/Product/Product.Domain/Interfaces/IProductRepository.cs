using Product.Domain.Entities;

namespace Product.Domain.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Entities.Product>> GetAllAsync();
    Task<Entities.Product?> GetByIdAsync(int id);
    Task<Entities.Product> AddAsync(Entities.Product product);
    Task UpdateAsync(Entities.Product product);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<ProductCategory>> GetAllCategoriesAsync();
    Task<ProductCategory?> GetCategoryByIdAsync(int id);
    Task<ProductCategory> AddCategoryAsync(ProductCategory category);
    Task UpdateCategoryAsync(ProductCategory category);
    Task DeleteCategoryAsync(int id);
}
