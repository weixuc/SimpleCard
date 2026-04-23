using FluentAssertions;
using SimpleCard.Application.Cards.Queries.GetCardBalance;
using SimpleCard.Application.Common.Exceptions;
using SimpleCard.Application.Tests.Helpers;
using SimpleCard.Domain.Entities;
using SimpleCard.Domain.Interfaces;
using Xunit;

namespace SimpleCard.Application.Tests.Cards.Queries;

public class GetCardBalanceQueryHandlerTests
{
    private static GetCardBalanceQueryHandler CreateHandler(
        SimpleCard.Infrastructure.Persistence.AppDbContext db,
        FakeExchangeRateService? fakeService = null)
        => new(db, fakeService ?? new FakeExchangeRateService());

    [Fact]
    public async Task Handle_UsdCurrency_ReturnsBalanceWithoutConversion()
    {
        using var db = InMemoryDbContextFactory.Create();
        var card = new Card { CreditLimit = 1000m };
        db.Cards.Add(card);
        db.Transactions.Add(new Transaction { CardId = card.Id, Description = "A", TransactionDate = new DateOnly(2024, 1, 1), Amount = 200m });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetCardBalanceQuery(card.Id, "USD"), CancellationToken.None);

        result.CardId.Should().Be(card.Id);
        result.CreditLimitUsd.Should().Be(1000m);
        result.TotalTransactionsUsd.Should().Be(200m);
        result.AvailableBalanceUsd.Should().Be(800m);
        result.Currency.Should().Be("USD");
        result.ExchangeRate.Should().Be(1m);
        result.ConvertedAvailableBalance.Should().Be(800m);
    }

    [Fact]
    public async Task Handle_UsdCurrency_CaseInsensitive()
    {
        using var db = InMemoryDbContextFactory.Create();
        var card = new Card { CreditLimit = 500m };
        db.Cards.Add(card);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetCardBalanceQuery(card.Id, "usd"), CancellationToken.None);

        result.Currency.Should().Be("USD");
        result.ExchangeRate.Should().Be(1m);
    }

    [Fact]
    public async Task Handle_ForeignCurrency_ReturnsConvertedBalance()
    {
        using var db = InMemoryDbContextFactory.Create();
        var card = new Card { CreditLimit = 1000m };
        db.Cards.Add(card);
        db.Transactions.Add(new Transaction { CardId = card.Id, Description = "B", TransactionDate = new DateOnly(2024, 1, 1), Amount = 300m });
        await db.SaveChangesAsync();

        var fake = new FakeExchangeRateService
        {
            GetLatestRateFunc = (currency) => new ExchangeRateResult(1.25m, new DateOnly(2024, 3, 31))
        };
        var handler = CreateHandler(db, fake);
        var result = await handler.Handle(new GetCardBalanceQuery(card.Id, "Canada-Dollar"), CancellationToken.None);

        result.Currency.Should().Be("Canada-Dollar");
        result.ExchangeRate.Should().Be(1.25m);
        result.AvailableBalanceUsd.Should().Be(700m);
        result.ConvertedAvailableBalance.Should().Be(875m);
    }

    [Fact]
    public async Task Handle_NoTransactions_BalanceEqualsCreditLimit()
    {
        using var db = InMemoryDbContextFactory.Create();
        var card = new Card { CreditLimit = 2000m };
        db.Cards.Add(card);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetCardBalanceQuery(card.Id, "USD"), CancellationToken.None);

        result.TotalTransactionsUsd.Should().Be(0m);
        result.AvailableBalanceUsd.Should().Be(2000m);
        result.ConvertedAvailableBalance.Should().Be(2000m);
    }

    [Fact]
    public async Task Handle_MultipleTransactions_SumsCorrectly()
    {
        using var db = InMemoryDbContextFactory.Create();
        var card = new Card { CreditLimit = 1000m };
        db.Cards.Add(card);
        db.Transactions.AddRange(
            new Transaction { CardId = card.Id, Description = "X", TransactionDate = new DateOnly(2024, 1, 1), Amount = 100m },
            new Transaction { CardId = card.Id, Description = "Y", TransactionDate = new DateOnly(2024, 1, 2), Amount = 250m },
            new Transaction { CardId = card.Id, Description = "Z", TransactionDate = new DateOnly(2024, 1, 3), Amount = 50m }
        );
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetCardBalanceQuery(card.Id, "USD"), CancellationToken.None);

        result.TotalTransactionsUsd.Should().Be(400m);
        result.AvailableBalanceUsd.Should().Be(600m);
    }

    [Fact]
    public async Task Handle_CardNotFound_ThrowsNotFoundException()
    {
        using var db = InMemoryDbContextFactory.Create();
        var handler = CreateHandler(db);

        var act = () => handler.Handle(new GetCardBalanceQuery(Guid.NewGuid(), "USD"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Card*not found*");
    }

    [Fact]
    public async Task Handle_ForeignCurrency_ExchangeRateUnavailable_ThrowsException()
    {
        using var db = InMemoryDbContextFactory.Create();
        var card = new Card { CreditLimit = 1000m };
        db.Cards.Add(card);
        await db.SaveChangesAsync();

        var fake = new FakeExchangeRateService
        {
            GetLatestRateFunc = (_) => null
        };
        var handler = CreateHandler(db, fake);

        var act = () => handler.Handle(new GetCardBalanceQuery(card.Id, "UnknownCurrency"), CancellationToken.None);

        await act.Should().ThrowAsync<ExchangeRateUnavailableException>()
            .WithMessage("*UnknownCurrency*");
    }

    [Fact]
    public async Task Handle_ForeignCurrency_RoundingToTwoDecimalPlaces()
    {
        using var db = InMemoryDbContextFactory.Create();
        var card = new Card { CreditLimit = 1000m };
        db.Cards.Add(card);
        db.Transactions.Add(new Transaction { CardId = card.Id, Description = "R", TransactionDate = new DateOnly(2024, 1, 1), Amount = 333.33m });
        await db.SaveChangesAsync();

        var fake = new FakeExchangeRateService
        {
            GetLatestRateFunc = (_) => new ExchangeRateResult(3m, new DateOnly(2024, 3, 31))
        };
        var handler = CreateHandler(db, fake);
        var result = await handler.Handle(new GetCardBalanceQuery(card.Id, "Japan-Yen"), CancellationToken.None);

        var expected = Math.Round((1000m - 333.33m) * 3m, 2);
        result.ConvertedAvailableBalance.Should().Be(expected);
    }
}
