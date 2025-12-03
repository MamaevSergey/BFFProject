using Microsoft.AspNetCore.Mvc;
using Grpc.Core;
using UserService.Protos;

namespace ApiGateway.Controllers
{
    // DTO для входа
    public class LoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly Users.UsersClient _userClient;

        public AuthController(Users.UsersClient userClient)
        {
            _userClient = userClient;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            try
            {
                // gRPC запрос
                var request = new LoginRequest
                {
                    Email = model.Email,
                    Password = model.Password
                };

                var response = await _userClient.LoginAsync(request);

                return Ok(new
                {
                    Token = response.Token,
                    UserId = response.UserId
                });
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
            {
                return Unauthorized("Неверный email или пароль");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Error: {ex.Message}");
            }
        }
    }
}