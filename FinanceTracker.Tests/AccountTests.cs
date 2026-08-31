using FinanceTracker;

namespace FinanceTracker.Tests;

public class AccountTests
{
    [Fact]
    public void Constructor_ValidValues_CreatesAccount()
    {
        const string name = " Main Checking Account ";
        const AccountType accountType = AccountType.CheckingAccount;
        const decimal initialBalance = 1000.25m;

        var account = new Account(
            name,
            accountType,
            initialBalance
        );

        Assert.Equal("Main Checking Account", account.Name);
        Assert.Equal(accountType, account.AccountType);
        Assert.Equal(initialBalance, account.InitialBalance);
    }
    
    [Fact]
    public void Constructor_InitialBalanceWithMoreThanTwoDecimalPlaces_ThrowsArgumentException()
    {
        const decimal invalidBalance = 1000.123m;

        var exception = Assert.Throws<ArgumentException>(() =>
            new Account(
                "Main Checking Account",
                AccountType.CheckingAccount,
                invalidBalance
            )
        );

        Assert.Equal("initialBalance", exception.ParamName);
    }
    
    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new Account(
                null!,
                AccountType.CheckingAccount,
                1000m
            )
        );

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_BlankName_ThrowsArgumentException(string invalidName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Account(
                invalidName,
                AccountType.CheckingAccount,
                1000m
            )
        );

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void AcceptTransfer_TransferBelongsToAnotherAccount_ThrowsArgumentException()
    {
        var receivingAccount = new Account("Checking", AccountType.CheckingAccount, 0m);
        var otherAccount = new Account("Savings", AccountType.SavingsAccount, 0m);
        var kind = new TransferKind("Salary", TransferKindMode.Income);
        var transfer = new Transfer(
            100m,
            "Salary",
            DateTime.Today,
            TransferMode.Income,
            true,
            otherAccount,
            kind);

        var exception = Assert.Throws<ArgumentException>(() =>
            receivingAccount.AcceptTransfer(transfer));

        Assert.Equal("transfer", exception.ParamName);
        Assert.Empty(receivingAccount.AccountTransfers);
    }

    [Fact]
    public void GetAccountBalance_UsesOnlyCompletedTransfersWithCorrectSigns()
    {
        var account = new Account("Checking", AccountType.CheckingAccount, 1000m);
        var incomeKind = new TransferKind("Salary", TransferKindMode.Income);
        var expenseKind = new TransferKind("Food", TransferKindMode.Expense);

        AddTransfer(account, 500m, TransferMode.Income, true, incomeKind);
        AddTransfer(account, 125.50m, TransferMode.Expense, true, expenseKind);
        AddTransfer(account, 900m, TransferMode.Expense, false, expenseKind);

        var balance = account.GetAccountBalance();

        Assert.Equal(1374.50m, balance);
    }

    [Fact]
    public void GetMonthlyExpensePerKind_FiltersAndGroupsEligibleTransfers()
    {
        var account = new Account("Checking", AccountType.CheckingAccount, 0m);
        var food = new TransferKind("Food", TransferKindMode.Expense);
        var housing = new TransferKind("Housing", TransferKindMode.Expense);
        var salary = new TransferKind("Salary", TransferKindMode.Income);
        var targetMonth = new DateTime(2025, 6, 1);

        AddTransfer(account, 20m, TransferMode.Expense, true, food, new DateTime(2025, 6, 3));
        AddTransfer(account, 15m, TransferMode.Expense, true, food, new DateTime(2025, 6, 17));
        AddTransfer(account, 700m, TransferMode.Expense, true, housing, new DateTime(2025, 6, 5));
        AddTransfer(account, 80m, TransferMode.Expense, false, food, new DateTime(2025, 6, 20));
        AddTransfer(account, 2000m, TransferMode.Income, true, salary, new DateTime(2025, 6, 1));
        AddTransfer(account, 40m, TransferMode.Expense, true, food, new DateTime(2025, 5, 30));

        var expenses = account.GetMonthlyExpensePerKind(targetMonth);

        Assert.Equal(2, expenses.Count);
        Assert.Equal(35m, expenses[food]);
        Assert.Equal(700m, expenses[housing]);
    }

    private static Transfer AddTransfer(
        Account account,
        decimal amount,
        TransferMode mode,
        bool isCompleted,
        TransferKind kind,
        DateTime? effectiveAt = null)
    {
        var transfer = new Transfer(
            amount,
            null,
            effectiveAt ?? DateTime.Today,
            mode,
            isCompleted,
            account,
            kind);

        account.AcceptTransfer(transfer);
        return transfer;
    }
}
