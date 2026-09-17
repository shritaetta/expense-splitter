using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitter.Api.Data;

namespace ExpenseSplitter.Api.Services
{
    public interface IBalanceCalculator
    {
        Task<Dictionary<Guid, decimal>> CalculateNetBalancesAsync(Guid groupId, CancellationToken cancellationToken = default);
    }

    public class BalanceCalculator : IBalanceCalculator
    {
        private readonly AppDbContext _context;

        public BalanceCalculator(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<Guid, decimal>> CalculateNetBalancesAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            var balances = new Dictionary<Guid, decimal>();

            // 1. Initialize balances for all group members
            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => gm.UserId)
                .ToListAsync(cancellationToken);

            foreach (var memberId in members)
            {
                balances[memberId] = 0m;
            }

            // 2. Fetch all expenses for this group
            var expenses = await _context.Expenses
                .Include(e => e.Participants)
                .Where(e => e.GroupId == groupId)
                .ToListAsync(cancellationToken);

            foreach (var expense in expenses)
            {
                if (!balances.ContainsKey(expense.PayerId))
                    balances[expense.PayerId] = 0m;

                decimal totalOwedToPayer = 0m;

                foreach (var participant in expense.Participants)
                {
                    if (!balances.ContainsKey(participant.UserId))
                        balances[participant.UserId] = 0m;

                    // The participant owes money, so their balance decreases
                    balances[participant.UserId] -= participant.OwedAmount;
                    
                    // Keep track of total owed to the payer for this expense
                    totalOwedToPayer += participant.OwedAmount;
                }

                // The payer is owed money, so their balance increases
                balances[expense.PayerId] += totalOwedToPayer;
            }

            // 3. Fetch all settlements for this group
            var settlements = await _context.Settlements
                .Where(s => s.GroupId == groupId)
                .ToListAsync(cancellationToken);

            foreach (var settlement in settlements)
            {
                if (!balances.ContainsKey(settlement.PayerId))
                    balances[settlement.PayerId] = 0m;
                if (!balances.ContainsKey(settlement.PayeeId))
                    balances[settlement.PayeeId] = 0m;

                // The payer PAID money, so their net balance goes UP (they owe less)
                balances[settlement.PayerId] += settlement.Amount;
                
                // The payee RECEIVED money, so their net balance goes DOWN (they are owed less)
                balances[settlement.PayeeId] -= settlement.Amount;
            }

            return balances;
        }
    }
}
