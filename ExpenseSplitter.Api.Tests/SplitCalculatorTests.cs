using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ExpenseSplitter.Api.Models;
using ExpenseSplitter.Api.Services;
using ExpenseSplitter.Api.Exceptions;

namespace ExpenseSplitter.Api.Tests
{
    public class SplitCalculatorTests
    {
        private readonly ISplitCalculator _calculator = new SplitCalculator();

        [Fact]
        public void Calculate_EqualSplit_WithRemainder_DistributesCentsDeterministically()
        {
            // Arrange
            // $100 split 3 ways is $33.33 each, with 1 penny left over
            decimal totalAmount = 100m;
            
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var user3 = Guid.NewGuid();

            // Order doesn't matter for the input, the calculator sorts by UserId
            var participants = new List<SplitParticipantInput>
            {
                new SplitParticipantInput { UserId = user3, IsPayer = false },
                new SplitParticipantInput { UserId = user1, IsPayer = true },
                new SplitParticipantInput { UserId = user2, IsPayer = false }
            };

            // Act
            var results = _calculator.Calculate(totalAmount, SplitType.Equal, participants).ToList();

            // Assert
            var sortedUserIds = participants.Select(p => p.UserId).OrderBy(id => id).ToList();
            var firstUser = sortedUserIds[0];

            Assert.Equal(3, results.Count);
            
            // The first user by Guid sorting gets the extra penny ($33.34)
            var shareForFirst = results.Single(r => r.UserId == firstUser);
            Assert.Equal(33.34m, shareForFirst.OwedAmount);

            // The rest get $33.33
            foreach (var share in results.Where(r => r.UserId != firstUser))
            {
                Assert.Equal(33.33m, share.OwedAmount);
            }
            
            // Sum should exactly equal the original total
            Assert.Equal(100m, results.Sum(r => r.OwedAmount));
        }

        [Fact]
        public void Calculate_PercentageSplit_DistributesAmountsCorrectly()
        {
            // Arrange
            decimal totalAmount = 200m; // 20%, 30%, 50%
            
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var user3 = Guid.NewGuid();

            var participants = new List<SplitParticipantInput>
            {
                new SplitParticipantInput { UserId = user1, ShareValue = 20m },
                new SplitParticipantInput { UserId = user2, ShareValue = 30m },
                new SplitParticipantInput { UserId = user3, ShareValue = 50m }
            };

            // Act
            var results = _calculator.Calculate(totalAmount, SplitType.Percentage, participants).ToList();

            // Assert
            Assert.Equal(40m, results.Single(r => r.UserId == user1).OwedAmount);
            Assert.Equal(60m, results.Single(r => r.UserId == user2).OwedAmount);
            Assert.Equal(100m, results.Single(r => r.UserId == user3).OwedAmount);
            Assert.Equal(200m, results.Sum(r => r.OwedAmount));
        }

        [Fact]
        public void Calculate_PercentageSplit_ThrowsIfSumIsNot100()
        {
            // Arrange
            decimal totalAmount = 100m;
            var participants = new List<SplitParticipantInput>
            {
                new SplitParticipantInput { UserId = Guid.NewGuid(), ShareValue = 50m },
                new SplitParticipantInput { UserId = Guid.NewGuid(), ShareValue = 40m } // Sums to 90
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => 
                _calculator.Calculate(totalAmount, SplitType.Percentage, participants));
            
            Assert.Contains("Total percentage must equal 100", ex.Message);
        }
    }
}
