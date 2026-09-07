using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceTracker;

public class AccountService
{
    private AppDbContext DbContext { get; set; }
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        AppDbContext dbContext,
        ILogger<AccountService> logger)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task<Account> CreateAccountAsync(
        string name,
        AccountType type,
        decimal initialBalance,
        CancellationToken cancellationToken = default)
    {
        var account = new Account(name, type, initialBalance);

        DbContext.Accounts.Add(account);

        await DbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created account {AccountId}",
            account.Id
        );

        return account;
    }


    public Task<Account?> GetAccountByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentException(
                "id can't be 0 or lower",
                nameof(id));
        }

        var account = DbContext.Accounts
            .Include(account => account.AccountTransfers)
            .ThenInclude(transfer => transfer.TransferKind)
            .SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

        return account;
    }

    public Task<List<Account>> GetAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        return DbContext.Accounts
            .Include(account => account.AccountTransfers)
            .OrderBy(account => account.Name)
            .ThenBy(account => account.Id)
            .ToListAsync(cancellationToken);
    }
}
