using ClosedXML.Excel;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Backup;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class BackupExportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly InnovationDbContext _db;

    public BackupExportServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<InnovationDbContext>().UseSqlite(_connection).Options;
        _db = new InnovationDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task GenerateAsync_produces_a_users_sheet_containing_seeded_user_data()
    {
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            SamAccountName = "admin1",
            Email = "admin1@gac-demo.sa",
            FullNameAr = "المدير",
            FullNameEn = "Admin One",
        });
        await _db.SaveChangesAsync();

        var service = new BackupExportService(_db);
        var result = await service.GenerateAsync();

        Assert.True(result.SheetCount > 0);
        Assert.True(result.RowCount > 0);

        using var workbook = new XLWorkbook(new MemoryStream(result.Content));
        Assert.Contains(workbook.Worksheets, w => w.Name == "Users");
        var usersSheet = workbook.Worksheet("Users");
        Assert.Contains(usersSheet.CellsUsed(), c => c.GetString() == "admin1@gac-demo.sa");
    }

    [Fact]
    public async Task GenerateAsync_never_throws_on_the_full_seeded_model()
    {
        var service = new BackupExportService(_db);
        var result = await service.GenerateAsync();
        // EnsureCreated applies HasData seed rows (roles, statuses, etc.), so many sheets have content.
        Assert.True(result.SheetCount >= 10);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
