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
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "Bff_";
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My BFF API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your valid token in the text input below.\r\n\r\nExample: \"12345abcdef\""
    });

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

// User Service
builder.Services.AddGrpcClient<Users.UsersClient>(o =>
{
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

var key = Encoding.ASCII.GetBytes("MySuperSecretKey_1234567890_MySuperSecretKey");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();