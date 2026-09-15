# FinanceTracker

FinanceTracker V1 is a bilingual console application for recording accounts,
income, expenses, and scheduled transfers. It stores data locally in SQLite and
supports English and Brazilian Portuguese.

## V1 features

- Create, list, view, edit, and delete accounts.
- Create and list income and expense categories.
- Create, edit, delete, and complete transfers.
- Track scheduled, overdue, and completed transfers.
- Keep account balances synchronized with completed transfers.
- Show a monthly expense summary grouped by category.
- Preserve data between application runs in a local SQLite database.
- Use the console in English or Brazilian Portuguese.

Category editing and deletion are planned for a future version. V1 is a console
application and does not include a mobile app.

## Requirements

- .NET 10 SDK

## Build and run

From the repository root:

```bash
dotnet restore
dotnet build FinanceTracker.sln
dotnet test FinanceTracker.sln
dotnet run --project FinanceTracker/FinanceTracker.csproj
```

The application creates and migrates its database automatically on startup.

## Database location

By default, FinanceTracker stores `financetracker.db` in the operating system's
local application-data directory, inside a `FinanceTracker` folder.

For an isolated or custom installation, set `FINANCETRACKER_DATABASE_PATH` to an
absolute file path before launching the application.

Linux or macOS:

```bash
FINANCETRACKER_DATABASE_PATH=/absolute/path/financetracker.db \
  dotnet run --project FinanceTracker/FinanceTracker.csproj
```

PowerShell:

```powershell
$env:FINANCETRACKER_DATABASE_PATH = "C:\absolute\path\financetracker.db"
dotnet run --project FinanceTracker/FinanceTracker.csproj
```

Relative override paths are rejected so the database location cannot silently
depend on the directory used to launch the program.

## Publish

Create a framework-dependent Release build with:

```bash
dotnet publish FinanceTracker/FinanceTracker.csproj -c Release
```

The published application is written to
`FinanceTracker/bin/Release/net10.0/publish/`. Run it on a system with the .NET
10 runtime installed:

```bash
dotnet FinanceTracker/bin/Release/net10.0/publish/FinanceTracker.dll
```
