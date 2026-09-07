using FinanceTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinanceTracker.Tests;

public class PersistenceTests
{
    [Fact]
    public async Task Services_SaveAndReloadCompleteAccountGraph()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var accountService = new AccountService(
            dbContext,
            NullLogger<AccountService>.Instance);
        var kindService = new TransferKindService(
            dbContext,
            NullLogger<TransferKindService>.Instance);
        var transferService = new TransferService(
            dbContext,
            NullLogger<TransferService>.Instance);

        var account = await accountService.CreateAccountAsync(
            "Checking",
            AccountType.CheckingAccount,
            1234.56m);
        var kind = await kindService.CreateTransferKindAsync(
            "Food",
            TransferKindMode.Expense);
        await transferService.CreateTransferAsync(
            78.91m,
            "Groceries",
            new DateTime(2025, 6, 10),
            TransferMode.Expense,
            true,
            account,
            kind);

        dbContext.ChangeTracker.Clear();

        var reloadedAccount = await accountService.GetAccountByIdAsync(account.Id);

        Assert.NotNull(reloadedAccount);
        Assert.Equal("Checking", reloadedAccount.Name);
        Assert.Equal(1234.56m, reloadedAccount.InitialBalance);

        var reloadedTransfer = Assert.Single(reloadedAccount.AccountTransfers);
        Assert.Equal(78.91m, reloadedTransfer.Amount);
        Assert.Equal("Groceries", reloadedTransfer.Description);
        Assert.Equal("Food", reloadedTransfer.TransferKind.Name);
        Assert.Same(reloadedAccount, reloadedTransfer.Account);
    }

    [Fact]
    public async Task TransferService_ListsDueTransfersAndMarksOneAsCompleted()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var accountService = new AccountService(
            dbContext,
            NullLogger<AccountService>.Instance);
        var kindService = new TransferKindService(
            dbContext,
            NullLogger<TransferKindService>.Instance);
        var transferService = new TransferService(
            dbContext,
            NullLogger<TransferService>.Instance);

        var account = await accountService.CreateAccountAsync(
            "Checking",
            AccountType.CheckingAccount,
            100m);
        var kind = await kindService.CreateTransferKindAsync(
            "Bills",
            TransferKindMode.Expense);
        var dueTransfer = await transferService.CreateTransferAsync(
            25m,
            "Due bill",
            DateTime.Today,
            TransferMode.Expense,
            false,
            account,
            kind);
        await transferService.CreateTransferAsync(
            40m,
            "Future bill",
            DateTime.Today.AddDays(1),
            TransferMode.Expense,
            false,
            account,
            kind);

        var completableTransfers = await transferService
            .GetCompletableTransfersAsync(DateTime.Today);

        var completableTransfer = Assert.Single(completableTransfers);
        Assert.Equal(dueTransfer.Id, completableTransfer.Id);

        var completedTransfer = await transferService
            .MarkTransferAsCompletedAsync(dueTransfer.Id);

        Assert.NotNull(completedTransfer);
        Assert.True(completedTransfer.IsCompleted);

        dbContext.ChangeTracker.Clear();
        var reloadedAccount = await accountService.GetAccountByIdAsync(account.Id);

        Assert.NotNull(reloadedAccount);
        Assert.Equal(75m, reloadedAccount.GetAccountBalance());
        Assert.Single(
            reloadedAccount.AccountTransfers,
            transfer => transfer.IsCompleted);
        Assert.Single(
            reloadedAccount.AccountTransfers,
            transfer => !transfer.IsCompleted);
    }
}
