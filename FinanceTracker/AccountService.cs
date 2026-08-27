using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker;

public class AccountService {

    private AppDbContext DbContext { get; set; }

    public AccountService(AppDbContext dbContext) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }


    public async Task<Account> CreateAccountAsync(
        string name, 
        AccountType type, 
        decimal initialBalance) 
    {
        var account = new Account(name, type, initialBalance);

        DbContext.Accounts.Add(account);

        await DbContext.SaveChangesAsync();

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