namespace FinanceTracker;

public enum TransferMode {
    Income,
    Expense
}

public enum TransferStatus {
    Completed,
    Scheduled,
    Overdue
}

public class Transfer {
    public int Id { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTime EffectiveAt { get; private set; }
    public TransferMode TransferMode { get; private set; }
    public TransferStatus Status {
        get {
            if (IsCompleted) {
                return TransferStatus.Completed;
            }

            return EffectiveAt.Date > DateTime.Today ? 
                TransferStatus.Scheduled :
                TransferStatus.Overdue;
        }
    }
    public bool IsCompleted { get; private set; }
    public TransferKind TransferKind { get; private set; } = null!;
    public int TransferKindId { get; private set; }
    public Account Account { get; private set; } = null!; 
    public int AccountId { get; private set; }
    
    private Transfer() {} // For entity materialization by EF Core
    
    public Transfer(decimal amount, string? description, DateTime effectiveAt, TransferMode transferMode,
      bool isCompleted, Account account, TransferKind transferKind) {
        
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(transferKind);
        
        if (amount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(amount), 
                "Amount must be greater than zero");
        }

        if (effectiveAt.Year is < 2020 or > 2100) {
            throw new ArgumentOutOfRangeException(nameof(effectiveAt), 
                "Date should be between 2020 and 2100");
        }

        if (effectiveAt.Date > DateTime.Today && isCompleted) {
            throw new ArgumentException("A future transaction cannot already be completed.", 
                nameof(isCompleted));
        }

        if ((transferMode == TransferMode.Income && transferKind.TransferKindMode == TransferKindMode.Expense) ||
            (transferMode == TransferMode.Expense && transferKind.TransferKindMode == TransferKindMode.Income)) {
            throw new ArgumentException("Transfer kind is incompatible with the selected transfer mode",
                nameof(transferMode));
        }

        if (decimal.Round(amount, 2) != amount) {
            throw new ArgumentException(
                "Amount cannot have more than two decimal places.",
                nameof(amount));
        }
        
        Amount = amount;
        Description = description;
        EffectiveAt = effectiveAt;
        TransferMode = transferMode;
        IsCompleted = isCompleted;
        Account = account;
        TransferKind = transferKind; 
    }

    public void MarkAsCompleted() {
        if (EffectiveAt.Date > DateTime.Today) {
            throw new InvalidOperationException("Future transaction can't be completed before the due date.");
        }

        IsCompleted = true;
    }
}
