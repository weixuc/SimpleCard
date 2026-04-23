using MediatR;

namespace SimpleCard.Application.Transactions.Queries.GetTransaction;

public record GetTransactionQuery(Guid TransactionId, string Currency) : IRequest<TransactionResponse>;
