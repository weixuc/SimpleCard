using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleCard.Application.Common.Interfaces;
using SimpleCard.Infrastructure.Persistence;

namespace SimpleCard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbProvider = configuration["DatabaseProvider"] ?? "Npgsql";

        if (dbProvider == "InMemory")
        {
            var dbName = configuration["InMemoryDbName"] ?? "SimpleCardDb";
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedMigrationHistoryIfNeededAsync(db);
        await db.Database.MigrateAsync();
    }

    // Handles databases that were originally created via EnsureCreated() before migrations
    // were introduced. If the schema already exists but __EFMigrationsHistory does not,
    // we create the history table and mark all known migrations as applied so that
    // MigrateAsync() skips their DDL instead of failing with "relation already exists".
    private static async Task SeedMigrationHistoryIfNeededAsync(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = """
                SELECT EXISTS (
                    SELECT FROM information_schema.tables WHERE table_name = 'Cards'
                ) AND NOT EXISTS (
                    SELECT FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'
                )
                """;
            var needsSeed = (bool)(await checkCmd.ExecuteScalarAsync())!;
            if (!needsSeed) return;

            var efVersion = typeof(DbContext).Assembly.GetName().Version!;
            var productVersion = $"{efVersion.Major}.{efVersion.Minor}.{efVersion.Build}";
            var values = string.Join(",\n    ",
                db.Database.GetMigrations().Select(m => $"('{m}', '{productVersion}')"));

            using var seedCmd = conn.CreateCommand();
            seedCmd.CommandText = $"""
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
                    {values};
                """;
            await seedCmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await conn.CloseAsync();
        }
    }
}
