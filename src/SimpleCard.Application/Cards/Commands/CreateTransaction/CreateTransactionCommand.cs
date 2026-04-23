using MediatR;

namespace SimpleCard.Application.Cards.Commands.CreateTransaction;

public record CreateTransactionCommand(
    Guid CardId,
    string Description,
    DateOnly TransactionDate,
    decimal Amount) : IRequest<CreateTransactionResponse>;
