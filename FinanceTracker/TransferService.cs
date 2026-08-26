using FinanceTracker.Data;

namespace FinanceTracker;

public class TransferService {

    private AppDbContext DbContext;
    public TransferService(AppDbContext dbContext) {
        ArgumentNullException.ThrowIfNull(dbContext);
        DbContext = dbContext; 
    }

    public Transfer CreateTransfer(
        decimal amount, 
        string? description, 
        DateTime effectiveAt, 
        TransferMode transferMode,
        bool isCompleted, 
        Account account, 
        TransferKind transferKind) {
        
        var transfer = new Transfer(
            amount, description, effectiveAt, 
            transferMode, isCompleted, account, transferKind
        );
        
        account.AcceptTransfer(transfer);

        DbContext.Transfers.Add(transfer);
        
        DbContext.SaveChanges();
        
        return transfer; 
    }
}