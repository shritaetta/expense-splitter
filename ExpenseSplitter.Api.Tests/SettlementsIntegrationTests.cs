using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.DTOs;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Tests
{
    public class SettlementsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SettlementsIntegrationTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "InMemoryDbForSettlementsTesting_" + Guid.NewGuid().ToString();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "JwtSettings:Secret", "VerySecureSecretKeyForTestingPurposesOnly!" },
                        { "JwtSettings:Issuer", "TestIssuer" },
                        { "JwtSettings:Audience", "TestAudience" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName);
                    });
                });
            });
        }

        [Fact]
        public async Task MarkAsPaid_EndToEnd_SimplifiesBalancesToZero()
        {
            var client = _factory.CreateClient();

            // 1. Setup Data in DB
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            var group = new Group { Id = Guid.NewGuid(), Name = "EndToEnd Group", Currency = "USD" };
            var alice = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice2@test.com" };
            var bob = new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob2@test.com" };

            db.Groups.Add(group);
            db.Users.AddRange(alice, bob);
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = alice.Id });
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = bob.Id });

            // Alice pays $60, Bob owes it all
            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                PayerId = alice.Id, // Alice paid
                TotalAmount = 60m,
                Description = "Lunch",
                Category = "Food",
                Date = DateTimeOffset.UtcNow,
                SplitTypeId = SplitType.ExactAmount,
                Participants = new List<ExpenseParticipant>
                {
                    new ExpenseParticipant { UserId = bob.Id, OwedAmount = 60m, ShareValue = 60m }
                }
            };
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            // Wait, we need authentication since the endpoints require [Authorize].
            // To bypass or mock auth, we can just fetch a real token using AuthController or create a mock.
            // Let's create a real token using AuthController.
            var registerRes = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto { Email = "alice2_auth@test.com", Password = "Password123!", Name = "Alice" });
            var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = "alice2_auth@test.com", Password = "Password123!" });
            var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authData.Token);
            
            // Re-assign Alice to the registered user to make sure we pass [RequireGroupMember]
            var actualAliceId = authData.User.Id;
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = actualAliceId });
            expense.PayerId = actualAliceId;
            await db.SaveChangesAsync();

            // 2. GET /balances Before
            var beforeBalancesRes = await client.GetAsync($"/api/groups/{group.Id}/expenses/balances");
            beforeBalancesRes.EnsureSuccessStatusCode();
            var beforeBalances = await beforeBalancesRes.Content.ReadFromJsonAsync<List<BalanceDto>>();

            var aliceBefore = beforeBalances.First(b => b.UserId == actualAliceId).Balance;
            var bobBefore = beforeBalances.First(b => b.UserId == bob.Id).Balance;

            Console.WriteLine($"\n--- BEFORE SETTLEMENT ---");
            Console.WriteLine($"Alice Balance: {aliceBefore}");
            Console.WriteLine($"Bob Balance: {bobBefore}");

            // Assert before balances
            Assert.Equal(60m, aliceBefore);
            Assert.Equal(-60m, bobBefore);

            // 3. POST Settlement (Bob pays Alice)
            var settlementPayload = new CreateSettlementDto
            {
                PayerId = bob.Id, // Debtor
                PayeeId = actualAliceId, // Creditor
                Amount = 60m
            };

            var postRes = await client.PostAsJsonAsync($"/api/groups/{group.Id}/settlements", settlementPayload);
            postRes.EnsureSuccessStatusCode();

            // 4. GET /balances After
            var afterBalancesRes = await client.GetAsync($"/api/groups/{group.Id}/expenses/balances");
            afterBalancesRes.EnsureSuccessStatusCode();
            var afterBalances = await afterBalancesRes.Content.ReadFromJsonAsync<List<BalanceDto>>();

            var aliceAfter = afterBalances.First(b => b.UserId == actualAliceId).Balance;
            var bobAfter = afterBalances.First(b => b.UserId == bob.Id).Balance;

            Console.WriteLine($"\n--- AFTER SETTLEMENT ---");
            Console.WriteLine($"Alice Balance: {aliceAfter}");
            Console.WriteLine($"Bob Balance: {bobAfter}\n");

            // Assert after balances
            Assert.Equal(0m, aliceAfter);
            Assert.Equal(0m, bobAfter);
        }

        private class BalanceDto
        {
            public Guid UserId { get; set; }
            public decimal Balance { get; set; }
        }
    }
}
