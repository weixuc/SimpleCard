using MediatR;
using SimpleCard.Application.Common.Exceptions;
using SimpleCard.Application.Common.Interfaces;
using SimpleCard.Domain.Entities;
using SimpleCard.Domain.Interfaces;

namespace SimpleCard.Application.Transactions.Queries.GetTransaction;

public class GetTransactionQueryHandler(IAppDbContext db, IExchangeRateService exchangeRateService)
    : IRequestHandler<GetTransactionQuery, TransactionResponse>
{
    public async Task<TransactionResponse> Handle(
        GetTransactionQuery request,
        CancellationToken cancellationToken)
    {
        var transaction = await db.Transactions.FindAsync([request.TransactionId], cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.TransactionId);

        if (request.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return new TransactionResponse(
                transaction.Id, transaction.Description, transaction.TransactionDate,
                transaction.Amount, "USD", 1m, transaction.Amount);
        }

        var rateResult = await exchangeRateService.GetRateForTransactionDateAsync(
            request.Currency, transaction.TransactionDate)
            ?? throw new ExchangeRateUnavailableException(
                request.Currency,
                $"no rate found within 6 months on or before {transaction.TransactionDate:yyyy-MM-dd}. " +
                $"The transaction cannot be converted to the target currency.");

        var convertedAmount = Math.Round(transaction.Amount * rateResult.Rate, 2);

        return new TransactionResponse(
            transaction.Id, transaction.Description, transaction.TransactionDate,
            transaction.Amount, request.Currency, rateResult.Rate, convertedAmount);
    }
}
