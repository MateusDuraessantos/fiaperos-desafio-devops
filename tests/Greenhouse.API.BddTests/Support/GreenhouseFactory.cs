using Greenhouse.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Greenhouse.API.BddTests.Support;

// Each scenario owns a relational database. JWT, controllers, EF and middleware remain real.
public sealed class GreenhouseFactory : WebApplicationFactory<Program>
{
    public const string Username = "bdd-admin";
    public const string Password = "BddOnly@2026!";
    private readonly SqliteConnection? sqlite;
    private readonly string? sqlConnection;
    private int cleanupStarted;
    public string Provider => sqlConnection is null ? "SQLite" : "SQL Server";

    public GreenhouseFactory()
    {
        var configured = Environment.GetEnvironmentVariable("BDD_SQLSERVER_CONNECTION");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var builder = new SqlConnectionStringBuilder(configured)
            {
                InitialCatalog = "GreenhouseBdd_" + Guid.NewGuid().ToString("N"),
                Pooling = false
            };
            sqlConnection = builder.ConnectionString;
        }
        else
        {
            sqlite = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
            sqlite.Open();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // UseSetting is visible even to the validations before builder.Build() in Program.
        foreach (var (key, value) in new Dictionary<string, string>
        {
            ["JwtSettings:SecretKey"] = "BddOnly_SigningKey_NotForDeployment_2026!",
            ["JwtSettings:Issuer"] = "Greenhouse.Bdd",
            ["JwtSettings:Audience"] = "Greenhouse.Bdd.Client",
            ["JwtSettings:ExpirationHours"] = "1",
            ["AdminCredentials:Username"] = Username,
            ["AdminCredentials:Password"] = Password,
            ["Logging:LogLevel:Default"] = "Warning"
        }) builder.UseSetting(key, value);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                if (sqlConnection is null) options.UseSqlite(sqlite!);
                else options.UseSqlServer(sqlConnection);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing && sqlConnection is not null && Interlocked.Exchange(ref cleanupStarted, 1) == 0)
            {
                using var scope = Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeleted();
            }
        }
        finally
        {
            base.Dispose(disposing);
            if (disposing) sqlite?.Dispose();
        }
    }
}
