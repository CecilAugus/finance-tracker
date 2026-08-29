using FinanceTracker.Data;
using Microsoft.Extensions.Logging;

namespace FinanceTracker;

public class TransferKindService
{
    private AppDbContext DbContext;
    private readonly ILogger<TransferKindService> _logger;

    public TransferKindService(
        AppDbContext dbContext,
        ILogger<TransferKindService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<TransferKind> CreateTransferKindAsync(
        string name, 
        TransferKindMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        var kind = new TransferKind(name, mode);

        DbContext.TransferKinds.Add(kind);

        await DbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created transfer kind {TransferKindId}",
            kind.Id
        );

        return kind;
    }
}