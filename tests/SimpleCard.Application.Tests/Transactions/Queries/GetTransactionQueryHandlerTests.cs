using FluentAssertions;
using SimpleCard.Application.Common.Exceptions;
using SimpleCard.Application.Tests.Helpers;
using SimpleCard.Application.Transactions.Queries.GetTransaction;
using SimpleCard.Domain.Entities;
using SimpleCard.Domain.Interfaces;
using Xunit;

namespace SimpleCard.Application.Tests.Transactions.Queries;

public class GetTransactionQueryHandlerTests
{
    private static readonly DateOnly TestDate = new(2024, 3, 15);

    private static GetTransactionQueryHandler CreateHandler(
        SimpleCard.Infrastructure.Persistence.AppDbContext db,
        FakeExchangeRateService? fakeService = null)
        => new(db, fakeService ?? new FakeExchangeRateService());

    private static async Task<Transaction> SeedTransactionAsync(
        SimpleCard.Infrastructure.Persistence.AppDbContext db,
        decimal amount = 100m)
    {
        var card = new Card { CreditLimit = 1000m };
        db.Cards.Add(card);
        var tx = new Transaction
        {
            CardId = card.Id,
            Description = "Test Purchase",
            TransactionDate = TestDate,
            Amount = amount
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    [Fact]
    public async Task Handle_UsdCurrency_ReturnsTransactionWithoutConversion()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tx = await SeedTransactionAsync(db, 75.50m);

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetTransactionQuery(tx.Id, "USD"), CancellationToken.None);

        result.Id.Should().Be(tx.Id);
        result.Description.Should().Be("Test Purchase");
        result.TransactionDate.Should().Be(TestDate);
        result.OriginalAmountUsd.Should().Be(75.50m);
        result.Currency.Should().Be("USD");
        result.ExchangeRate.Should().Be(1m);
        result.ConvertedAmount.Should().Be(75.50m);
    }

    [Fact]
    public async Task Handle_UsdCurrency_CaseInsensitive()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tx = await SeedTransactionAsync(db);

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetTransactionQuery(tx.Id, "usd"), CancellationToken.None);

        result.Currency.Should().Be("USD");
        result.ExchangeRate.Should().Be(1m);
    }

    [Fact]
    public async Task Handle_ForeignCurrency_ReturnsConvertedAmount()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tx = await SeedTransactionAsync(db, 100m);

        var fake = new FakeExchangeRateService
        {
            GetRateForTransactionDateFunc = (currency, date) => new ExchangeRateResult(1.35m, new DateOnly(2024, 3, 31))
        };
        var handler = CreateHandler(db, fake);
        var result = await handler.Handle(new GetTransactionQuery(tx.Id, "Canada-Dollar"), CancellationToken.None);

        result.Currency.Should().Be("Canada-Dollar");
        result.ExchangeRate.Should().Be(1.35m);
        result.OriginalAmountUsd.Should().Be(100m);
        result.ConvertedAmount.Should().Be(135m);
    }

    [Fact]
    public async Task Handle_ForeignCurrency_RoundingToTwoDecimalPlaces()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tx = await SeedTransactionAsync(db, 10m);

        var fake = new FakeExchangeRateService
        {
            GetRateForTransactionDateFunc = (_, _) => new ExchangeRateResult(1.3333m, new DateOnly(2024, 3, 31))
        };
        var handler = CreateHandler(db, fake);
        var result = await handler.Handle(new GetTransactionQuery(tx.Id, "Japan-Yen"), CancellationToken.None);

        result.ConvertedAmount.Should().Be(Math.Round(10m * 1.3333m, 2));
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsNotFoundException()
    {
        using var db = InMemoryDbContextFactory.Create();
        var handler = CreateHandler(db);

        var act = () => handler.Handle(new GetTransactionQuery(Guid.NewGuid(), "USD"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Transaction*not found*");
    }

    [Fact]
    public async Task Handle_ForeignCurrency_ExchangeRateUnavailable_ThrowsException()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tx = await SeedTransactionAsync(db);

        var fake = new FakeExchangeRateService
        {
            GetRateForTransactionDateFunc = (_, _) => null
        };
        var handler = CreateHandler(db, fake);

        var act = () => handler.Handle(new GetTransactionQuery(tx.Id, "UnknownCurrency"), CancellationToken.None);

        await act.Should().ThrowAsync<ExchangeRateUnavailableException>()
            .WithMessage("*UnknownCurrency*");
    }

    [Fact]
    public async Task Handle_ForeignCurrency_PassesCorrectDateToExchangeRateService()
    {
        using var db = InMemoryDbContextFactory.Create();
        var tx = await SeedTransactionAsync(db);

        DateOnly? capturedDate = null;
        string? capturedCurrency = null;
        var fake = new FakeExchangeRateService
        {
            GetRateForTransactionDateFunc = (currency, date) =>
            {
                capturedCurrency = currency;
                capturedDate = date;
                return new ExchangeRateResult(1m, date);
            }
        };
        var handler = CreateHandler(db, fake);
        await handler.Handle(new GetTransactionQuery(tx.Id, "Euro Zone-Euro"), CancellationToken.None);

        capturedCurrency.Should().Be("Euro Zone-Euro");
        capturedDate.Should().Be(TestDate);
    }
}
