using MediatR;

namespace SimpleCard.Application.Cards.Commands.CreateCard;

public record CreateCardCommand(decimal CreditLimit) : IRequest<CreateCardResponse>;
