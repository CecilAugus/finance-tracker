using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceTracker;

public class TransferService
{
    private AppDbContext DbContext;
    private readonly ILogger<TransferService> _logger;

    public TransferService(
        AppDbContext dbContext,
        ILogger<TransferService> logger)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <exception cref="ArgumentException">
    /// Thrown when the account data violates a domain rule.
    /// </exception>
    /// <exception cref="DbUpdateException">
    /// Thrown when the account cannot be saved.
    /// </exception>
    public async Task<Transfer> CreateTransferAsync(
        decimal amount,
        string? description,
        DateTime effectiveAt,
        TransferMode transferMode,
        bool isCompleted,
        Account account,
        TransferKind transferKind,
        CancellationToken cancellationToken = default)
    {
        var transfer = new Transfer(
            amount, description, effectiveAt,
            transferMode, isCompleted, account, transferKind
        );

        account.AcceptTransfer(transfer);

        DbContext.Transfers.Add(transfer);

        await DbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created transfer {TransferId}",
            transfer.Id);

        return transfer;
    }

    public Task<List<Transfer>> GetCompletableTransfersAsync(
        DateTime asOf,
        CancellationToken cancellationToken = default)
    {
        var effectiveThrough = asOf.Date;

        return DbContext.Transfers
            .Include(transfer => transfer.Account)
            .Include(transfer => transfer.TransferKind)
            .Where(transfer => !transfer.IsCompleted &&
                               transfer.EffectiveAt <= effectiveThrough)
            .OrderBy(transfer => transfer.EffectiveAt)
            .ThenBy(transfer => transfer.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Transfer?> MarkTransferAsCompletedAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentException(
                "id can't be 0 or lower",
                nameof(id));
        }

        var transfer = await DbContext.Transfers
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (transfer is null)
        {
            return null;
        }

        transfer.MarkAsCompleted();
        await DbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Marked transfer {TransferId} as completed",
            transfer.Id);

        return transfer;
    }
}
