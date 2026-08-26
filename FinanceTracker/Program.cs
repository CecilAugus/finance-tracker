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
var transferService = new TransferService();
#endregion


#region testing


var checkingAccount = accountService.CreateAccount(
    "Main Checking Account",
    AccountType.CheckingAccount,
    1000m
);

var groceries = new TransferKind(
    "Groceries",
    TransferKindMode.Expense
);

var salary = new TransferKind(
    "Salary",
    TransferKindMode.Income
);

transferService.CreateTransfer(
    2500m,
    "Monthly salary",
    DateTime.Today,
    TransferMode.Income,
    true,
    checkingAccount,
    salary
);

transferService.CreateTransfer(
    150m,
    "Groceries",
    DateTime.Today,
    TransferMode.Expense,
    true,
    checkingAccount,
    groceries
);

transferService.CreateTransfer(
    80m,
    "More groceries",
    DateTime.Today,
    TransferMode.Expense,
    true,
    checkingAccount,
    groceries
);

Console.WriteLine($"Account: {checkingAccount.Name}");
Console.WriteLine($"Balance: {checkingAccount.GetAccountBalance()}");
Console.WriteLine($"Total spending: {checkingAccount.GetTotalExpense()}");
Console.WriteLine(
    $"Monthly spending: {checkingAccount.GetTotalExpenseByMonth(DateTime.Today)}"
);
#endregion
