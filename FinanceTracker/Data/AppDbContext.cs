using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Data;

public class AppDbContext : DbContext {
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferKind> TransferKinds => Set<TransferKind>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Transfer>()
            .Property(transfer => transfer.Amount)
            .HasConversion(
                amount => (long)(amount * 100),
                amount => amount / 100m);

        modelBuilder.Entity<Account>()
            .Property(account => account.InitialBalance)
            .HasConversion(
                balance => (long)(balance * 100),
                balance => balance / 100m);
    }
}