using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ExpenseSplitter.Api.Services;

namespace ExpenseSplitter.Api.Tests
{
    public class SettlementCalculatorTests
    {
        private readonly ISettlementCalculator _calculator = new SettlementCalculator();

        [Fact]
        public void CalculateSettlements_PerfectMatch_SimplifiesToZero()
        {
            // Arrange
            var userA = Guid.NewGuid(); // Owes 100
            var userB = Guid.NewGuid(); // Owed 100

            var balances = new Dictionary<Guid, decimal>
            {
                { userA, -100m },
                { userB, 100m }
            };

            // Act
            var settlements = _calculator.CalculateSettlements(balances);

            // Assert
            Assert.Single(settlements);
            var s = settlements.First();
            Assert.Equal(userA, s.FromUserId);
            Assert.Equal(userB, s.ToUserId);
            Assert.Equal(100m, s.Amount);
        }

        [Fact]
        public void CalculateSettlements_ComplexChain_SimplifiesCorrectly()
        {
            // Arrange
            var A = Guid.NewGuid(); 
            var B = Guid.NewGuid(); 
            var C = Guid.NewGuid(); 
            var D = Guid.NewGuid(); 

            // Net balances: 
            // A: +150
            // B: +50
            // C: -80
            // D: -120
            // Sum = 0

            var balances = new Dictionary<Guid, decimal>
            {
                { A, 150m },
                { B, 50m },
                { C, -80m },
                { D, -120m }
            };

            // Act
            var settlements = _calculator.CalculateSettlements(balances);

            // Assert
            // Largest creditor A (+150), largest debtor D (-120).
            // D pays A 120. A's remaining balance is +30. D's is 0.
            // Next largest creditor B (+50), next debtor C (-80).
            // Wait, creditors: A(+30), B(+50) -> B is now largest creditor (+50)
            // Debtors: C(-80).
            // C pays B 50. B's balance is 0. C's is -30.
            // Next creditors: A(+30). Debtors: C(-30).
            // C pays A 30.
            // Total transactions: 3

            Assert.Equal(3, settlements.Count);

            // D pays A 120
            Assert.Contains(settlements, s => s.FromUserId == D && s.ToUserId == A && s.Amount == 120m);
            // C pays B 50
            Assert.Contains(settlements, s => s.FromUserId == C && s.ToUserId == B && s.Amount == 50m);
            // C pays A 30
            Assert.Contains(settlements, s => s.FromUserId == C && s.ToUserId == A && s.Amount == 30m);
        }

        [Fact]
        public void CalculateSettlements_IgnoresZeroBalances()
        {
            // Arrange
            var A = Guid.NewGuid();
            var B = Guid.NewGuid();
            var C = Guid.NewGuid(); // 0 balance

            var balances = new Dictionary<Guid, decimal>
            {
                { A, 50m },
                { B, -50m },
                { C, 0m }
            };

            // Act
            var settlements = _calculator.CalculateSettlements(balances);

            // Assert
            Assert.Single(settlements);
            var s = settlements.First();
            Assert.Equal(B, s.FromUserId);
            Assert.Equal(A, s.ToUserId);
            Assert.Equal(50m, s.Amount);
            
            Assert.DoesNotContain(settlements, s => s.FromUserId == C || s.ToUserId == C);
        }
    }
}
