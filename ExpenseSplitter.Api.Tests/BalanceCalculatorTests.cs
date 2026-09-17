using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Models;
using ExpenseSplitter.Api.Services;

namespace ExpenseSplitter.Api.Tests
{
    public class BalanceCalculatorTests
    {
        [Fact]
        public async Task CalculateNetBalancesAsync_IncludesExpensesAndSettlements()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            var group = new Group { Id = Guid.NewGuid(), Name = "Balance Test", Currency = "USD" };
            var alice = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com" }; // Payer
            var bob = new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@test.com" };       // Debtor
            
            context.Groups.Add(group);
            context.Users.AddRange(alice, bob);
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = alice.Id });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = bob.Id });

            // Expense 1: Alice pays $100. Bob owes $50.
            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                PayerId = alice.Id,
                TotalAmount = 100m,
                Description = "Dinner",
                Category = "Food",
                Date = DateTimeOffset.UtcNow,
                SplitTypeId = SplitType.Equal,
                Participants = new List<ExpenseParticipant>
                {
                    new ExpenseParticipant { UserId = bob.Id, OwedAmount = 50m }
                }
            };
            context.Expenses.Add(expense);

            // Settlement 1: Bob pays Alice $20 back.
            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                PayerId = bob.Id,   // Bob pays
                PayeeId = alice.Id, // Alice receives
                Amount = 20m,
                Date = DateTimeOffset.UtcNow
            };
            context.Settlements.Add(settlement);

            await context.SaveChangesAsync();

            var calculator = new BalanceCalculator(context);

            // Act
            var balances = await calculator.CalculateNetBalancesAsync(group.Id);

            // Assert
            // Expected: 
            // Alice: +50 from expense, -20 from settlement = +30
            // Bob: -50 from expense, +20 from settlement = -30

            Assert.Equal(30m, balances[alice.Id]);
            Assert.Equal(-30m, balances[bob.Id]);
        }
    }
}
