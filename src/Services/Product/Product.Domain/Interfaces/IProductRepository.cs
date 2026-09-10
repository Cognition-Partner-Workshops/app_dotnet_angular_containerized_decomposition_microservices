using ProductEntity = Product.Domain.Entities.Product;

namespace Product.Domain.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductEntity> AddAsync(ProductEntity product, CancellationToken cancellationToken = default);
    Task<ProductEntity?> UpdateAsync(ProductEntity product, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int productCategoryId, CancellationToken cancellationToken = default);
}
