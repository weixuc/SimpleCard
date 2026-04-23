using MediatR;

namespace SimpleCard.Application.Cards.Queries.GetCardBalance;

public record GetCardBalanceQuery(Guid CardId, string Currency) : IRequest<CardBalanceResponse>;
