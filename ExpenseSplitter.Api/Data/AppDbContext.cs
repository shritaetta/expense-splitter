using Microsoft.EntityFrameworkCore;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<GroupMember> GroupMembers { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<ExpenseParticipant> ExpenseParticipants { get; set; } = null!;
        public DbSet<SplitTemplate> SplitTemplates { get; set; } = null!;
        public DbSet<SplitTemplateItem> SplitTemplateItems { get; set; } = null!;
        public DbSet<RecurringExpense> RecurringExpenses { get; set; } = null!;
        public DbSet<Settlement> Settlements { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // GroupMember: Composite Key & Relationships
            modelBuilder.Entity<GroupMember>()
                .HasKey(gm => new { gm.GroupId, gm.UserId });
                
            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.GroupMembers)
                .HasForeignKey(gm => gm.GroupId);

            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMembers)
                .HasForeignKey(gm => gm.UserId);

            // ExpenseParticipant: Composite Key & Relationships
            modelBuilder.Entity<ExpenseParticipant>()
                .HasKey(ep => new { ep.ExpenseId, ep.UserId });

            modelBuilder.Entity<ExpenseParticipant>()
                .HasOne(ep => ep.Expense)
                .WithMany(e => e.Participants)
                .HasForeignKey(ep => ep.ExpenseId);

            modelBuilder.Entity<ExpenseParticipant>()
                .HasOne(ep => ep.User)
                .WithMany()
                .HasForeignKey(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // SplitTemplateItem: Composite Key & Relationships
            modelBuilder.Entity<SplitTemplateItem>()
                .HasKey(sti => new { sti.TemplateId, sti.UserId });

            modelBuilder.Entity<SplitTemplateItem>()
                .HasOne(sti => sti.Template)
                .WithMany(st => st.TemplateItems)
                .HasForeignKey(sti => sti.TemplateId);

            modelBuilder.Entity<SplitTemplateItem>()
                .HasOne(sti => sti.User)
                .WithMany()
                .HasForeignKey(sti => sti.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Expense: Payer Relationship
            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Payer)
                .WithMany()
                .HasForeignKey(e => e.PayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // RecurringExpense: Payer Relationship
            modelBuilder.Entity<RecurringExpense>()
                .HasOne(re => re.Payer)
                .WithMany()
                .HasForeignKey(re => re.PayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Settlement: Payer and Payee Relationships
            modelBuilder.Entity<Settlement>()
                .HasOne(s => s.Payer)
                .WithMany()
                .HasForeignKey(s => s.PayerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Settlement>()
                .HasOne(s => s.Payee)
                .WithMany()
                .HasForeignKey(s => s.PayeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure Decimal Precision for SQLite (Though SQLite ignores this, good practice for migrations/future SQL Server)
            modelBuilder.Entity<Expense>().Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ExpenseParticipant>().Property(ep => ep.OwedAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ExpenseParticipant>().Property(ep => ep.ShareValue).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SplitTemplateItem>().Property(sti => sti.ShareValue).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<RecurringExpense>().Property(re => re.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Settlement>().Property(s => s.Amount).HasColumnType("decimal(18,2)");
        }
    }
}
