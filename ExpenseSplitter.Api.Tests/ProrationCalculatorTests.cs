using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ExpenseSplitter.Api.Models;
using ExpenseSplitter.Api.Services;
using ExpenseSplitter.Api.Exceptions;

namespace ExpenseSplitter.Api.Tests
{
    public class ProrationCalculatorTests
    {
        private readonly IProrationCalculator _calculator = new ProrationCalculator();

        [Fact]
        public void Calculate_FullCycle_AllMembersPresent_BehavesLikeNormalSplit()
        {
            // Arrange
            // 30 days cycle, 2 users present full time. $100 total. Equal split.
            var start = new DateTimeOffset(2023, 11, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero); // 30 days

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var participants = new List<ProrationParticipantInput>
            {
                new ProrationParticipantInput { UserId = user1, IsPayer = true, JoinedAt = start.AddDays(-10) },
                new ProrationParticipantInput { UserId = user2, IsPayer = false, JoinedAt = start.AddDays(-5) }
            };

            // Act
            var results = _calculator.CalculateProratedShares(100m, SplitType.Equal, start, end, participants).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Equal(50m, results.First(r => r.UserId == user1).OwedAmount);
            Assert.Equal(50m, results.First(r => r.UserId == user2).OwedAmount);
        }

        [Fact]
        public void Calculate_HalfCycle_ProratesCorrectly()
        {
            // Arrange
            // 30 days cycle, $3000 total.
            // User1 present full 30 days. User2 present for first 15 days, then leaves.
            // Equal split mathematically means:
            // Days 1-15 (15 days): Daily cost $100. Split 50/50 -> User1 pays $50/day, User2 pays $50/day.
            // Days 16-30 (15 days): Daily cost $100. User1 pays $100/day. User2 pays $0.
            // Total: User1 = (15*50) + (15*100) = $2250. User2 = (15*50) = $750.
            
            var start = new DateTimeOffset(2023, 11, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var participants = new List<ProrationParticipantInput>
            {
                new ProrationParticipantInput { UserId = user1, JoinedAt = start },
                new ProrationParticipantInput { UserId = user2, JoinedAt = start, LeftAt = start.AddDays(15) }
            };

            // Act
            var results = _calculator.CalculateProratedShares(3000m, SplitType.Equal, start, end, participants).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Equal(2250m, results.Single(r => r.UserId == user1).OwedAmount);
            Assert.Equal(750m, results.Single(r => r.UserId == user2).OwedAmount);
        }

        [Fact]
        public void Calculate_EmptyDaysInCycle_SkipsAndDistributesToActiveDays()
        {
            // Arrange
            // 30 days cycle, $3000 total.
            // Days 1-10: EMPTY (no one active)
            // Days 11-30 (20 days): User1 active.
            // Expected: The 10 empty days are skipped. The 20 active days have daily cost of $3000 / 20 = $150.
            // User1 is the only one active, so they pay 100% of the $150 for 20 days = $3000.
            
            var start = new DateTimeOffset(2023, 11, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);

            var user1 = Guid.NewGuid();

            var participants = new List<ProrationParticipantInput>
            {
                new ProrationParticipantInput { UserId = user1, JoinedAt = start.AddDays(10) }
            };

            // Act
            var results = _calculator.CalculateProratedShares(3000m, SplitType.Equal, start, end, participants).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(3000m, results.Single().OwedAmount);
        }

        [Fact]
        public void Calculate_NoActiveMembersAtAll_ThrowsException()
        {
            // Arrange
            var start = new DateTimeOffset(2023, 11, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);

            // Left before cycle started
            var participants = new List<ProrationParticipantInput>
            {
                new ProrationParticipantInput { UserId = Guid.NewGuid(), JoinedAt = start.AddDays(-20), LeftAt = start.AddDays(-10) }
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => 
                _calculator.CalculateProratedShares(1000m, SplitType.Equal, start, end, participants));
            
            Assert.Contains("No members were active during this billing cycle", ex.Message);
        }

        [Fact]
        public void Calculate_TemplateSplit_ZeroWeight_Excluded()
        {
            // Arrange
            // 20 days total. $1000 cost.
            // User1 has weight 10. User2 has weight 0.
            // User1 pays all $1000.
            var start = new DateTimeOffset(2023, 11, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2023, 11, 21, 0, 0, 0, TimeSpan.Zero);

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var participants = new List<ProrationParticipantInput>
            {
                new ProrationParticipantInput { UserId = user1, JoinedAt = start, ShareValue = 10m },
                new ProrationParticipantInput { UserId = user2, JoinedAt = start, ShareValue = 0m }
            };

            // Act
            var results = _calculator.CalculateProratedShares(1000m, SplitType.Template, start, end, participants).ToList();

            // Assert
            Assert.Equal(1000m, results.Single(r => r.UserId == user1).OwedAmount);
            Assert.Equal(0m, results.Single(r => r.UserId == user2).OwedAmount);
        }
    }
}
