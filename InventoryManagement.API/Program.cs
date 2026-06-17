using InventoryManagement.API;
using InventoryManagement.API.Extensions;
using InventoryManagement.API.Middlewares;
using InventoryManagement.Application;
using InventoryManagement.Infrastructure;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

builder.AddLoggingServices();

builder.Services
    .AddApi()
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
    
}
