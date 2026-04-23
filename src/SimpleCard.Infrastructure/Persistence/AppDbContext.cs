using Microsoft.EntityFrameworkCore;
using SimpleCard.Application.Common.Interfaces;
using SimpleCard.Domain.Entities;
using SimpleCard.Infrastructure.Persistence.Configurations;

namespace SimpleCard.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CardConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
    }
}
