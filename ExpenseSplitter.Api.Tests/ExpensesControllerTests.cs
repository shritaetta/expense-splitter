using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ExpenseSplitter.Api.Controllers;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.DTOs;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Tests
{
    public class ExpensesControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateExpense_WithPayerInParticipantIds_FiltersOutPayerFromExpenseParticipants()
        {
            // Arrange
            var context = GetDbContext();
            
            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", Currency = "USD" };
            var payer = new User { Id = Guid.NewGuid(), Name = "Payer", Email = "payer@test.com" };
            var debtor = new User { Id = Guid.NewGuid(), Name = "Debtor", Email = "debtor@test.com" };

            // Both users are active members
            context.Groups.Add(group);
            context.Users.AddRange(payer, debtor);
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = payer.Id, JoinedAt = DateTimeOffset.UtcNow });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = debtor.Id, JoinedAt = DateTimeOffset.UtcNow });
            await context.SaveChangesAsync();

            var controller = new ExpensesController(context, new ExpenseSplitter.Api.Services.SplitCalculator(), new ExpenseSplitter.Api.Services.BalanceCalculator(context));
            var dto = new CreateExpenseDto
            {
                PayerId = payer.Id,
                Description = "Dinner",
                TotalAmount = 100,
                Date = DateTimeOffset.UtcNow,
                Category = "Food",
                // BOTH payer and debtor are in the list
                Participants = new List<CreateExpenseParticipantDto>
                {
                    new CreateExpenseParticipantDto { UserId = payer.Id },
                    new CreateExpenseParticipantDto { UserId = debtor.Id }
                }
            };

            // Act
            var result = await controller.CreateExpense(group.Id, dto) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var returnedExpense = Assert.IsType<ExpenseDto>(result.Value);
            
            // Should only have 1 participant mapped (the debtor)
            Assert.Single(returnedExpense.Participants);
            Assert.Equal(debtor.Id, returnedExpense.Participants.First().UserId);

            // Double check the database directly
            var dbExpense = await context.Expenses
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == returnedExpense.Id);
                
            Assert.NotNull(dbExpense);
            Assert.Single(dbExpense.Participants);
            Assert.DoesNotContain(dbExpense.Participants, p => p.UserId == payer.Id);
        }
        [Fact]
        public async Task CreateExpense_PercentageSplit_FiltersOutPayer()
        {
            // Arrange
            var context = GetDbContext();
            
            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", Currency = "USD" };
            var payer = new User { Id = Guid.NewGuid(), Name = "Payer", Email = "payer@test.com" };
            var debtor1 = new User { Id = Guid.NewGuid(), Name = "Debtor1", Email = "d1@test.com" };
            var debtor2 = new User { Id = Guid.NewGuid(), Name = "Debtor2", Email = "d2@test.com" };

            context.Groups.Add(group);
            context.Users.AddRange(payer, debtor1, debtor2);
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = payer.Id, JoinedAt = DateTimeOffset.UtcNow });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = debtor1.Id, JoinedAt = DateTimeOffset.UtcNow });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = debtor2.Id, JoinedAt = DateTimeOffset.UtcNow });
            await context.SaveChangesAsync();

            var controller = new ExpensesController(context, new ExpenseSplitter.Api.Services.SplitCalculator(), new ExpenseSplitter.Api.Services.BalanceCalculator(context));
            var dto = new CreateExpenseDto
            {
                PayerId = payer.Id,
                Description = "Percentage Split",
                TotalAmount = 200,
                Date = DateTimeOffset.UtcNow,
                Category = "Food",
                SplitTypeId = SplitType.Percentage,
                Participants = new List<CreateExpenseParticipantDto>
                {
                    new CreateExpenseParticipantDto { UserId = payer.Id, ShareValue = 20 }, // Payer owes $40 implicitly
                    new CreateExpenseParticipantDto { UserId = debtor1.Id, ShareValue = 30 }, // Owes $60
                    new CreateExpenseParticipantDto { UserId = debtor2.Id, ShareValue = 50 }  // Owes $100
                }
            };

            // Act
            var result = await controller.CreateExpense(group.Id, dto) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var returnedExpense = Assert.IsType<ExpenseDto>(result.Value);
            
            // Should have 2 participants mapped (debtor1 and debtor2)
            Assert.Equal(2, returnedExpense.Participants.Count);
            Assert.DoesNotContain(returnedExpense.Participants, p => p.UserId == payer.Id);

            var dbExpense = await context.Expenses.Include(e => e.Participants).FirstOrDefaultAsync();
            Assert.Equal(2, dbExpense.Participants.Count);
            
            var dbDebtor1 = dbExpense.Participants.Single(p => p.UserId == debtor1.Id);
            Assert.Equal(60m, dbDebtor1.OwedAmount);

            var dbDebtor2 = dbExpense.Participants.Single(p => p.UserId == debtor2.Id);
            Assert.Equal(100m, dbDebtor2.OwedAmount);
        }

        [Fact]
        public async Task CreateExpense_ExactAmountSplit_FiltersOutPayer()
        {
            // Arrange
            var context = GetDbContext();
            
            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", Currency = "USD" };
            var payer = new User { Id = Guid.NewGuid(), Name = "Payer", Email = "payer@test.com" };
            var debtor = new User { Id = Guid.NewGuid(), Name = "Debtor", Email = "debtor@test.com" };

            context.Groups.Add(group);
            context.Users.AddRange(payer, debtor);
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = payer.Id, JoinedAt = DateTimeOffset.UtcNow });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = debtor.Id, JoinedAt = DateTimeOffset.UtcNow });
            await context.SaveChangesAsync();

            var controller = new ExpensesController(context, new ExpenseSplitter.Api.Services.SplitCalculator(), new ExpenseSplitter.Api.Services.BalanceCalculator(context));
            var dto = new CreateExpenseDto
            {
                PayerId = payer.Id,
                Description = "Exact Amount Split",
                TotalAmount = 75.50m,
                Date = DateTimeOffset.UtcNow,
                Category = "Food",
                SplitTypeId = SplitType.ExactAmount,
                Participants = new List<CreateExpenseParticipantDto>
                {
                    new CreateExpenseParticipantDto { UserId = payer.Id, ShareValue = 25.50m }, // Payer implicitly owes this
                    new CreateExpenseParticipantDto { UserId = debtor.Id, ShareValue = 50.00m } // Debtor owes this
                }
            };

            // Act
            var result = await controller.CreateExpense(group.Id, dto) as OkObjectResult;

            // Assert
            var dbExpense = await context.Expenses.Include(e => e.Participants).FirstOrDefaultAsync();
            Assert.Single(dbExpense.Participants); // Only debtor
            Assert.DoesNotContain(dbExpense.Participants, p => p.UserId == payer.Id);
            
            var dbDebtor = dbExpense.Participants.Single();
            Assert.Equal(debtor.Id, dbDebtor.UserId);
            Assert.Equal(50.00m, dbDebtor.OwedAmount);
        }
        [Fact]
        public async Task GetBalances_AfterPercentageSplit_CalculatesCorrectNetBalances()
        {
            // Arrange
            var context = GetDbContext();
            
            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", Currency = "USD" };
            var payer = new User { Id = Guid.NewGuid(), Name = "Payer", Email = "payer@test.com" };
            var debtor1 = new User { Id = Guid.NewGuid(), Name = "Debtor1", Email = "d1@test.com" };
            var debtor2 = new User { Id = Guid.NewGuid(), Name = "Debtor2", Email = "d2@test.com" };

            context.Groups.Add(group);
            context.Users.AddRange(payer, debtor1, debtor2);
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = payer.Id, JoinedAt = DateTimeOffset.UtcNow });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = debtor1.Id, JoinedAt = DateTimeOffset.UtcNow });
            context.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = debtor2.Id, JoinedAt = DateTimeOffset.UtcNow });
            await context.SaveChangesAsync();

            var controller = new ExpensesController(context, new ExpenseSplitter.Api.Services.SplitCalculator(), new ExpenseSplitter.Api.Services.BalanceCalculator(context));
            
            // Create the $200 expense (20% payer, 30% d1, 50% d2)
            var dto = new CreateExpenseDto
            {
                PayerId = payer.Id,
                Description = "Percentage Split",
                TotalAmount = 200,
                Date = DateTimeOffset.UtcNow,
                Category = "Food",
                SplitTypeId = SplitType.Percentage,
                Participants = new List<CreateExpenseParticipantDto>
                {
                    new CreateExpenseParticipantDto { UserId = payer.Id, ShareValue = 20 },
                    new CreateExpenseParticipantDto { UserId = debtor1.Id, ShareValue = 30 },
                    new CreateExpenseParticipantDto { UserId = debtor2.Id, ShareValue = 50 }
                }
            };
            await controller.CreateExpense(group.Id, dto);

            // Act
            var result = await controller.GetBalances(group.Id) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            
            // Result is an anonymous type, let's use dynamic or just serialize/deserialize to verify
            var balancesJson = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var balances = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(balancesJson);
            
            Assert.NotNull(balances);
            
            decimal payerBalance = 0;
            decimal debtor1Balance = 0;
            decimal debtor2Balance = 0;
            
            foreach(var item in balances)
            {
                var id = item.GetProperty("UserId").GetGuid();
                var bal = item.GetProperty("Balance").GetDecimal();
                if (id == payer.Id) payerBalance = bal;
                else if (id == debtor1.Id) debtor1Balance = bal;
                else if (id == debtor2.Id) debtor2Balance = bal;
            }

            // The payer is owed 160 (since they owe 40 themselves, 200 - 40 = 160)
            Assert.Equal(160m, payerBalance);
            // Debtors owe 60 and 100
            Assert.Equal(-60m, debtor1Balance);
            Assert.Equal(-100m, debtor2Balance);
            
            // Sum of all balances must be exactly 0
            Assert.Equal(0m, payerBalance + debtor1Balance + debtor2Balance);
        }
    }
}
