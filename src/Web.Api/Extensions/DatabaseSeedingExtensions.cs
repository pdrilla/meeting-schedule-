using Infrastructure.Database;

namespace Web.Api.Extensions;

public static class DatabaseSeedingExtensions
{
    public static async Task<WebApplication> SeedDatabaseAsync(this WebApplication app)
    {
        await DatabaseSeeder.SeedAsync(app.Services);
        return app;
    }
}