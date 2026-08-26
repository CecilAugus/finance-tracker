using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker;

public class AccountService {

    private AppDbContext DbContext { get; set; }

    public AccountService(AppDbContext dbContext) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    
    public Account CreateAccount(string name, AccountType type, 
        decimal initialBalance)
    {
        var account = new Account(name, type, initialBalance);
        
        DbContext.Accounts.Add(account);

        DbContext.SaveChanges();
        
        return account; 
    }

    public Account? GetAccountById(int id) {
        if (id <= 0) {
            throw new ArgumentException("id can't be 0 or lower", nameof(id));
        }

        // eager loading: you explicitly request the relationships needed for the operation
        // don't include every relationship in every query - it retrieves unnecessary data. 
        var account = DbContext.Accounts
            .Include(account => account.AccountTransfers)
            .ThenInclude(transfer => transfer.TransferKind)
            .SingleOrDefault(account => account.Id == id);

        return account; 
    }
}