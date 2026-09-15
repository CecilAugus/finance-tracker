namespace FinanceTracker.Tests;

public class DatabasePathResolverTests
{
    [Fact]
    public void Resolve_NoOverride_PlacesDatabaseUnderLocalApplicationData()
    {
        var localApplicationData = Path.GetFullPath("local-application-data");

        var databasePath = DatabasePathResolver.Resolve(
            configuredDatabasePath: null,
            localApplicationData);

        Assert.Equal(
            Path.Combine(
                localApplicationData,
                "FinanceTracker",
                "financetracker.db"),
            databasePath);
    }

    [Fact]
    public void Resolve_NoOverrideAndEmptyLocalAppData_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DatabasePathResolver.Resolve(
                configuredDatabasePath: null,
                localApplicationData: ""));
    }

    [Fact]
    public void Resolve_AbsoluteOverride_UsesConfiguredDatabasePath()
    {
        var configuredPath = Path.GetFullPath("isolated-financetracker.db");

        var databasePath = DatabasePathResolver.Resolve(
            configuredPath,
            localApplicationData: "");

        Assert.Equal(configuredPath, databasePath);
    }

    [Fact]
    public void Resolve_RelativeOverride_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DatabasePathResolver.Resolve(
                "relative-financetracker.db",
                Path.GetFullPath("local-application-data")));
    }
}
