using System.Text.Json.Serialization;

namespace SimpleCard.ExchangeRateClient.Models;

internal class TreasuryApiResponse
{
    [JsonPropertyName("data")]
    public List<ExchangeRateEntry> Data { get; set; } = [];
}

internal class ExchangeRateEntry
{
    [JsonPropertyName("exchange_rate")]
    public string ExchangeRate { get; set; } = string.Empty;

    [JsonPropertyName("record_date")]
    public string RecordDate { get; set; } = string.Empty;
}
