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

    [Fact]
    public void UpdateDetails_ValidValues_UpdatesEditableProperties()
    {
        var account = CreateAccount();
        var originalKind = new TransferKind("General", TransferKindMode.IncomeAndExpense);
        var updatedKind = new TransferKind("Salary", TransferKindMode.Income);
        var transfer = CreateTransfer(account, originalKind, DateTime.Today, false);

        transfer.UpdateDetails(
            250.75m,
            "September salary",
            DateTime.Today,
            TransferMode.Income,
            true,
            updatedKind);

        Assert.Equal(250.75m, transfer.Amount);
        Assert.Equal("September salary", transfer.Description);
        Assert.Equal(DateTime.Today, transfer.EffectiveAt);
        Assert.Equal(TransferMode.Income, transfer.TransferMode);
        Assert.True(transfer.IsCompleted);
        Assert.Same(updatedKind, transfer.TransferKind);
    }

    [Fact]
    public void UpdateDetails_IncompatibleKind_ThrowsAndPreservesOriginalValues()
    {
        var account = CreateAccount();
        var originalKind = new TransferKind("General", TransferKindMode.IncomeAndExpense);
        var incompatibleKind = new TransferKind("Salary", TransferKindMode.Income);
        var transfer = CreateTransfer(account, originalKind, DateTime.Today, false);

        Assert.Throws<ArgumentException>(() =>
            transfer.UpdateDetails(
                25m,
                "Changed description",
                DateTime.Today,
                TransferMode.Expense,
                true,
                incompatibleKind));

        Assert.Equal(10m, transfer.Amount);
        Assert.Null(transfer.Description);
        Assert.Equal(TransferMode.Expense, transfer.TransferMode);
        Assert.False(transfer.IsCompleted);
        Assert.Same(originalKind, transfer.TransferKind);
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
