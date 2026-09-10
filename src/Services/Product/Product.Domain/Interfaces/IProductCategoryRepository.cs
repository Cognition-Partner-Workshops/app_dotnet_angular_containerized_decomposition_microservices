using Product.Domain.Entities;

namespace Product.Domain.Interfaces;

public interface IProductCategoryRepository
{
    Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductCategory> AddAsync(ProductCategory category, CancellationToken cancellationToken = default);
    Task<ProductCategory?> UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
