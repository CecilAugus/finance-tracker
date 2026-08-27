using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceTracker;

public class AccountService {

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
        decimal initialBalance) 
    {
        var account = new Account(name, type, initialBalance);

        DbContext.Accounts.Add(account);

        await DbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created account {AccountId}",
            account.Id
            );

        return account;
    }

    public Task<Account?> GetAccountByIdAsync(int id) {
        if (id <= 0) {
            throw new ArgumentException(
                "id can't be 0 or lower",
                nameof(id));
        }

        var account = DbContext.Accounts
            .Include(account => account.AccountTransfers)
            .ThenInclude(transfer => transfer.TransferKind)
            .SingleOrDefaultAsync(account => account.Id == id);

        return account; 
    }
}