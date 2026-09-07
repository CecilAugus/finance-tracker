using System.Globalization;
using System.Resources;
using FinanceTracker;
using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FinanceTracker.ConsoleUI;

#region starting app

Console.WriteLine("Choose Language: ");
Console.WriteLine("1 - English");
Console.WriteLine("2 - Português");

var option = Console.ReadLine();
var culture = option switch
{
    "2" => new CultureInfo("pt-BR"),
    _ => new CultureInfo("en-US")
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

#region logging

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

var startupLogger = loggerFactory.CreateLogger("Startup");

#endregion

#region database setting

var applicationDataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "FinanceTracker");

try
{
    Directory.CreateDirectory(applicationDataDirectory);
}
catch (Exception exception)
{
    startupLogger.LogCritical(
        exception,
        "Could not create the FinanceTracker data directory");
    Console.WriteLine(resources.GetString("DatabaseStartupError"));
    return;
}

var databasePath = Path.Combine(
    applicationDataDirectory,
    "financetracker.db");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={databasePath}")
    .Options;

await using var db = new AppDbContext(options);

#endregion

#region services

var accountLogger = loggerFactory.CreateLogger<AccountService>();
var transferLogger = loggerFactory.CreateLogger<TransferService>();
var transferKindLogger = loggerFactory.CreateLogger<TransferKindService>();

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

try
{
    await db.Database.MigrateAsync(cancellationToken);
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    return;
}
catch (Exception exception)
{
    startupLogger.LogCritical(
        exception,
        "Could not initialize the FinanceTracker database at {DatabasePath}",
        databasePath);
    Console.WriteLine(resources.GetString("DatabaseStartupError"));
    return;
}

var consoleApplication = new ConsoleApplication(
    resources,
    accountService,
    transferKindService,
    transferService
    );

await consoleApplication.RunAsync(cancellationToken);
