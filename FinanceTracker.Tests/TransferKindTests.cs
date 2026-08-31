using FinanceTracker;

namespace FinanceTracker.Tests;

public class TransferKindTests
{
    [Fact]
    public void Constructor_ValidValues_CreatesTransferKindAndTrimsName()
    {
        var kind = new TransferKind("  Food  ", TransferKindMode.Expense);

        Assert.Equal("Food", kind.Name);
        Assert.Equal(TransferKindMode.Expense, kind.TransferKindMode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_BlankName_ThrowsArgumentException(string invalidName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new TransferKind(invalidName, TransferKindMode.Expense));

        Assert.Equal("name", exception.ParamName);
    }
}
