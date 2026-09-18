using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ExpenseSplitter.Api.Controllers;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.DTOs;
using ExpenseSplitter.Api.Models;
using ExpenseSplitter.Api.Services;

namespace ExpenseSplitter.Api.Tests
{
    public class SettlementsControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetSuggestedSettlements_AfterPartialPayment_CalculatesCorrectly()
        {
            // Arrange
            var context = GetDbContext();
            var group = new Group { Id = Guid.NewGuid(), Name = "Settlement Test Group", Currency = "USD" };
            var alice = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com" }; // Payer
            var bob = new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@test.com" };       // Debtor

            context.Groups.Add(group);
            context.Users.AddRange(alice, bob);
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = alice.Id, JoinedAt = DateTimeOffset.UtcNow });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = bob.Id, JoinedAt = DateTimeOffset.UtcNow });

            // 1. Create an expense where Bob owes Alice $100
            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                PayerId = alice.Id,
                TotalAmount = 100m,
                Description = "Concert Tickets",
                Category = "Entertainment",
                Date = DateTimeOffset.UtcNow,
                SplitTypeId = SplitType.ExactAmount,
                Participants = new List<ExpenseParticipant>
                {
                    new ExpenseParticipant { UserId = bob.Id, OwedAmount = 100m }
                }
            };
            context.Expenses.Add(expense);
            await context.SaveChangesAsync();

            var balanceCalculator = new BalanceCalculator(context);
            var settlementCalculator = new SettlementCalculator();
            var controller = new SettlementsController(context, balanceCalculator, settlementCalculator, NullLogger<SettlementsController>.Instance);

            // Verify initial suggestion: Bob pays Alice $100
            var initialResult = await controller.GetSuggestedSettlements(group.Id) as OkObjectResult;
            Assert.NotNull(initialResult);
            var initialSuggestions = Assert.IsAssignableFrom<IEnumerable<SuggestedSettlementDto>>(initialResult.Value).ToList();
            Assert.Single(initialSuggestions);
            Assert.Equal(bob.Id, initialSuggestions[0].FromUserId);
            Assert.Equal(alice.Id, initialSuggestions[0].ToUserId);
            Assert.Equal(100m, initialSuggestions[0].Amount);

            // 2. POST a partial settlement: Bob pays Alice $40
            var settlementDto = new CreateSettlementDto
            {
                PayerId = bob.Id,
                PayeeId = alice.Id,
                Amount = 40m
            };
            await controller.CreateSettlement(group.Id, settlementDto);

            // 3. Act: Get suggestions again
            var finalResult = await controller.GetSuggestedSettlements(group.Id) as OkObjectResult;
            
            // Assert
            Assert.NotNull(finalResult);
            var finalSuggestions = Assert.IsAssignableFrom<IEnumerable<SuggestedSettlementDto>>(finalResult.Value).ToList();
            
            // It should now suggest Bob owes Alice $60
            Assert.Single(finalSuggestions);
            Assert.Equal(bob.Id, finalSuggestions[0].FromUserId);
            Assert.Equal(alice.Id, finalSuggestions[0].ToUserId);
            Assert.Equal(60m, finalSuggestions[0].Amount);
        }
    }
}
