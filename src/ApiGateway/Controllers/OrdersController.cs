using Microsoft.AspNetCore.Mvc;
using OrderService.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization; // <--- Добавь
using System.Security.Claims; // <--- 1. Добавь это

namespace ApiGateway.Controllers
{
    // Модель данных, которую пришлет фронтенд
    public class CreateOrderDto
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly Orders.OrdersClient _orderClient;

        // Внедряем клиент заказов
        public OrdersController(Orders.OrdersClient orderClient)
        {
            _orderClient = orderClient;
        }

        // POST /api/orders
        [HttpPost]
        [Authorize] // <--- ТЕПЕРЬ СЮДА НЕЛЬЗЯ БЕЗ ТОКЕНА!
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto model)
        {
            try
            {
                // 2. БЕЗОПАСНОСТЬ: Достаем ID из токена, а не из JSON
                // ClaimTypes.NameIdentifier - это стандартное имя для ID, которое мы зашили в UserService
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdString, out int tokenUserId))
                {
                    return Unauthorized("Invalid token data");
                }

                // 3. Формируем gRPC запрос, используя ID из токена
                var request = new CreateOrderRequest
                {
                    UserId = tokenUserId, // <--- ЖЕСТКО ЗАДАЕМ ID ИЗ ТОКЕНА (model.UserId игнорируем)
                    ProductId = model.ProductId,
                    Quantity = model.Quantity
                };

                var response = await _orderClient.CreateOrderAsync(request);

                return Ok(new { OrderId = response.OrderId, Message = "Order created successfully" });
            }
            catch (RpcException ex)
            {
                return base.StatusCode(500, $"gRPC Error: {ex.Status.Detail}");
            }
        }
    }
}