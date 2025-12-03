using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;
using ProductService.Protos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddGrpcClient<Products.ProductsClient>(o =>
{
    var url = builder.Configuration["Services:Product"] ?? "http://localhost:5003";
    o.Address = new Uri(url);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.MapGrpcService<OrderService.Services.OrderService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
