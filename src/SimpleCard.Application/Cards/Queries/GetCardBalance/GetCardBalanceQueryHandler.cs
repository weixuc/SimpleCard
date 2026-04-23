using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleCard.Application.Common.Exceptions;
using SimpleCard.Application.Common.Interfaces;
using SimpleCard.Domain.Entities;
using SimpleCard.Domain.Interfaces;

namespace SimpleCard.Application.Cards.Queries.GetCardBalance;

public class GetCardBalanceQueryHandler(IAppDbContext db, IExchangeRateService exchangeRateService)
    : IRequestHandler<GetCardBalanceQuery, CardBalanceResponse>
{
    public async Task<CardBalanceResponse> Handle(
        GetCardBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var card = await db.Cards.FindAsync([request.CardId], cancellationToken)
            ?? throw new NotFoundException(nameof(Card), request.CardId);

        var totalSpent = await db.Transactions
            .Where(t => t.CardId == card.Id)
            .SumAsync(t => t.Amount, cancellationToken);
        var availableBalance = card.CreditLimit - totalSpent;

        if (request.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return new CardBalanceResponse(
                card.Id, card.CreditLimit, totalSpent,
                availableBalance, "USD", 1m, availableBalance);
        }

        var rateResult = await exchangeRateService.GetLatestRateAsync(request.Currency)
            ?? throw new ExchangeRateUnavailableException(
                request.Currency,
                "no rate data found for the requested currency");

        var convertedBalance = Math.Round(availableBalance * rateResult.Rate, 2);

        return new CardBalanceResponse(
            card.Id, card.CreditLimit, totalSpent,
            availableBalance, request.Currency, rateResult.Rate, convertedBalance);
    }
}
