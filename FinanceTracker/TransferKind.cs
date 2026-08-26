namespace FinanceTracker;

public enum TransferKindMode {
    Income,
    Expense,
    IncomeAndExpense
}

public class TransferKind {
    public int Id { get; private set; }
    public string Name { get; private set; }
    public TransferKindMode TransferKindMode { get; private set; }
    
    public TransferKind(string name, TransferKindMode transferKindMode) {
        ArgumentNullException.ThrowIfNull(name);
        
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException($"{nameof(name)} can't be empty", nameof(name));
        }
        
        Name = name.Trim();
        TransferKindMode = transferKindMode;
    }
}