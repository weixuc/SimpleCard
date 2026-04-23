namespace SimpleCard.Application.Common.Exceptions;

public class ExchangeRateUnavailableException(string currency, string reason)
    : Exception($"No exchange rate available for currency '{currency}': {reason}");
