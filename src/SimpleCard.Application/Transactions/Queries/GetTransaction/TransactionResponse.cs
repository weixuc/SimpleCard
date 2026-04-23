namespace SimpleCard.Application.Transactions.Queries.GetTransaction;

public record TransactionResponse(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal OriginalAmountUsd,
    string Currency,
    decimal ExchangeRate,
    decimal ConvertedAmount);
