using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SimpleCard.Domain.Interfaces;

namespace SimpleCard.Api.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeExchangeRateService FakeExchangeRateService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DatabaseProvider", "InMemory");
        builder.UseSetting("InMemoryDbName", Guid.NewGuid().ToString());

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(IExchangeRateService))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddSingleton<IExchangeRateService>(FakeExchangeRateService);
        });
    }
}
