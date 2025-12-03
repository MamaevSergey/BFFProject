using Grpc.Core;
using ProductService.Protos;
using ProductService.Data;
using ProductService.Models;
using Microsoft.EntityFrameworkCore;

namespace ProductService.Services
{
    public class ProductService : Products.ProductsBase
    {
        private readonly ILogger<ProductService> _logger;
        private readonly ProductDbContext _dbContext;

        public ProductService(ILogger<ProductService> logger, ProductDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        private async Task EnsureProductsExist()
        {
            if (!await _dbContext.Products.AnyAsync())
            {
                _logger.LogInformation("База товаров пуста. Добавляем тестовые товары...");
                _dbContext.Products.AddRange(
                    new Product { Name = "iPhone 15", Price = 999.99, Description = "Apple Smartphone" },
                    new Product { Name = "Samsung Galaxy S24", Price = 899.50, Description = "Android Flagship" },
                    new Product { Name = "MacBook Pro", Price = 1999.00, Description = "Laptop for devs" }
                );
                await _dbContext.SaveChangesAsync();
            }
        }

        public override async Task<ProductModel> GetProductById(GetProductByIdRequest request, ServerCallContext context)
        {
            await EnsureProductsExist();

            var product = await _dbContext.Products.FindAsync(request.Id);

            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
            }

            return new ProductModel
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description
            };
        }
    }
}