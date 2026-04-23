namespace SimpleCard.Application.Cards.Queries.GetCardBalance;

public record CardBalanceResponse(
    Guid CardId,
    decimal CreditLimitUsd,
    decimal TotalTransactionsUsd,
    decimal AvailableBalanceUsd,
    string Currency,
    decimal ExchangeRate,
    decimal ConvertedAvailableBalance);
