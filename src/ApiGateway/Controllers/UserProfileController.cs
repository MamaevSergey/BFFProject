using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed; // 1. Нужно для работы с Redis
using System.Text.Json; // 2. Нужно для превращения объекта в строку JSON
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
        private readonly IDistributedCache _cache; // 3. Внедряем интерфейс кэша

        // Добавляем IDistributedCache в конструктор
        public UserProfileController(Users.UsersClient userClient, Orders.OrdersClient orderClient, IDistributedCache cache)
        {
            _userClient = userClient;
            _orderClient = orderClient;
            _cache = cache;
        }

        // POST: Регистрация (без кэша)
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

        // GET: Профиль с КЭШИРОВАНИЕМ и Fallback
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            // --- 1. ПРОВЕРКА КЭША (REDIS) ---
            string cacheKey = $"profile_{userId}"; // Уникальный ключ, например "Bff_profile_1"

            // Пытаемся быстро прочитать строку из памяти
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                // УРА! Данные найдены. Сервисы не трогаем.
                // Превращаем строку обратно в объект C#
                var resultFromCache = JsonSerializer.Deserialize<object>(cachedData);
                return Ok(resultFromCache);
            }

            // --- 2. ЕСЛИ КЭШ ПУСТ - ИДЕМ К СЕРВИСАМ (Тяжелая работа) ---

            // А. Получаем юзера
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

            // Б. Получаем заказы (с Fallback)
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

            // В. Собираем итоговый объект
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

            // --- 3. СОХРАНЯЕМ В КЭШ (REDIS) ---

            // Настраиваем время жизни кэша (30 секунд)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            };

            // Превращаем результат в JSON строку
            var jsonResponse = JsonSerializer.Serialize(result);

            // Записываем в Redis
            await _cache.SetStringAsync(cacheKey, jsonResponse, cacheOptions);

            return Ok(result);
        }
    }
}