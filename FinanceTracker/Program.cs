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

#region main loop

string GetText(string key)
{
    return resources.GetString(key)
           ?? throw new MissingManifestResourceException(
               $"Resource key '{key}' was not found.");
}



var isRunning = true;

while (isRunning && !cancellationToken.IsCancellationRequested)
{
    Console.WriteLine();
    Console.WriteLine(GetText("MenuTitle"));
    Console.WriteLine(GetText("MenuCreateAccount"));
    Console.WriteLine(GetText("MenuListAccounts"));
    Console.WriteLine(GetText("MenuCreateTransferKind"));
    Console.WriteLine(GetText("MenuListTransferKinds"));
    Console.WriteLine(GetText("MenuCreateTransfer"));
    Console.WriteLine(GetText("MenuViewAccount"));
    Console.WriteLine(GetText("MenuExit"));

    Console.Write(GetText("MenuChooseOption") + " ");
    var selectedOption = Console.ReadLine();

    switch (selectedOption)
    {
        case "1":
            Console.WriteLine(GetText("NotImplemented"));
            break;

        case "2":
            Console.WriteLine(GetText("NotImplemented"));
            break;

        case "3":
            Console.WriteLine(GetText("NotImplemented"));
            break;

        case "4":
            Console.WriteLine(GetText("NotImplemented"));
            break;

        case "5":
            Console.WriteLine(GetText("NotImplemented"));
            break;

        case "6":
            Console.WriteLine(GetText("NotImplemented"));
            break;

        case "0":
            isRunning = false;
            break;

        default:
            Console.WriteLine(GetText("InvalidOption"));
            break;
    }
}

#endregion