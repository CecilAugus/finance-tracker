# FinanceTracker mobile foundation research

Checked against current Microsoft/.NET documentation on 2026-09-06. The target is an offline-first Android app using .NET MAUI 10, EF Core 10, SQLite, MVVM, and dependency injection.

## Conclusion

`net10.0-android` with local SQLite is a sound baseline for this project. .NET 10 is an active LTS release through 2028-11-14, while MAUI 10 has its own shorter support window through 2027-05-11 and requires the latest servicing update. The project should therefore start on MAUI 10 now and budget for a MAUI 11 upgrade after it becomes stable. ([.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui))

Microsoft's MAUI local-database tutorial uses the third-party `sqlite-net-pcl` package and names `Microsoft.Data.Sqlite` as an alternative. It does not present EF Core as the default MAUI persistence tutorial. Keeping EF Core is nevertheless a defensible choice here because FinanceTracker already has a tested EF model and migrations, Microsoft maintains `Microsoft.EntityFrameworkCore.Sqlite`, and modern .NET includes Android as a target. This is a project-specific inference from the official platform and provider guidance, so EF behavior and startup time should be tested on an Android device early. ([MAUI local databases](https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/database-sqlite?view=net-maui-10.0), [EF Core SQLite provider](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/), [EF Core platforms](https://learn.microsoft.com/en-us/ef/core/miscellaneous/platforms))

## Supported stack and Android prerequisites

