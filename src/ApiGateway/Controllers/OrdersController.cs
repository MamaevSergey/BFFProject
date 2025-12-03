using Microsoft.AspNetCore.Mvc;
using OrderService.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ApiGateway.Controllers
{
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

        public OrdersController(Orders.OrdersClient orderClient)
        {
            _orderClient = orderClient;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto model)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdString, out int tokenUserId))
                {
                    return Unauthorized("Invalid token data");
                }

                var request = new CreateOrderRequest
                {
                    UserId = tokenUserId,
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