namespace FinanceTracker;

public static class DatabasePathResolver
{
    public const string EnvironmentVariableName = "FINANCETRACKER_DATABASE_PATH";

    public static string Resolve(
        string? configuredDatabasePath,
        string? localApplicationData)
    {
        if (string.IsNullOrWhiteSpace(configuredDatabasePath) &&
            string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException(
                "The operating system did not provide a local application-data directory.");
        }

        if (string.IsNullOrWhiteSpace(configuredDatabasePath))
        {
            return Path.Combine(
                localApplicationData!,
                "FinanceTracker",
                "financetracker.db");
        }

        if (!Path.IsPathFullyQualified(configuredDatabasePath))
        {
            throw new ArgumentException(
                "The configured database path must be absolute.",
                nameof(configuredDatabasePath));
        }

        return Path.GetFullPath(configuredDatabasePath);
    }
}
