using System.Collections;
using System.Globalization;
using ClosedXML.Excel;
using InnovationToImpact.Domain.Backup;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Backup;

public class BackupExportService : IBackupExportService
{
    private static readonly char[] IllegalSheetChars = { '\\', '/', '*', '?', ':', '[', ']' };

    private readonly InnovationDbContext _db;

    public BackupExportService(InnovationDbContext db) => _db = db;

    public Task<BackupExportResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var setMethod = typeof(DbContext).GetMethods()
            .First(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0);

        var sheetCount = 0;
        var totalRows = 0;

        foreach (var entityType in _db.Model.GetEntityTypes()
                     .Where(e => !e.IsOwned())
                     .OrderBy(e => e.GetTableName() ?? e.ClrType.Name, StringComparer.Ordinal))
        {
            var clrType = entityType.ClrType;

            var scalarProps = entityType.GetProperties()
                .Where(p => p.PropertyInfo is not null)
                .ToList();
            if (scalarProps.Count == 0)
            {
                continue;
            }

            var query = (IEnumerable)setMethod.MakeGenericMethod(clrType).Invoke(_db, null)!;

            var sheetName = SafeSheetName(entityType.GetTableName() ?? clrType.Name, usedNames);
            var sheet = workbook.Worksheets.Add(sheetName);

            for (var c = 0; c < scalarProps.Count; c++)
            {
                sheet.Cell(1, c + 1).Value = scalarProps[c].Name;
            }

            var rowIndex = 2;
            foreach (var entity in query)
            {
                for (var c = 0; c < scalarProps.Count; c++)
                {
                    var value = scalarProps[c].PropertyInfo!.GetValue(entity);
                    var cell = sheet.Cell(rowIndex, c + 1);
                    switch (value)
                    {
                        case null:
                            break;
                        case bool b:
                            cell.Value = b;
                            break;
                        case DateTime dt:
                            cell.Value = dt;
                            break;
                        case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                            cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                            break;
                        default:
                            cell.Value = value.ToString();
                            break;
                    }
                }

                rowIndex++;
                totalRows++;
            }

            sheetCount++;
        }

        _db.ChangeTracker.Clear();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(new BackupExportResult(stream.ToArray(), sheetCount, totalRows));
    }

    internal static string SafeSheetName(string raw, HashSet<string> usedNames)
    {
        var cleaned = new string(raw.Select(ch => IllegalSheetChars.Contains(ch) ? '_' : ch).ToArray());
        if (cleaned.Length == 0)
        {
            cleaned = "Sheet";
        }
        if (cleaned.Length > 31)
        {
            cleaned = cleaned[..31];
        }

        var candidate = cleaned;
        var suffix = 1;
        while (!usedNames.Add(candidate))
        {
            var tail = $"_{suffix++}";
            candidate = cleaned.Length + tail.Length > 31
                ? cleaned[..(31 - tail.Length)] + tail
                : cleaned + tail;
        }

        return candidate;
    }
}
