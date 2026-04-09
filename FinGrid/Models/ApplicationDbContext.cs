using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinGrid.Models
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public DbSet<BankConnection> BankConnections { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<BankTransaction> BankTransactions { get; set; }
        public DbSet<BudgetCategory> BudgetCategories { get; set; }
        public DbSet<ManualWallet> ManualWallets { get; set; }
        public DbSet<MccMapping> MccMappings { get; set; }
        public DbSet<ManualTransaction> ManualTransactions { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<BankAccount>()
                .Property(p => p.Balance)
                .HasColumnType("decimal(18,2)");

            builder.Entity<BankTransaction>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<BankTransaction>()
                .Property(p => p.BalanceAfter)
                .HasColumnType("decimal(18,2)");
        }

    }
}