- The Android MAUI project should target `net10.0-android`. .NET 10 uses Android API 36 as its compile target. The .NET 10 templates recommend Android API 24 as the minimum to avoid desugaring runtime failures; API 21-23 remain supported only with Mono. Keep the default Mono runtime because CoreCLR on Android is still marked experimental for .NET 10. ([MAUI changes in .NET 10](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-10?view=net-maui-10.0))
- On Linux, Microsoft supports building MAUI for Android. The required pieces are the .NET 10 SDK, the `maui-android` workload, Android SDK/platform tools, an emulator or physical device, and a Java SDK. Current .NET for Android guidance recommends Microsoft OpenJDK 21 and setting `JAVA_HOME` and `ANDROID_HOME`. The `InstallAndroidDependencies` MSBuild target can inspect the project and install the exact SDK components it needs. ([MAUI installation](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?view=net-maui-10.0), [.NET for Android dependencies](https://learn.microsoft.com/en-us/dotnet/android/getting-started/installation/dependencies))
- This workstation already has .NET SDK `10.0.400` and runtime `10.0.11`, but `dotnet workload list` reports no installed workloads. The first concrete setup blocker is therefore installing `maui-android`, followed by Android SDK/JDK/emulator verification.

## Local database location

The database path must be built at runtime:

```csharp
var databasePath = Path.Combine(
    FileSystem.Current.AppDataDirectory,
    "financetracker.db");
```

`AppDataDirectory` is the cross-platform location for persistent application files; on Android it maps to the app's `FilesDir` and participates in Android Auto Backup from API 23. `CacheDirectory` must not hold the finance database because the operating system may clear cached data. ([MAUI file-system helpers](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-system-helpers?view=net-maui-10.0))

This replaces both current relative connection strings (`Data Source=financetracker.db`) in `Program.cs` and `AppDbContextFactory.cs` at mobile runtime. The design-time factory can continue using a development path for EF tooling, but runtime configuration should come from `MauiProgram`.

## EF Core lifetime and dependency injection

An EF `DbContext` is intended to represent one short unit of work, must be disposed, is not thread-safe, and must not run parallel operations. `AddDbContextFactory<TContext>` is the documented option when the dependency-injection scope does not match the required context lifetime. ([DbContext configuration and lifetime](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/))

This matters in MAUI because non-Blazor MAUI apps do not create a scope per navigation; an `AddScoped` registration effectively lives for the whole app unless code explicitly creates scopes. Microsoft recommends transient pages and view models in common navigation scenarios and registration in `MauiProgram.CreateMauiApp`. ([MAUI dependency injection](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection?view=net-maui-10.0))

FinanceTracker's current services each retain one injected `AppDbContext`. Before mobile pages use them, change them to receive `IDbContextFactory<AppDbContext>` and create/dispose a context inside each public operation. This avoids stale tracked entities, cross-page state, and concurrent access to one context. A representative target shape is:

```csharp
public sealed class AccountService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<AccountService> logger)
{
    public async Task<List<Account>> GetAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Accounts
            .AsNoTracking()
            .OrderBy(account => account.Name)
            .ThenBy(account => account.Id)
            .ToListAsync(cancellationToken);
    }
}
```

The mobile composition root can then register the database and presentation types:

```csharp
var databasePath = Path.Combine(
    FileSystem.Current.AppDataDirectory,
    "financetracker.db");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddTransient<AccountService>();
builder.Services.AddTransient<AccountsViewModel>();
builder.Services.AddTransient<AccountsPage>();
```

## Schema initialization and upgrades

FinanceTracker already has an EF migration, so the mobile app should stay on migrations and call `Database.MigrateAsync()` through a short-lived context before showing database-backed screens. Do not call `EnsureCreatedAsync()` on the production app database: it bypasses migrations and later makes `MigrateAsync()` fail. EF Core explicitly supports runtime migrations for applications that accept the startup tradeoffs, and EF 9+ takes a database-wide migration lock. ([Applying EF Core migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying), [`EnsureCreatedAsync` guidance](https://learn.microsoft.com/en-us/ef/core/managing-schemas/ensure-created))

For a per-install, app-owned offline database, runtime migration is a practical inference: there is no server deployment step or DBA identity available on the phone. It still needs an initialization/loading state and a recoverable error screen. Every migration should be inspected and tested both against an empty database and a copy from the previous released app version. SQLite can leave its `__EFMigrationsLock` acquired if the process dies during migration, which can block the next attempt; initialization should log this failure and offer a support/export path rather than silently deleting user data. ([SQLite provider limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations))

SQLite does not implement asynchronous I/O; `Microsoft.Data.Sqlite` async ADO.NET calls execute synchronously. EF-created databases enable write-ahead logging by default. Keep the async service API for UI composition and cancellation, but bound and measure expensive local queries because an `Async` suffix does not make SQLite work non-blocking. ([Microsoft.Data.Sqlite async limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async))

## Money and SQLite

SQLite does not natively support `decimal` ordering and comparison. EF can persist and read it, but some comparison and ordering operations otherwise require client evaluation. ([SQLite provider limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations))

FinanceTracker already mitigates this correctly in `AppDbContext`: `Amount` and `InitialBalance` are converted to integer minor units (`long`) and the domain rejects values beyond two decimal places. Retain that mapping and add migration/query tests before introducing database-side money ranges, sorting, or aggregation. Integer cents preserve exact financial values better than converting money to binary floating point.

## MVVM boundary

MAUI's MVVM guidance keeps the XAML view responsible for layout and bindings, while a view model exposes state and commands. `CommunityToolkit.Mvvm` supplies `ObservableObject`, observable-property generators, and relay-command generators, including async commands, without coupling the reusable services to MAUI controls. ([MAUI MVVM and data binding](https://learn.microsoft.com/en-us/dotnet/maui/xaml/fundamentals/mvvm?view=net-maui-10.0), [MVVM Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/), [`RelayCommand` generator](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand))

Therefore no `Console.ReadLine`/`Console.WriteLine` code should move into mobile. The reusable path is:

```text
XAML Page -> ViewModel command/state -> application service -> EF Core -> SQLite
```

The current domain entities, validation rules, EF mappings, migrations, and most service operations are reusable. The console menus and resource prompts remain a separate presentation adapter. The least risky solution layout is a reusable `net10.0` class library for domain/application/persistence code, the existing console host, and a new `net10.0-android` MAUI host. This avoids making the MAUI project the owner of design-time migrations and keeps all console code out of the Android package.

## Recommended implementation order

1. Install and verify the `maui-android` workload, OpenJDK 21, Android SDK API 36, emulator, and a physical-device deployment path.
2. Extract reusable code from the executable project without changing domain behavior; keep console and MAUI as separate hosts.
3. Refactor services to `IDbContextFactory<AppDbContext>` and rerun existing persistence tests.
4. Add the MAUI project, app-data connection path, DI registrations, and a database initializer that runs migrations before data screens load.
5. Prove the entire foundation with one thin vertical slice: an accounts `CollectionView`, an add-account page, persistence across app restarts, and an upgrade from a previous database copy.
6. Add transaction/category screens, dashboard queries, localization, validation/error states, and navigation only after that slice works on Android.

This order exposes the platform and EF risks before investing in the full mobile UI.
