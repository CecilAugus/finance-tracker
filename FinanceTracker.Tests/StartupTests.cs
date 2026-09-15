using System.Diagnostics;

namespace FinanceTracker.Tests;

public class StartupTests
{
    [Fact]
    public async Task Program_TwoRunsWithIsolatedDatabase_PersistsDataAndLoadsPortugueseResources()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory(
            "FinanceTracker-startup-test-");

        try
        {
            var databasePath = Path.Combine(
                temporaryDirectory.FullName,
                "financetracker.db");

            var firstRun = await RunApplicationAsync(
                databasePath,
                temporaryDirectory.FullName,
                "1", "1", "Startup Test", "1", "100", "0");

            Assert.Equal(0, firstRun.ExitCode);
            Assert.Contains("Finance Tracker has started.", firstRun.StandardOutput);
            Assert.Contains(
                "Account 'Startup Test' created successfully with ID 1.",
                firstRun.StandardOutput);
            Assert.True(File.Exists(databasePath));
            Assert.True(new FileInfo(databasePath).Length > 0);

            var secondRun = await RunApplicationAsync(
                databasePath,
                temporaryDirectory.FullName,
                "2", "2", "0");

            Assert.Equal(0, secondRun.ExitCode);
            Assert.Contains("Gerenciador de Finanças inicializado.", secondRun.StandardOutput);
            Assert.Contains("Contas", secondRun.StandardOutput);
            Assert.Contains("Startup Test", secondRun.StandardOutput);
        }
        finally
        {
            temporaryDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Program_DatabaseCannotBeOpened_DisplaysStartupErrorWithoutEnteringMenu()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory(
            "FinanceTracker-startup-error-test-");

        try
        {
            var directoryUsedAsDatabase = Path.Combine(
                temporaryDirectory.FullName,
                "database-directory");
            Directory.CreateDirectory(directoryUsedAsDatabase);

            var result = await RunApplicationAsync(
                directoryUsedAsDatabase,
                temporaryDirectory.FullName,
                "1");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains(
                "The database could not be opened. No data was changed.",
                result.StandardOutput);
            Assert.DoesNotContain("1 - Create account", result.StandardOutput);
        }
        finally
        {
            temporaryDirectory.Delete(recursive: true);
        }
    }

    private static async Task<ApplicationResult> RunApplicationAsync(
        string databasePath,
        string workingDirectory,
        params string[] inputLines)
    {
        var applicationAssembly = Path.Combine(
            AppContext.BaseDirectory,
            "FinanceTracker.dll");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(applicationAssembly);
        startInfo.Environment[DatabasePathResolver.EnvironmentVariableName] = databasePath;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The FinanceTracker process could not be started.");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        await process.StandardInput.WriteAsync(
            string.Join(Environment.NewLine, inputLines) + Environment.NewLine);
        process.StandardInput.Close();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The FinanceTracker process did not exit within 30 seconds.");
        }

        return new ApplicationResult(
            process.ExitCode,
            await standardOutput,
            await standardError);
    }

    private sealed record ApplicationResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
