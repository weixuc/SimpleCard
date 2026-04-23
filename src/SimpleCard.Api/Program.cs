using SimpleCard.Api.Middleware;
using SimpleCard.Application;
using SimpleCard.ExchangeRateClient;
using SimpleCard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Card Transaction API", Version = "v1" }));
builder.Services.AddHealthChecks();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddExchangeRateClient();

var app = builder.Build();

if (builder.Configuration.GetValue<string>("DatabaseProvider") != "InMemory")
    await app.Services.ApplyMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Card Transaction API v1"));
}
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

public partial class Program { }
