using FluentAssertions;
using SimpleCard.Application.Cards.Commands.CreateTransaction;
using SimpleCard.Application.Common.Exceptions;
using SimpleCard.Application.Tests.Helpers;
using SimpleCard.Domain.Entities;
using Xunit;

namespace SimpleCard.Application.Tests.Cards.Commands;

public class CreateTransactionCommandHandlerTests
{
    private static readonly DateOnly TestDate = new(2024, 3, 15);

    [Fact]
    public async Task Handle_ValidCommand_CreatesTransactionAndReturnsResponse()
    {
        using var db = InMemoryDbContextFactory.Create();
        var card = new Card { CreditLimit = 1000m };
        db.Cards.Add(card);
        await db.SaveChangesAsync();

        var handler = new CreateTransactionCommandHandler(db);
        var command = new CreateTransactionCommand(card.Id, "Groceries", TestDate, 75.50m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.CardId.Should().Be(card.Id);
        result.Description.Should().Be("Groceries");
        result.TransactionDate.Should().Be(TestDate);
        result.Amount.Should().Be(75.50m);
        db.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_CardNotFound_ThrowsNotFoundException()
    {
        using var db = InMemoryDbContextFactory.Create();
        var handler = new CreateTransactionCommandHandler(db);
        var command = new CreateTransactionCommand(Guid.NewGuid(), "Test", TestDate, 100m);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Card*not found*");
    }
}
