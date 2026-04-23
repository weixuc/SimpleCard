using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleCard.Api.Tests.Helpers;
using SimpleCard.Domain.Interfaces;
using Xunit;

namespace SimpleCard.Api.Tests;

public class TransactionsEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public TransactionsEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.FakeExchangeRateService.Reset();
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateTransactionAsync(decimal amount = 50m)
    {
        var cardResp = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 1000m });
        var card = await cardResp.Content.ReadFromJsonAsync<TxCardDto>();

        var txResp = await _client.PostAsJsonAsync(
            $"/api/cards/{card!.Id}/transactions",
            new { description = "Purchase", transactionDate = "2024-03-15", amount });

        var tx = await txResp.Content.ReadFromJsonAsync<TxDto>();
        return tx!.Id;
    }

    [Fact]
    public async Task GET_transactions_UsdDefault_ReturnsTransaction()
    {
        var txId = await CreateTransactionAsync(75.50m);

        var response = await _client.GetAsync($"/api/transactions/{txId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TxResponseDto>();
        body!.Id.Should().Be(txId);
        body.OriginalAmountUsd.Should().Be(75.50m);
        body.Currency.Should().Be("USD");
        body.ExchangeRate.Should().Be(1m);
        body.ConvertedAmount.Should().Be(75.50m);
    }

    [Fact]
    public async Task GET_transactions_ForeignCurrency_ReturnsConvertedAmount()
    {
        _factory.FakeExchangeRateService.GetRateForTransactionDateFunc = (_, _) =>
            new ExchangeRateResult(1.35m, new DateOnly(2024, 3, 31));

        var txId = await CreateTransactionAsync(100m);

        var response = await _client.GetAsync($"/api/transactions/{txId}?currency=Canada-Dollar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TxResponseDto>();
        body!.Currency.Should().Be("Canada-Dollar");
        body.ExchangeRate.Should().Be(1.35m);
        body.ConvertedAmount.Should().Be(135m);
    }

    [Fact]
    public async Task GET_transactions_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/transactions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_transactions_ForeignCurrency_ExchangeRateUnavailable_Returns400()
    {
        _factory.FakeExchangeRateService.GetRateForTransactionDateFunc = (_, _) => null;

        var txId = await CreateTransactionAsync();

        var response = await _client.GetAsync($"/api/transactions/{txId}?currency=UnknownCurrency");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

file record TxCardDto(Guid Id, decimal CreditLimit);
file record TxDto(Guid Id, Guid CardId, string Description, DateOnly TransactionDate, decimal Amount);
file record TxResponseDto(Guid Id, string Description, DateOnly TransactionDate, decimal OriginalAmountUsd, string Currency, decimal ExchangeRate, decimal ConvertedAmount);
