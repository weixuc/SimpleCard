using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SimpleCard.ExchangeRateClient;
using Xunit;

namespace SimpleCard.ExchangeRateClient.Tests;

public class TreasuryExchangeRateServiceTests
{
    private static TreasuryExchangeRateService CreateService(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.fiscaldata.treasury.gov/")
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new TreasuryExchangeRateService(client, cache);
    }

    private static HttpResponseMessage OkJson(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private const string SingleRateJson = """
        {
          "data": [
            { "exchange_rate": "1.2500", "record_date": "2024-03-31" }
          ]
        }
        """;

    private const string EmptyDataJson = """{ "data": [] }""";

    [Fact]
    public async Task GetRateForTransactionDateAsync_ValidResponse_ReturnsRate()
    {
        var service = CreateService(OkJson(SingleRateJson));

        var result = await service.GetRateForTransactionDateAsync("Canada-Dollar", new DateOnly(2024, 3, 15));

        result.Should().NotBeNull();
        result!.Rate.Should().Be(1.25m);
        result.RecordDate.Should().Be(new DateOnly(2024, 3, 31));
    }

    [Fact]
    public async Task GetRateForTransactionDateAsync_EmptyData_ReturnsNull()
    {
        var service = CreateService(OkJson(EmptyDataJson));

        var result = await service.GetRateForTransactionDateAsync("Canada-Dollar", new DateOnly(2024, 3, 15));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRateForTransactionDateAsync_HttpError_ReturnsNull()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await service.GetRateForTransactionDateAsync("Canada-Dollar", new DateOnly(2024, 3, 15));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestRateAsync_ValidResponse_ReturnsRate()
    {
        var service = CreateService(OkJson(SingleRateJson));

        var result = await service.GetLatestRateAsync("Canada-Dollar");

        result.Should().NotBeNull();
        result!.Rate.Should().Be(1.25m);
        result.RecordDate.Should().Be(new DateOnly(2024, 3, 31));
    }

    [Fact]
    public async Task GetLatestRateAsync_EmptyData_ReturnsNull()
    {
        var service = CreateService(OkJson(EmptyDataJson));

        var result = await service.GetLatestRateAsync("Canada-Dollar");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestRateAsync_HttpError_ReturnsNull()
    {
        var service = CreateService(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await service.GetLatestRateAsync("Canada-Dollar");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRateForTransactionDateAsync_IncludesSixMonthDateRangeInRequest()
    {
        Uri? capturedUri = null;
        var handler = new CapturingHttpMessageHandler(OkJson(SingleRateJson), u => capturedUri = u);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.fiscaldata.treasury.gov/")
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TreasuryExchangeRateService(client, cache);
        var txDate = new DateOnly(2024, 3, 15);

        await service.GetRateForTransactionDateAsync("Canada-Dollar", txDate);

        capturedUri!.Query.Should().Contain("2024-03-15");
        capturedUri.Query.Should().Contain("2023-09-15");
    }
}

file sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(response);
}

file sealed class CapturingHttpMessageHandler(HttpResponseMessage response, Action<Uri> capture) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        capture(request.RequestUri!);
        return Task.FromResult(response);
    }
}
