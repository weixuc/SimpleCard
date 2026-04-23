using MediatR;
using SimpleCard.Application.Common.Interfaces;
using SimpleCard.Domain.Entities;

namespace SimpleCard.Application.Cards.Commands.CreateCard;

public class CreateCardCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateCardCommand, CreateCardResponse>
{
    public async Task<CreateCardResponse> Handle(
        CreateCardCommand request,
        CancellationToken cancellationToken)
    {
        var card = new Card { CreditLimit = request.CreditLimit };
        db.Cards.Add(card);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateCardResponse(card.Id, card.CreditLimit);
    }
}
