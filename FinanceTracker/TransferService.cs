namespace FinanceTracker;

public class TransferService {


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
        
        return transfer; 
    }
}