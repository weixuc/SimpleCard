using FluentAssertions;
using SimpleCard.Application.Cards.Commands.CreateCard;
using SimpleCard.Application.Tests.Helpers;
using Xunit;

namespace SimpleCard.Application.Tests.Cards.Commands;

public class CreateCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesCardAndReturnsResponse()
    {
        using var db = InMemoryDbContextFactory.Create();
        var handler = new CreateCardCommandHandler(db);

        var result = await handler.Handle(new CreateCardCommand(1500m), CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.CreditLimit.Should().Be(1500m);
        db.Cards.Should().HaveCount(1);
        db.Cards.First().CreditLimit.Should().Be(1500m);
    }
}
