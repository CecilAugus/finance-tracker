using FinanceTracker.Data;

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
}