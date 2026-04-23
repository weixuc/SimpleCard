namespace SimpleCard.Application.Cards.Commands.CreateTransaction;

public record CreateTransactionResponse(
    Guid Id,
    Guid CardId,
    string Description,
    DateOnly TransactionDate,
    decimal Amount);
