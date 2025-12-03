using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderService.Protos;
using ProductService.Protos;
using System.Text;
using UserService.Protos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    // Если в конфиге пусто, берем localhost
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "Bff_";
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Настраиваем Swagger для поддержки JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My BFF API", Version = "v1" });

    // Описываем схему авторизации (Bearer)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // <--- Меняем на Http
        Scheme = "bearer", // <--- Указываем схему
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your valid token in the text input below.\r\n\r\nExample: \"12345abcdef\""
    });

    // Требуем эту схему для всех защищенных эндпоинтов
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// --- НАСТРОЙКА gRPC С RETRY (ПОВТОРАМИ) ---

// User Service
builder.Services.AddGrpcClient<Users.UsersClient>(o =>
{
    // Пытаемся взять адрес из конфига (Docker), если нет - берем localhost (Локально)
    var url = builder.Configuration["Services:User"] ?? "http://localhost:5001";
    o.Address = new Uri(url);
})
.AddStandardResilienceHandler();

// Order Service
builder.Services.AddGrpcClient<Orders.OrdersClient>(o =>
{
    var url = builder.Configuration["Services:Order"] ?? "http://localhost:5002";
    o.Address = new Uri(url);
})
.AddStandardResilienceHandler();

// Product Service
builder.Services.AddGrpcClient<Products.ProductsClient>(o =>
{
    var url = builder.Configuration["Services:Product"] ?? "http://localhost:5003";
    o.Address = new Uri(url);
})
.AddStandardResilienceHandler();

// -------------------------------------------

// НАСТРОЙКА JWT AUTHENTICATION
var key = Encoding.ASCII.GetBytes("MySuperSecretKey_1234567890_MySuperSecretKey"); // Тот же ключ, что в UserService!

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Для localhost можно false
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, // Упрощаем для теста
        ValidateAudience = false // Упрощаем для теста
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // <-- Сначала проверяем "Кто ты?"
app.UseAuthorization();  // <-- Потом проверяем "Можно ли тебе?"

app.MapControllers();

app.Run();