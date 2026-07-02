using Product.Domain.Entities;

namespace Product.Domain.Interfaces;

public interface IProductRepository
{
    Task<ProductEntity?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ProductEntity>> GetAllAsync();
    Task<ProductEntity> AddAsync(ProductEntity product);
}
