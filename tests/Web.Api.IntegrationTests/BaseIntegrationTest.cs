using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Database;
using System.Text;
using System.Text.Json;

namespace Web.Api.IntegrationTests;

public abstract class BaseIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient HttpClient;
    protected readonly WebApplicationFactory<Program> Factory;

    protected BaseIntegrationTest(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        HttpClient = Factory.CreateClient();
    }

    protected static StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    protected static async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    protected async Task<ApplicationDbContext> GetDbContextAsync()
    {
        var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}