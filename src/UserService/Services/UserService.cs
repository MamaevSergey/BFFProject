using Grpc.Core;
using UserService.Protos;
using UserService.Data;
using UserService.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace UserService.Services
{
    public class UserService : Users.UsersBase
    {
        private readonly ILogger<UserService> _logger;
        private readonly UserDbContext _dbContext;

        public UserService(ILogger<UserService> logger, UserDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Ищем пользователя с ID: {request.Id}");

            var userEntity = await _dbContext.Users.FindAsync(request.Id);

            if (userEntity == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"User with ID {request.Id} not found"));
            }

            return new UserResponse
            {
                Id = userEntity.Id,
                Name = userEntity.Name,
                Email = userEntity.Email
            };
        }

        public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Создаем пользователя: {request.Name}");

            var newUser = new User
            {
                Name = request.Name,
                Email = request.Email,
                Password = request.Password
            };

            _dbContext.Users.Add(newUser);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Пользователь создан с ID: {newUser.Id}");

            return new CreateUserResponse
            {
                Id = newUser.Id
            };
        }

        public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Попытка входа: {request.Email}");

            // 1. Ищем пользователя в БД по Email
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // 2. Проверяем пароль
            // Внимание: В реальном проекте пароли хешируют! Тут мы сравниваем строки для простоты.
            if (user == null || user.Password != request.Password)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid email or password"));
            }

            // 3. Генерируем JWT Токен
            var tokenHandler = new JwtSecurityTokenHandler();
            // Секретный ключ (должен быть длинным и сложным!)
            var key = Encoding.ASCII.GetBytes("MySuperSecretKey_1234567890_MySuperSecretKey");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name)
                }),
                Expires = DateTime.UtcNow.AddDays(7), // Токен живет 7 дней
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation($"Токен выдан для User ID: {user.Id}");

            return new LoginResponse
            {
                Token = tokenString,
                UserId = user.Id
            };
        }
    }
}