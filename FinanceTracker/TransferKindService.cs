using FinanceTracker.Data;

namespace FinanceTracker;

public class TransferKindService {
    private AppDbContext DbContext;

    public TransferKindService(AppDbContext dbContext) {
        ArgumentNullException.ThrowIfNull(dbContext);
        DbContext = dbContext;
    }

    public async Task<TransferKind> CreateTransferKindAsync(string name, TransferKindMode mode) {
        ArgumentNullException.ThrowIfNull(name);
        var kind = new TransferKind(name, mode);

        DbContext.TransferKinds.Add(kind);

        await DbContext.SaveChangesAsync();

        return kind;
    }
}