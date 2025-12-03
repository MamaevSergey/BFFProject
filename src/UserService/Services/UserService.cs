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

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.Password != request.Password)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid email or password"));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("MySuperSecretKey_1234567890_MySuperSecretKey");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name)
                }),
                Expires = DateTime.UtcNow.AddDays(1),
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