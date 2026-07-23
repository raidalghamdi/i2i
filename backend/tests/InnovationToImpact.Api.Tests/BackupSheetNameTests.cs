using InnovationToImpact.Infrastructure.Backup;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class BackupSheetNameTests
{
    [Fact]
    public void ShortNameWithNoIllegalChars_IsReturnedUnchanged()
    {
        var usedNames = new HashSet<string>();

        var result = BackupExportService.SafeSheetName("Users", usedNames);

        Assert.Equal("Users", result);
    }

    [Theory]
    [InlineData('/')]
    [InlineData('\\')]
    [InlineData('*')]
    [InlineData('?')]
    [InlineData(':')]
    [InlineData('[')]
    [InlineData(']')]
    public void IllegalChar_IsReplacedWithUnderscore(char illegalChar)
    {
        var usedNames = new HashSet<string>();
        var raw = $"Foo{illegalChar}Bar";

        var result = BackupExportService.SafeSheetName(raw, usedNames);

        Assert.Equal("Foo_Bar", result);
        Assert.DoesNotContain(illegalChar, result);
    }

    [Fact]
    public void NameLongerThan31Chars_IsTruncatedToExactly31Chars()
    {
        var usedNames = new HashSet<string>();
        var raw = new string('A', 40);

        var result = BackupExportService.SafeSheetName(raw, usedNames);

        Assert.Equal(31, result.Length);
        Assert.Equal(new string('A', 31), result);
    }

    [Fact]
    public void TruncationCollision_ProducesTwoDistinctNamesWithinLengthLimit()
    {
        var usedNames = new HashSet<string>();
        var sharedPrefix = new string('A', 31);
        var nameA = sharedPrefix + new string('1', 9); // 40 chars total
        var nameB = sharedPrefix + new string('2', 9); // 40 chars total, same first 31 chars as nameA

        var resultA = BackupExportService.SafeSheetName(nameA, usedNames);
        var resultB = BackupExportService.SafeSheetName(nameB, usedNames);

        Assert.NotEqual(resultA, resultB);
        Assert.True(resultA.Length <= 31);
        Assert.True(resultB.Length <= 31);
    }

    [Theory]
    [InlineData("///")]
    [InlineData("")]
    [InlineData("[]:*?")]
    public void OnlyIllegalCharsOrEmpty_YieldsNonEmptyNameWithinLengthLimit(string raw)
    {
        var usedNames = new HashSet<string>();

        var result = BackupExportService.SafeSheetName(raw, usedNames);

        Assert.NotEmpty(result);
        Assert.True(result.Length <= 31);
    }
}
