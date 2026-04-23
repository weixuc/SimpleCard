namespace SimpleCard.Domain.Interfaces;

public interface IExchangeRateService
{
    Task<ExchangeRateResult?> GetRateForTransactionDateAsync(string currency, DateOnly transactionDate);
    Task<ExchangeRateResult?> GetLatestRateAsync(string currency);
}

public record ExchangeRateResult(decimal Rate, DateOnly RecordDate);
