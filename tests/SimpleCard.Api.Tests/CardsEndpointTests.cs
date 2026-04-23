using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleCard.Api.Tests.Helpers;
using SimpleCard.Domain.Interfaces;
using Xunit;

namespace SimpleCard.Api.Tests;

public class CardsEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public CardsEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.FakeExchangeRateService.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_cards_CreatesCardAndReturns201()
    {
        var response = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 1500m });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CardDto>();
        body!.CreditLimit.Should().Be(1500m);
        body.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_cards_transactions_CreatesTransactionAndReturns201()
    {
        var cardResp = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 1000m });
        var card = await cardResp.Content.ReadFromJsonAsync<CardDto>();

        var response = await _client.PostAsJsonAsync(
            $"/api/cards/{card!.Id}/transactions",
            new { description = "Lunch", transactionDate = "2024-03-15", amount = 25.00m });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TransactionDto>();
        body!.CardId.Should().Be(card.Id);
        body.Description.Should().Be("Lunch");
        body.Amount.Should().Be(25.00m);
    }

    [Fact]
    public async Task POST_cards_transactions_UnknownCard_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/cards/{Guid.NewGuid()}/transactions",
            new { description = "Test", transactionDate = "2024-03-15", amount = 10m });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_cards_balance_UsdDefault_ReturnsBalance()
    {
        var cardResp = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 1000m });
        var card = await cardResp.Content.ReadFromJsonAsync<CardDto>();

        await _client.PostAsJsonAsync(
            $"/api/cards/{card!.Id}/transactions",
            new { description = "Shop", transactionDate = "2024-01-01", amount = 200m });

        var response = await _client.GetAsync($"/api/cards/{card.Id}/balance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BalanceDto>();
        body!.AvailableBalanceUsd.Should().Be(800m);
        body.Currency.Should().Be("USD");
        body.ExchangeRate.Should().Be(1m);
    }

    [Fact]
    public async Task GET_cards_balance_ForeignCurrency_ReturnsConvertedBalance()
    {
        _factory.FakeExchangeRateService.GetLatestRateFunc = _ => new ExchangeRateResult(1.25m, new DateOnly(2024, 3, 31));

        var cardResp = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 1000m });
        var card = await cardResp.Content.ReadFromJsonAsync<CardDto>();

        var response = await _client.GetAsync($"/api/cards/{card!.Id}/balance?currency=Canada-Dollar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BalanceDto>();
        body!.Currency.Should().Be("Canada-Dollar");
        body.ExchangeRate.Should().Be(1.25m);
        body.ConvertedAvailableBalance.Should().Be(1250m);
    }

    [Fact]
    public async Task GET_cards_balance_UnknownCard_Returns404()
    {
        var response = await _client.GetAsync($"/api/cards/{Guid.NewGuid()}/balance");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_cards_balance_ForeignCurrency_ExchangeRateUnavailable_Returns400()
    {
        _factory.FakeExchangeRateService.GetLatestRateFunc = _ => null;

        var cardResp = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 500m });
        var card = await cardResp.Content.ReadFromJsonAsync<CardDto>();

        var response = await _client.GetAsync($"/api/cards/{card!.Id}/balance?currency=UnknownCurrency");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_cards_NegativeCreditLimit_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = -100m });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_cards_ZeroCreditLimit_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 0m });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_cards_transactions_NegativeAmount_Returns400()
    {
        var cardResp = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 1000m });
        var card = await cardResp.Content.ReadFromJsonAsync<CardDto>();

        var response = await _client.PostAsJsonAsync(
            $"/api/cards/{card!.Id}/transactions",
            new { description = "Test", transactionDate = "2024-03-15", amount = -50m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_cards_transactions_EmptyDescription_Returns400()
    {
        var cardResp = await _client.PostAsJsonAsync("/api/cards", new { creditLimit = 1000m });
        var card = await cardResp.Content.ReadFromJsonAsync<CardDto>();

        var response = await _client.PostAsJsonAsync(
            $"/api/cards/{card!.Id}/transactions",
            new { description = "", transactionDate = "2024-03-15", amount = 25m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

file record CardDto(Guid Id, decimal CreditLimit);
file record TransactionDto(Guid Id, Guid CardId, string Description, DateOnly TransactionDate, decimal Amount);
file record BalanceDto(Guid CardId, decimal CreditLimitUsd, decimal TotalTransactionsUsd, decimal AvailableBalanceUsd, string Currency, decimal ExchangeRate, decimal ConvertedAvailableBalance);
