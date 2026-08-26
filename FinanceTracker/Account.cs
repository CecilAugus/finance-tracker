namespace FinanceTracker;

public enum AccountType {
    CheckingAccount,
    SavingsAccount,
    Cash,
    CreditCard,
    InvestmentPortfolio,
    Other
}

public class Account {
    public int Id { get; private set; }
    public string Name { get; private set; }
    public AccountType AccountType { get; private set; }
    public decimal InitialBalance { get; private set; }
    private readonly List<Transfer> _accountTransfers = [];
    public IReadOnlyCollection<Transfer> AccountTransfers => _accountTransfers;


    public Account(string name, AccountType accountType, decimal initialBalance) {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException(
                "Account name cannot be empty",
                nameof(name));
        }
        
        if (decimal.Round(initialBalance, 2) != initialBalance) {
            throw new ArgumentException(
                "Initial balance cannot have more than two decimal places.",
                nameof(initialBalance));
        }

        Name = name.Trim();
        AccountType = accountType;
        InitialBalance = initialBalance;
    }


    public void AcceptTransfer(Transfer transfer) {
        ArgumentNullException.ThrowIfNull(transfer);

        if (transfer.Account != this) {
            throw new ArgumentException($"Transfer doesn't belong to account: {Name}",
                nameof(transfer));
        }

        if (_accountTransfers.Contains(transfer)) {
            throw new ArgumentException($"Transfer has already being made to this Account",
                nameof(transfer));
        }

        _accountTransfers.Add(transfer);
    }


    public List<Transfer> GetTransfersByMode(TransferMode mode) {
        ArgumentNullException.ThrowIfNull(mode);

        return _accountTransfers
            .Where(transfer => transfer.TransferMode == mode)
            .ToList();
    }

    
    public decimal GetTotalByMode(TransferMode mode) {
        ArgumentNullException.ThrowIfNull(mode);

        return _accountTransfers
            .Where(transfer => transfer.TransferMode == mode)
            .Sum(transfer => transfer.Amount);
    }

    
    public List<Transfer> GetTransfersFromNewest() {
        return _accountTransfers
            .OrderByDescending(transfer => transfer.EffectiveAt)
            .ToList();
    }

    
    public decimal GetAccountBalance() {
        return InitialBalance + _accountTransfers
            .Where(transfer => transfer.IsCompleted)
            .Sum(transfer => transfer.TransferMode == TransferMode.Income
                ? transfer.Amount
                : -transfer.Amount);
    }

    
    public decimal GetTotalExpenseForKind(TransferKind kind) {
        ArgumentNullException.ThrowIfNull(kind);

        return _accountTransfers
            .Where(transfer => transfer.IsCompleted &&
                               transfer.TransferKind == kind &&
                               transfer.TransferMode == TransferMode.Expense)
            .Sum(transfer => transfer.Amount);
    }

    
    public decimal GetTotalExpense() {
        return _accountTransfers
            .Where(transfer => transfer.IsCompleted &&
                               transfer.TransferMode == TransferMode.Expense)
            .Sum(transfer => transfer.Amount);
    }

    
    public Dictionary<TransferKind, decimal> GetTotalExpensePerKind() {
        return _accountTransfers
            .Where(transfer => transfer.IsCompleted &&
                               transfer.TransferMode == TransferMode.Expense)
            .GroupBy(transfer => transfer.TransferKind)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(transfer => transfer.Amount));
    }
    

    public decimal GetTotalExpenseByMonth(DateTime date) {
        return _accountTransfers
            .Where(transfer => transfer.IsCompleted &&
                               transfer.TransferMode == TransferMode.Expense &&
                               transfer.EffectiveAt.Month == date.Month &&
                               transfer.EffectiveAt.Year == date.Year)
            .Sum(transfer => transfer.Amount);
    }


    public Dictionary<TransferKind, decimal> GetMonthlyExpensePerKind(DateTime month) {
        return _accountTransfers
            .Where(transfer => transfer.IsCompleted &&
                               transfer.TransferMode == TransferMode.Expense &&
                               transfer.EffectiveAt.Month == month.Month &&
                               transfer.EffectiveAt.Year == month.Year)
            .GroupBy(transfer => transfer.TransferKind)
            .ToDictionary(
                group => group.Key, 
                group => group.Sum(transfer => transfer.Amount));
    }
}