using System.Globalization;
using System.Resources;
using FinanceTracker;
using FinanceTracker.Data;
using Microsoft.Data.Sqlite;
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

string databasePath;

try
{
    databasePath = DatabasePathResolver.Resolve(
        Environment.GetEnvironmentVariable(
            DatabasePathResolver.EnvironmentVariableName),
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData));

    var databaseDirectory = Path.GetDirectoryName(databasePath)
        ?? throw new InvalidOperationException(
            "The database directory could not be determined.");

    Directory.CreateDirectory(databaseDirectory);
}
catch (Exception exception)
{
    startupLogger.LogCritical(
        exception,
        "Could not resolve or create the FinanceTracker data directory");
    Console.WriteLine(resources.GetString("DatabaseStartupError"));
    return;
}

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connectionString)
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
