using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinanceTracker.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
    public AppDbContext CreateDbContext(string[] args) {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source =financetracker.db")
            .Options;

        return new AppDbContext(options);
    }
}