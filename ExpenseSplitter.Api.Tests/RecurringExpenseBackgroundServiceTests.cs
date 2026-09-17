using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Models;
using ExpenseSplitter.Api.Services;

namespace ExpenseSplitter.Api.Tests
{
    public class RecurringExpenseBackgroundServiceTests
    {
        private ServiceProvider GetServiceProvider(AppDbContext context)
        {
            var services = new ServiceCollection();
            services.AddSingleton(context);
            services.AddSingleton<IProrationCalculator, ProrationCalculator>();
            services.AddScoped<IRecurringExpenseGenerator, RecurringExpenseGenerator>();
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task ProcessRecurringExpenses_GeneratesExpenseAndAdvancesNextRunDate()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var context = new AppDbContext(options);

            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", Currency = "USD" };
            var payer = new User { Id = Guid.NewGuid(), Name = "Payer", Email = "payer@test.com" };
            var debtor = new User { Id = Guid.NewGuid(), Name = "Debtor", Email = "debtor@test.com" };

            context.Groups.Add(group);
            context.Users.AddRange(payer, debtor);
            
            // Join dates long before the cycle
            var joinDate = DateTimeOffset.UtcNow.AddDays(-100);
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = payer.Id, JoinedAt = joinDate });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = debtor.Id, JoinedAt = joinDate });

            var recurringExpense = new RecurringExpense
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                PayerId = payer.Id,
                Description = "Monthly Rent",
                TotalAmount = 1000m,
                Category = "Housing",
                Frequency = "Monthly",
                StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
                NextRunDate = DateTimeOffset.UtcNow.AddDays(-1), // DUE NOW (in the past)
                SplitTypeId = SplitType.Equal
            };
            
            var originalNextRunDate = recurringExpense.NextRunDate;
            context.RecurringExpenses.Add(recurringExpense);
            
            await context.SaveChangesAsync();

            var serviceProvider = GetServiceProvider(context);
            var backgroundService = new RecurringExpenseBackgroundService(serviceProvider);

            // Act
            await backgroundService.ProcessRecurringExpenses(CancellationToken.None);

            // Assert
            
            // 1. NextRunDate was advanced
            var updatedRecurring = await context.RecurringExpenses.FindAsync(recurringExpense.Id);
            Assert.NotNull(updatedRecurring);
            Assert.Equal(originalNextRunDate.AddMonths(1), updatedRecurring.NextRunDate);

            // 2. A single Expense was generated
            var expenses = await context.Expenses.Include(e => e.Participants).ToListAsync();
            Assert.Single(expenses);
            
            var generatedExpense = expenses.First();
            Assert.Equal(group.Id, generatedExpense.GroupId);
            Assert.Equal(1000m, generatedExpense.TotalAmount);
            Assert.Equal(recurringExpense.Id, generatedExpense.RecurringExpenseId);

            // 3. Payer is excluded from Participants, debtor owes half
            Assert.Single(generatedExpense.Participants);
            var debtorShare = generatedExpense.Participants.Single();
            Assert.Equal(debtor.Id, debtorShare.UserId);
            Assert.Equal(500m, debtorShare.OwedAmount);
        }
    }
}
