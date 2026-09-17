using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Services
{
    public interface IRecurringExpenseGenerator
    {
        Task<Expense> GenerateExpenseAsync(RecurringExpense recurringExpense, CancellationToken cancellationToken = default);
    }

    public class RecurringExpenseGenerator : IRecurringExpenseGenerator
    {
        private readonly AppDbContext _context;
        private readonly IProrationCalculator _prorationCalculator;

        public RecurringExpenseGenerator(AppDbContext context, IProrationCalculator prorationCalculator)
        {
            _context = context;
            _prorationCalculator = prorationCalculator;
        }

        public async Task<Expense> GenerateExpenseAsync(RecurringExpense re, CancellationToken cancellationToken = default)
        {
            var cycleStart = re.NextRunDate;
            var cycleEnd = re.Frequency == "Monthly" ? cycleStart.AddMonths(1) : cycleStart.AddDays(30);

            var groupMembers = await _context.GroupMembers
                .Where(gm => gm.GroupId == re.GroupId)
                .ToListAsync(cancellationToken);

            var templateItems = re.SplitTypeId == SplitType.Template && re.SplitTemplateId.HasValue
                ? await _context.SplitTemplateItems
                    .Where(sti => sti.TemplateId == re.SplitTemplateId.Value)
                    .ToDictionaryAsync(sti => sti.UserId, sti => sti.ShareValue, cancellationToken)
                : new System.Collections.Generic.Dictionary<Guid, decimal>();

            var inputs = groupMembers.Select(gm => new ProrationParticipantInput
            {
                UserId = gm.UserId,
                IsPayer = gm.UserId == re.PayerId,
                JoinedAt = gm.JoinedAt,
                LeftAt = gm.LeftAt,
                ShareValue = re.SplitTypeId == SplitType.Template 
                    ? (templateItems.ContainsKey(gm.UserId) ? templateItems[gm.UserId] : 0m)
                    : (re.SplitTypeId == SplitType.Equal ? null : (decimal?)null)
            }).ToList();

            var calculatedShares = _prorationCalculator.CalculateProratedShares(
                re.TotalAmount,
                re.SplitTypeId,
                cycleStart,
                cycleEnd,
                inputs
            );

            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                GroupId = re.GroupId,
                PayerId = re.PayerId,
                Description = $"{re.Description} ({cycleStart:MMM yyyy})",
                TotalAmount = re.TotalAmount,
                Date = DateTimeOffset.UtcNow,
                Category = re.Category,
                SplitTypeId = re.SplitTypeId,
                RecurringExpenseId = re.Id
            };

            foreach (var share in calculatedShares.Where(s => !s.IsPayer))
            {
                expense.Participants.Add(new ExpenseParticipant
                {
                    ExpenseId = expense.Id,
                    UserId = share.UserId,
                    OwedAmount = share.OwedAmount,
                    ShareValue = inputs.First(i => i.UserId == share.UserId).ShareValue
                });
            }

            return expense;
        }
    }
}
