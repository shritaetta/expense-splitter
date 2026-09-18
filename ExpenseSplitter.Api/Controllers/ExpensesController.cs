using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.DTOs;
using ExpenseSplitter.Api.Models;
using ExpenseSplitter.Api.Exceptions;
using ExpenseSplitter.Api.Filters;

namespace ExpenseSplitter.Api.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId}/[controller]")]
    [Authorize]
    [RequireGroupMember]
    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ExpenseSplitter.Api.Services.ISplitCalculator _calculator;
        private readonly ExpenseSplitter.Api.Services.IBalanceCalculator _balanceCalculator;

        public ExpensesController(AppDbContext context, ExpenseSplitter.Api.Services.ISplitCalculator calculator, ExpenseSplitter.Api.Services.IBalanceCalculator balanceCalculator)
        {
            _context = context;
            _calculator = calculator;
            _balanceCalculator = balanceCalculator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense(Guid groupId, [FromBody] CreateExpenseDto dto)
        {
            if (dto.TotalAmount <= 0)
                throw new ValidationException("Expense amount must be positive.");

            if (dto.Participants == null || !dto.Participants.Any())
                throw new ValidationException("At least one participant is required.");

            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            var payerIsMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == dto.PayerId && gm.LeftAt == null);
            if (!payerIsMember)
                throw new ValidationException("Payer must be an active member of the group.");

            var activeMembers = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.LeftAt == null)
                .Select(gm => gm.UserId)
                .ToListAsync();

            var participantIds = dto.Participants.Select(p => p.UserId).Distinct().ToList();
            var invalidParticipants = participantIds.Except(activeMembers).ToList();
            if (invalidParticipants.Any())
            {
                throw new ValidationException($"One or more participants are not active members of the group: {string.Join(", ", invalidParticipants)}");
            }

            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                PayerId = dto.PayerId,
                Description = dto.Description,
                TotalAmount = dto.TotalAmount,
                Date = dto.Date,
                Category = dto.Category,
                SplitTypeId = dto.SplitTypeId
            };

            var inputs = dto.Participants.DistinctBy(p => p.UserId).Select(p => new ExpenseSplitter.Api.Services.SplitParticipantInput
            {
                UserId = p.UserId,
                ShareValue = p.ShareValue,
                IsPayer = p.UserId == dto.PayerId
            }).ToList();

            var calculatedShares = _calculator.Calculate(dto.TotalAmount, dto.SplitTypeId, inputs);

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

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            var result = new ExpenseDto
            {
                Id = expense.Id,
                GroupId = expense.GroupId,
                PayerId = expense.PayerId,
                Description = expense.Description,
                TotalAmount = expense.TotalAmount,
                Date = expense.Date,
                Category = expense.Category,
                Participants = expense.Participants.Select(p => new ExpenseParticipantDto
                {
                    UserId = p.UserId,
                    OwedAmount = p.OwedAmount
                }).ToList()
            };

            return Ok(result);
        }

        [HttpGet("balances")]
        public async Task<IActionResult> GetBalances(Guid groupId)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            var balances = await _balanceCalculator.CalculateNetBalancesAsync(groupId);

            var balancesList = balances.Select(b => new
            {
                UserId = b.Key,
                Balance = b.Value
            }).ToList();

            return Ok(balancesList);
        }
    }
}
