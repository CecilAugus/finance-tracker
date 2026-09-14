using System.Globalization;
using System.Resources;
using FinanceTracker.ConsoleUI;
using FinanceTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinanceTracker.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCollectionDefinition
{
    public const string Name = "Console application";
}

[Collection(ConsoleCollectionDefinition.Name)]
public class ConsoleApplicationTests
{
    [Fact]
    public async Task RunAsync_UserCompletesV1Workflow_PersistsExpectedResultsAndReports()
    {
        var originalInput = Console.In;
        var originalOutput = Console.Out;
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

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
            var resources = new ResourceManager(
                "FinanceTracker.Resources.Strings",
                typeof(Account).Assembly);
            using var output = new StringWriter(culture);

            Console.SetOut(output);
            Console.SetIn(CreateInput(
                "1", "Checking", "1", "1000",
                "2",
                "3", "Salary", "1",
                "3", "Food", "2",
                "3", "Bills", "2",
                "4",
                "5", "1", "1", "1", "500", "Salary", "06/10/2025", "1",
                "5", "1", "2", "2", "100.50", "Groceries", "06/15/2025", "1",
                "5", "1", "2", "3", "75", "Electricity", "06/20/2025", "2",
                "6", "1",
                "12", "1", "06/2025",
                "10", "1", "3", "2", "3", "80", "Updated bill", "06/20/2025", "2",
                "7", "3",
                "12", "1", "06/2025",
                "0"));

            var firstRun = new ConsoleApplication(
                resources,
                accountService,
                kindService,
                transferService);

            await firstRun.RunAsync(CancellationToken.None);

            var firstRunOutput = output.ToString();

            Assert.Contains("Account 'Checking' created successfully with ID 1.", firstRunOutput);
            Assert.Contains("Transfer created successfully with ID 3.", firstRunOutput);
            Assert.Contains("Current balance: $1,399.50", firstRunOutput);
            Assert.Contains("Transfer #3 updated successfully.", firstRunOutput);
            Assert.Contains("Transfer #3 marked as completed.", firstRunOutput);

            var firstReport = GetReport(
                firstRunOutput,
                "Completed expenses for Checking in June 2025:",
                occurrence: 1);
            Assert.Contains("Food: $100.50", firstReport);
            Assert.Contains("Total: $100.50", firstReport);
            Assert.DoesNotContain("Bills:", firstReport);

            var secondReport = GetReport(
                firstRunOutput,
                "Completed expenses for Checking in June 2025:",
                occurrence: 2);
            Assert.Contains("Bills: $80.00", secondReport);
            Assert.Contains("Food: $100.50", secondReport);
            Assert.Contains("Total: $180.50", secondReport);

            dbContext.ChangeTracker.Clear();
            var persistedAccount = await accountService.GetAccountByIdAsync(1);

            Assert.NotNull(persistedAccount);
            Assert.Equal(3, persistedAccount.AccountTransfers.Count);
            Assert.Equal(1319.50m, persistedAccount.GetAccountBalance());

            Console.SetIn(CreateInput(
                "6", "1",
                "11", "1", "3", "0",
                "11", "1", "3", "1",
                "6", "1",
                "8", "1", "Main Checking", "1", "1100",
                "6", "1",
                "9", "1", "0",
                "2",
                "9", "1", "1",
                "2",
                "99",
                "0"));

            var secondRun = new ConsoleApplication(
                resources,
                accountService,
                kindService,
                transferService);

            await secondRun.RunAsync(CancellationToken.None);

            var completeOutput = output.ToString();

            Assert.Contains("Transfer deletion cancelled.", completeOutput);
            Assert.Contains("Transfer #3 deleted successfully.", completeOutput);
            Assert.Contains("Current balance: $1,399.50", completeOutput);
            Assert.Contains("Account \"Main Checking\" (ID 1) updated successfully.", completeOutput);
            Assert.Contains("Current balance: $1,499.50", completeOutput);
            Assert.Contains("Account deletion cancelled.", completeOutput);
            Assert.Contains("Account \"Main Checking\" (ID 1) deleted successfully.", completeOutput);
            Assert.Contains("No accounts have been created yet.", completeOutput);
            Assert.Contains("Invalid option. Please try again.", completeOutput);

            dbContext.ChangeTracker.Clear();
            Assert.Empty(await accountService.GetAccountsAsync());
            Assert.Equal(3, (await kindService.GetTransferKindsAsync()).Count);
        }
        finally
        {
            Console.SetIn(originalInput);
            Console.SetOut(originalOutput);
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task RunAsync_PortugueseCulture_DisplaysLocalizedMenu()
    {
        var originalInput = Console.In;
        var originalOutput = Console.Out;
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("pt-BR");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            var resources = new ResourceManager(
                "FinanceTracker.Resources.Strings",
                typeof(Account).Assembly);
            var application = new ConsoleApplication(
                resources,
                new AccountService(dbContext, NullLogger<AccountService>.Instance),
                new TransferKindService(dbContext, NullLogger<TransferKindService>.Instance),
                new TransferService(dbContext, NullLogger<TransferService>.Instance));
            using var output = new StringWriter(culture);

            Console.SetIn(CreateInput("0"));
            Console.SetOut(output);

            await application.RunAsync(CancellationToken.None);

            var localizedOutput = output.ToString();
            Assert.Contains("1 - Criar conta", localizedOutput);
            Assert.Contains("10 - Editar transação", localizedOutput);
            Assert.Contains("11 - Excluir transação", localizedOutput);
            Assert.Contains("12 - Ver resumo mensal de despesas", localizedOutput);
            Assert.Contains("0 - Sair", localizedOutput);
        }
        finally
        {
            Console.SetIn(originalInput);
            Console.SetOut(originalOutput);
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static StringReader CreateInput(params string[] lines)
    {
        return new StringReader(string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    private static string GetReport(
        string output,
        string heading,
        int occurrence)
    {
        var headingIndex = -1;

        for (var currentOccurrence = 0; currentOccurrence < occurrence; currentOccurrence++)
        {
            headingIndex = output.IndexOf(
                heading,
                headingIndex + 1,
                StringComparison.Ordinal);
        }

        Assert.True(headingIndex >= 0, $"Report heading was not found: {heading}");

        var nextMenuIndex = output.IndexOf(
            Environment.NewLine + "FinanceTracker",
            headingIndex,
            StringComparison.Ordinal);

        return nextMenuIndex >= 0
            ? output[headingIndex..nextMenuIndex]
            : output[headingIndex..];
    }
}
