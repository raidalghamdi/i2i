using InnovationToImpact.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Tests;

public sealed class SqliteContextFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public InnovationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new InnovationDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
