using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using ProductEntity = Product.Domain.Entities.Product;

namespace Product.Infrastructure.Data;

public static class ProductDbSeeder
{
    public static async Task SeedAsync(ProductDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.ProductCategories.AnyAsync(cancellationToken))
            return;

        var none = new ProductCategory
        {
            Name = "None",
            Description = "Default category. Products that have not been assigned a category"
        };

        var cars = new ProductCategory
        {
            Name = "Cars",
            Description = "Motor vehicles"
        };

        context.ProductCategories.AddRange(none, cars);

        context.Products.AddRange(
            new ProductEntity
            {
                Name = "BMW M6",
                Description = "Yet another masterpiece from the world's best car manufacturer",
                BuyingPrice = 109775m,
                SellingPrice = 114234m,
                UnitsInStock = 12,
                IsActive = true,
                ProductCategory = cars
            },
            new ProductEntity
            {
                Name = "Nissan Patrol",
                Description = "A true man's choice",
                BuyingPrice = 78990m,
                SellingPrice = 86990m,
                UnitsInStock = 4,
                IsActive = true,
                ProductCategory = cars
            },
            new ProductEntity
            {
                Name = "Uncategorised sample",
                Description = "Product that has not been assigned a real category",
                BuyingPrice = 10m,
                SellingPrice = 15m,
                UnitsInStock = 100,
                IsActive = true,
                ProductCategory = none
            });

        await context.SaveChangesAsync(cancellationToken);
    }
}
