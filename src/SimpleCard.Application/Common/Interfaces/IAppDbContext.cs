using Microsoft.EntityFrameworkCore;
using SimpleCard.Domain.Entities;

namespace SimpleCard.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Card> Cards { get; }
    DbSet<Transaction> Transactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
