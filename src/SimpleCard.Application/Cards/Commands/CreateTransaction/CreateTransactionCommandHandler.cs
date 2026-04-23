using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleCard.Application.Common.Exceptions;
using SimpleCard.Application.Common.Interfaces;
using SimpleCard.Domain.Entities;

namespace SimpleCard.Application.Cards.Commands.CreateTransaction;

public class CreateTransactionCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateTransactionCommand, CreateTransactionResponse>
{
    public async Task<CreateTransactionResponse> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var card = await db.Cards.FindAsync([request.CardId], cancellationToken)
            ?? throw new NotFoundException(nameof(Card), request.CardId);

        var transaction = new Transaction
        {
            CardId = card.Id,
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            Amount = request.Amount
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateTransactionResponse(
            transaction.Id,
            transaction.CardId,
            transaction.Description,
            transaction.TransactionDate,
            transaction.Amount);
    }
}
