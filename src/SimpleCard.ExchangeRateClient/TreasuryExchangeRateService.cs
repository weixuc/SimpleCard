using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SimpleCard.Domain.Interfaces;
using SimpleCard.ExchangeRateClient.Models;

namespace SimpleCard.ExchangeRateClient;

public class TreasuryExchangeRateService(HttpClient httpClient, IMemoryCache cache) : IExchangeRateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<ExchangeRateResult?> GetRateForTransactionDateAsync(
        string currency, DateOnly transactionDate)
    {
        var cacheKey = $"tx:{currency}:{transactionDate:yyyy-MM-dd}";
        if (cache.TryGetValue(cacheKey, out ExchangeRateResult? cached))
            return cached;

        var sixMonthsBefore = transactionDate.AddMonths(-6);
        var filter = $"country_currency_desc:eq:{currency}," +
                     $"record_date:lte:{transactionDate:yyyy-MM-dd}," +
                     $"record_date:gte:{sixMonthsBefore:yyyy-MM-dd}";
        var result = await FetchMostRecentRateAsync(filter);
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    public async Task<ExchangeRateResult?> GetLatestRateAsync(string currency)
    {
        var cacheKey = $"latest:{currency}";
        if (cache.TryGetValue(cacheKey, out ExchangeRateResult? cached))
            return cached;

        var filter = $"country_currency_desc:eq:{currency}";
        var result = await FetchMostRecentRateAsync(filter);
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    private async Task<ExchangeRateResult?> FetchMostRecentRateAsync(string filter)
    {
        var url = "services/api/v1/accounting/od/rates_of_exchange" +
                  $"?fields=exchange_rate,record_date" +
                  $"&filter={Uri.EscapeDataString(filter)}" +
                  $"&sort=-record_date" +
                  $"&page[size]=1";

        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<TreasuryApiResponse>(content, JsonOptions);

        if (apiResponse?.Data is not { Count: > 0 } data) return null;

        var item = data[0];
        var rate = decimal.Parse(item.ExchangeRate);
        var recordDate = DateOnly.Parse(item.RecordDate);

        return new ExchangeRateResult(rate, recordDate);
    }
}
