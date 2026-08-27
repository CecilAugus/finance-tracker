using System.Globalization;
using System.Resources;
using FinanceTracker;
using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;

#region language selection
Console.WriteLine("Choose Language: ");
Console.WriteLine("1 - English");
Console.WriteLine("2 - Português");

var option = Console.ReadLine();
var culture = option switch {
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

var accountService = new AccountService(db);
var transferService = new TransferService(db);
var transferKindService = new TransferKindService(db);
#endregion


#region testing


var checkingAccount = await accountService.CreateAccountAsync(
    "Main Checking Account",
    AccountType.CheckingAccount,
    1000m
);


var groceries = await transferKindService.CreateTransferKindAsync(
    "Groceries",
    TransferKindMode.Expense
);

var salary = await transferKindService.CreateTransferKindAsync(
    "Salary",
    TransferKindMode.Income
);

await transferService.CreateTransferAsync(
    2500m,
    "Monthly salary",
    DateTime.Today,
    TransferMode.Income,
    true,
    checkingAccount,
    salary
);

await transferService.CreateTransferAsync(
    150m,
    "Groceries",
    DateTime.Today,
    TransferMode.Expense,
    true,
    checkingAccount,
    groceries
);

await transferService.CreateTransferAsync(
    80m,
    "More groceries",
    DateTime.Today,
    TransferMode.Expense,
    true,
    checkingAccount,
    groceries
);

var checkingAccountId = checkingAccount.Id;

db.ChangeTracker.Clear();

var loadedAccount = await accountService.GetAccountByIdAsync(checkingAccountId)
    ?? throw new InvalidOperationException(
        $"Account with ID {checkingAccountId} was not found."
    );

Console.WriteLine($"Account: {loadedAccount.Name}");
Console.WriteLine($"Balance: {loadedAccount.GetAccountBalance()}");
Console.WriteLine($"Total spending: {loadedAccount.GetTotalExpense()}");
Console.WriteLine(
    $"Monthly spending: {loadedAccount.GetTotalExpenseByMonth(DateTime.Today)}"
);
#endregion
