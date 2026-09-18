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

        [Fact]
        public void Calculate_ExactAmount_DistributesExactlyAsRequested()
        {
            // Arrange
            decimal totalAmount = 150m;
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var participants = new List<SplitParticipantInput>
            {
                new SplitParticipantInput { UserId = user1, ShareValue = 50m },
                new SplitParticipantInput { UserId = user2, ShareValue = 100m }
            };

            // Act
            var results = _calculator.Calculate(totalAmount, SplitType.ExactAmount, participants).ToList();

            // Assert
            Assert.Equal(50m, results.Single(r => r.UserId == user1).OwedAmount);
            Assert.Equal(100m, results.Single(r => r.UserId == user2).OwedAmount);
            Assert.Equal(150m, results.Sum(r => r.OwedAmount));
        }

        [Fact]
        public void Calculate_ExactAmount_ThrowsIfSumDoesNotMatchTotal()
        {
            // Arrange
            decimal totalAmount = 150m;
            var participants = new List<SplitParticipantInput>
            {
                new SplitParticipantInput { UserId = Guid.NewGuid(), ShareValue = 50m },
                new SplitParticipantInput { UserId = Guid.NewGuid(), ShareValue = 50m } // Sums to 100, not 150
            };

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() =>
                _calculator.Calculate(totalAmount, SplitType.ExactAmount, participants));
            
            Assert.Contains("Sum of exact amounts", ex.Message);
        }

        [Fact]
        public void Calculate_Template_DistributesByArbitraryWeights()
        {
            // Arrange
            decimal totalAmount = 300m;
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var user3 = Guid.NewGuid();

            var participants = new List<SplitParticipantInput>
            {
                new SplitParticipantInput { UserId = user1, ShareValue = 1m }, // 1 part
                new SplitParticipantInput { UserId = user2, ShareValue = 2m }, // 2 parts
                new SplitParticipantInput { UserId = user3, ShareValue = 3m }  // 3 parts
                // Total = 6 parts. 300 / 6 = 50 per part
            };

            // Act
            var results = _calculator.Calculate(totalAmount, SplitType.Template, participants).ToList();

            // Assert
            // Since rounding could give a penny to the first user by Guid, we just verify the sum and roughly the amounts
            var r1 = results.Single(r => r.UserId == user1).OwedAmount;
            var r2 = results.Single(r => r.UserId == user2).OwedAmount;
            var r3 = results.Single(r => r.UserId == user3).OwedAmount;
            
            Assert.True(Math.Abs(50m - r1) <= 0.01m);
            Assert.True(Math.Abs(100m - r2) <= 0.01m);
            Assert.True(Math.Abs(150m - r3) <= 0.01m);
            Assert.Equal(300m, results.Sum(r => r.OwedAmount));
        }
    }
}
