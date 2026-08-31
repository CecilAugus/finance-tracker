using System.Data.Common;
using System.Globalization;
using System.Resources;
using FinanceTracker;
using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLitePCL;

#region starting app

Console.WriteLine("Choose Language: ");
Console.WriteLine("1 - English");
Console.WriteLine("2 - Português");

var option = Console.ReadLine();
var culture = option switch
{
    "2" => new CultureInfo("pt-BR"),
    _ => new CultureInfo("en")
};

CultureInfo.CurrentCulture = culture;
CultureInfo.CurrentUICulture = culture;

var resources = new ResourceManager(
    "FinanceTracker.Resources.Strings",
    typeof(Program).Assembly
);

var message = resources.GetString("AppStarted");
Console.WriteLine(message);

#endregion

#region database setting

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("Data Source=financetracker.db")
    .Options;

using var db = new AppDbContext(options);

#endregion

#region loggers and services

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var accountLogger = loggerFactory.CreateLogger<AccountService>();
var transferLogger = loggerFactory.CreateLogger<TransferService>();
var transferKindLogger = loggerFactory.CreateLogger<TransferKindService>();
var logger = loggerFactory.CreateLogger("general");

var accountService = new AccountService(db, accountLogger);
var transferService = new TransferService(db, transferLogger);
var transferKindService = new TransferKindService(db, transferKindLogger);

#endregion

using var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};
var cancellationToken = cancellationSource.Token;

#region testing

try
{
    // await using ensures it is disposed and rolled back if the block exits before committing.
    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
    

    var checkingAccount = await accountService.CreateAccountAsync(
        "Main Checking Account",
        AccountType.CheckingAccount,
        1000m,
        cancellationToken
    );


    var groceries = await transferKindService.CreateTransferKindAsync(
        "Groceries",
        TransferKindMode.Expense,
        cancellationToken
    );

    var salary = await transferKindService.CreateTransferKindAsync(
        "Salary",
        TransferKindMode.Income,
        cancellationToken
    );

    await transferService.CreateTransferAsync(
        2500m,
        "Monthly salary",
        DateTime.Today,
        TransferMode.Income,
        true,
        checkingAccount,
        salary,
        cancellationToken
    );

    await transferService.CreateTransferAsync(
        150m,
        "Groceries",
        DateTime.Today,
        TransferMode.Expense,
        true,
        checkingAccount,
        groceries,
        cancellationToken
    );

    await transferService.CreateTransferAsync(
        80m,
        "More groceries",
        DateTime.Today,
        TransferMode.Expense,
        true,
        checkingAccount,
        groceries,
        cancellationToken
    );

    var checkingAccountId = checkingAccount.Id;

    db.ChangeTracker.Clear();

    var loadedAccount = await accountService.GetAccountByIdAsync(
        checkingAccountId,
        cancellationToken
    ) ?? throw new InvalidOperationException(
        $"Account with ID {checkingAccountId} was not found."
    );

    // permanently commits all preceding database changes
    // if an exception occurs before this line, transaction is disposed (sqlite rolls back)
    await transaction.CommitAsync(cancellationToken);

    Console.WriteLine($"Account: {loadedAccount.Name}");
    Console.WriteLine($"Balance: {loadedAccount.GetAccountBalance()}");
    Console.WriteLine($"Total spending: {loadedAccount.GetTotalExpense()}");
    Console.WriteLine(
        $"Monthly spending: {loadedAccount.GetTotalExpenseByMonth(DateTime.Today)}"
    );
}
catch (ArgumentException e)
{
    logger.LogWarning(
        e,
        "A finance operation was rejected because its input was invalid.");

    Console.WriteLine("Invalid data. Please check the supplied values and try again.");
}
catch (DbUpdateException e)
{
    logger.LogError(
        e,
        "A database update failed while saving FinanceTracker data.");

    Console.WriteLine(
        "The data could not be saved. Please try again.");
}
catch (DbException e)
{
    logger.LogError(
        e,
        "A database operation failed while reading FinanceTracker data");
    Console.WriteLine("Stored data could not be loaded. Try again.");
}
catch (InvalidOperationException e)
{
    logger.LogError(
        e,
        "FinanceTracker reached an unexpected application state.");
    Console.WriteLine("The requested account could not be loaded.");
}
catch (OperationCanceledException e)
{
    logger.LogInformation(
        e,
        "The FinanceTracker operation was cancelled.");
    Console.WriteLine("Operation Cancelled");
}

#endregion