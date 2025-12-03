using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using OrderService.Protos;
using UserService.Protos;

namespace ApiGateway.Controllers
{
    public class RegisterUserDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    [ApiController]
    [Route("api/profile")]
    public class UserProfileController : ControllerBase
    {
        private readonly Users.UsersClient _userClient;
        private readonly Orders.OrdersClient _orderClient;
        private readonly IDistributedCache _cache;

        public UserProfileController(Users.UsersClient userClient, Orders.OrdersClient orderClient, IDistributedCache cache)
        {
            _userClient = userClient;
            _orderClient = orderClient;
            _cache = cache;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto model)
        {
            try
            {
                var request = new CreateUserRequest
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = model.Password
                };

                var response = await _userClient.CreateUserAsync(request);
                return Ok(new { UserId = response.Id, Message = "User created successfully" });
            }
            catch (RpcException ex)
            {
                return base.StatusCode(500, $"gRPC Error: {ex.Status.Detail}");
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            string cacheKey = $"profile_{userId}";

            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var resultFromCache = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(resultFromCache);
            }

            UserResponse userResponse;
            try
            {
                userResponse = await _userClient.GetUserAsync(new GetUserRequest { Id = userId });
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                return NotFound($"User with ID {userId} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(503, $"User Service Unavailable: {ex.Message}");
            }

            object ordersData;
            try
            {
                var ordersResponse = await _orderClient.GetOrdersByUserAsync(new GetOrdersRequest { UserId = userId });
                ordersData = ordersResponse.Orders.Select(o => new
                {
                    OrderId = o.Id,
                    Product = o.ProductName,
                    Price = o.Price,
                    Date = o.CreatedAt
                });
            }
            catch (Exception)
            {
                ordersData = new[]
                {
                    new { Product = "⚠️ История заказов временно недоступна", Price = 0.0 }
                };
            }

            var result = new
            {
                UserInfo = new
                {
                    Id = userResponse.Id,
                    Name = userResponse.Name,
                    Email = userResponse.Email
                },
                Orders = ordersData
            };

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            };

            var jsonResponse = JsonSerializer.Serialize(result);

            await _cache.SetStringAsync(cacheKey, jsonResponse, cacheOptions);

            return Ok(result);
        }
    }
}