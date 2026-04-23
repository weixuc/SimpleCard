using SimpleCard.Domain.Interfaces;

namespace SimpleCard.Application.Tests.Helpers;

public class FakeExchangeRateService : IExchangeRateService
{
    public Func<string, DateOnly, ExchangeRateResult?>? GetRateForTransactionDateFunc { get; set; }
    public Func<string, ExchangeRateResult?>? GetLatestRateFunc { get; set; }

    public Task<ExchangeRateResult?> GetRateForTransactionDateAsync(string currency, DateOnly transactionDate)
        => Task.FromResult(GetRateForTransactionDateFunc?.Invoke(currency, transactionDate));

    public Task<ExchangeRateResult?> GetLatestRateAsync(string currency)
        => Task.FromResult(GetLatestRateFunc?.Invoke(currency));
}
