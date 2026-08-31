using FinanceTracker;

namespace FinanceTracker.Tests;

public class TransferTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveAmount_ThrowsArgumentOutOfRangeException(decimal invalidAmount)
    {
        var account = CreateAccount();
        var kind = new TransferKind("General", TransferKindMode.IncomeAndExpense);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Transfer(
                invalidAmount,
                null,
                DateTime.Today,
                TransferMode.Expense,
                false,
                account,
                kind));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_AmountWithMoreThanTwoDecimalPlaces_ThrowsArgumentException()
    {
        var account = CreateAccount();
        var kind = new TransferKind("General", TransferKindMode.IncomeAndExpense);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Transfer(
                10.001m,
                null,
                DateTime.Today,
                TransferMode.Expense,
                false,
                account,
                kind));

        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData(TransferMode.Income, TransferKindMode.Expense)]
    [InlineData(TransferMode.Expense, TransferKindMode.Income)]
    public void Constructor_IncompatibleKindAndMode_ThrowsArgumentException(
        TransferMode transferMode,
        TransferKindMode kindMode)
    {
        var account = CreateAccount();
        var kind = new TransferKind("Incompatible kind", kindMode);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Transfer(
                10m,
                null,
                DateTime.Today,
                transferMode,
                false,
                account,
                kind));

        Assert.Equal("transferMode", exception.ParamName);
    }

    [Fact]
    public void Status_ReflectsCompletionAndEffectiveDate()
    {
        var account = CreateAccount();
        var kind = new TransferKind("General", TransferKindMode.IncomeAndExpense);

        var completed = CreateTransfer(account, kind, DateTime.Today.AddDays(-1), true);
        var overdue = CreateTransfer(account, kind, DateTime.Today.AddDays(-1), false);
        var scheduled = CreateTransfer(account, kind, DateTime.Today.AddDays(1), false);

        Assert.Equal(TransferStatus.Completed, completed.Status);
        Assert.Equal(TransferStatus.Overdue, overdue.Status);
        Assert.Equal(TransferStatus.Scheduled, scheduled.Status);
    }

    [Fact]
    public void MarkAsCompleted_PastTransfer_ChangesStatusToCompleted()
    {
        var account = CreateAccount();
        var kind = new TransferKind("General", TransferKindMode.IncomeAndExpense);
        var transfer = CreateTransfer(account, kind, DateTime.Today.AddDays(-1), false);

        transfer.MarkAsCompleted();

        Assert.True(transfer.IsCompleted);
        Assert.Equal(TransferStatus.Completed, transfer.Status);
    }

    [Fact]
    public void MarkAsCompleted_FutureTransfer_ThrowsInvalidOperationException()
    {
        var account = CreateAccount();
        var kind = new TransferKind("General", TransferKindMode.IncomeAndExpense);
        var transfer = CreateTransfer(account, kind, DateTime.Today.AddDays(1), false);

        Assert.Throws<InvalidOperationException>(() => transfer.MarkAsCompleted());
        Assert.False(transfer.IsCompleted);
    }

    private static Account CreateAccount()
    {
        return new Account("Checking", AccountType.CheckingAccount, 0m);
    }

    private static Transfer CreateTransfer(
        Account account,
        TransferKind kind,
        DateTime effectiveAt,
        bool isCompleted)
    {
        return new Transfer(
            10m,
            null,
            effectiveAt,
            TransferMode.Expense,
            isCompleted,
            account,
            kind);
    }
}
