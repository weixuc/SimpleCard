using Microsoft.EntityFrameworkCore;
using SimpleCard.Infrastructure.Persistence;

namespace SimpleCard.Application.Tests.Helpers;

public static class InMemoryDbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
