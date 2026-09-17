using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseSplitter.Api.DTOs;

namespace ExpenseSplitter.Api.Services
{
    public interface ISettlementCalculator
    {
        List<SuggestedSettlementDto> CalculateSettlements(Dictionary<Guid, decimal> netBalances);
    }

    public class SettlementCalculator : ISettlementCalculator
    {
        // Algorithm Time Complexity: O(N log N)
        // 1. Splitting into creditors and debtors takes O(N).
        // 2. Sorting both lists takes O(N log N).
        // 3. The while loop runs at most N times because at each step at least one balance becomes exactly zero (or both). Thus the loop is O(N).
        // Overall: O(N) + O(N log N) + O(N) = O(N log N), where N is the number of participants.
        public List<SuggestedSettlementDto> CalculateSettlements(Dictionary<Guid, decimal> netBalances)
        {
            var results = new List<SuggestedSettlementDto>();

            // Convert to a mutable list of records so we can modify balances
            var creditors = netBalances
                .Where(b => b.Value > 0.005m) // Ignore tiny rounding fractions
                .Select(b => new { UserId = b.Key, Balance = b.Value })
                .OrderByDescending(b => b.Balance)
                .Select(b => new UserBalance { UserId = b.UserId, Balance = b.Balance })
                .ToList();

            var debtors = netBalances
                .Where(b => b.Value < -0.005m)
                .Select(b => new { UserId = b.Key, Balance = Math.Abs(b.Value) }) // Use absolute value for easier comparison
                .OrderByDescending(b => b.Balance)
                .Select(b => new UserBalance { UserId = b.UserId, Balance = b.Balance })
                .ToList();

            int i = 0; // Creditor index
            int j = 0; // Debtor index

            while (i < creditors.Count && j < debtors.Count)
            {
                var creditor = creditors[i];
                var debtor = debtors[j];

                // The amount to settle is the minimum of what the debtor owes and what the creditor is owed
                decimal amountToSettle = Math.Min(creditor.Balance, debtor.Balance);

                // Round to 2 decimal places to avoid floating point precision issues cascading
                amountToSettle = Math.Round(amountToSettle, 2);

                results.Add(new SuggestedSettlementDto
                {
                    FromUserId = debtor.UserId,
                    ToUserId = creditor.UserId,
                    Amount = amountToSettle
                });

                creditor.Balance -= amountToSettle;
                debtor.Balance -= amountToSettle;

                // Move to next creditor if this one is fully paid off
                if (creditor.Balance <= 0.005m) i++;
                
                // Move to next debtor if this one has fully paid their debt
                if (debtor.Balance <= 0.005m) j++;
            }

            return results;
        }

        private class UserBalance
        {
            public Guid UserId { get; set; }
            public decimal Balance { get; set; }
        }
    }
}
