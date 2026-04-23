using Microsoft.Extensions.DependencyInjection;
using SimpleCard.Domain.Interfaces;

namespace SimpleCard.ExchangeRateClient;

public static class DependencyInjection
{
    public static IServiceCollection AddExchangeRateClient(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<IExchangeRateService, TreasuryExchangeRateService>(client =>
        {
            client.BaseAddress = new Uri("https://api.fiscaldata.treasury.gov/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddStandardResilienceHandler();
        return services;
    }
}
