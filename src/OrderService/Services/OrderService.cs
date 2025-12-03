using Grpc.Core;
using OrderService.Protos;
using OrderService.Data;
using OrderService.Models;
using Microsoft.EntityFrameworkCore;
using ProductService.Protos;

namespace OrderService.Services
{
    public class OrderService : Orders.OrdersBase
    {
        private readonly ILogger<OrderService> _logger;
        private readonly OrderDbContext _dbContext;
        private readonly Products.ProductsClient _productClient;

        public OrderService(ILogger<OrderService> logger, OrderDbContext dbContext, Products.ProductsClient productClient)
        {
            _logger = logger;
            _dbContext = dbContext;
            _productClient = productClient;
        }

        public override async Task<OrdersListResponse> GetOrdersByUser(GetOrdersRequest request, ServerCallContext context)
        {
            var ordersFromDb = await _dbContext.Orders
                .Where(o => o.UserId == request.UserId)
                .ToListAsync();

            var response = new OrdersListResponse();

            foreach (var order in ordersFromDb)
            {
                response.Orders.Add(new OrderModel
                {
                    Id = order.Id,
                    ProductName = order.ProductName,
                    Price = order.Price,
                    CreatedAt = order.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            return Task.FromResult(response).Result;
        }

        public override async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
        {
            var productResponse = await _productClient.GetProductByIdAsync(new GetProductByIdRequest { Id = request.ProductId });

            var newOrder = new Order
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                ProductName = productResponse.Name,
                Price = productResponse.Price,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Orders.Add(newOrder);
            await _dbContext.SaveChangesAsync();

            return new CreateOrderResponse { OrderId = newOrder.Id };
        }
    }
}