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
using ExpenseSplitter.Api.Services;
using ExpenseSplitter.Api.Filters;

namespace ExpenseSplitter.Api.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId}/[controller]")]
    [Authorize]
    [RequireGroupMember]
    public class RecurringExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRecurringExpenseGenerator _generator;

        public RecurringExpensesController(AppDbContext context, IRecurringExpenseGenerator generator)
        {
            _context = context;
            _generator = generator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid groupId, [FromBody] CreateRecurringExpenseDto dto)
        {
            if (dto.TotalAmount <= 0)
                throw new ValidationException("Amount must be positive.");

            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            var payerIsMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == dto.PayerId && gm.LeftAt == null);
            if (!payerIsMember)
                throw new ValidationException("Payer must be an active member of the group.");

            if (dto.SplitTypeId == SplitType.Template && !dto.SplitTemplateId.HasValue)
                throw new ValidationException("Template ID is required for Template split types.");

            if (dto.SplitTypeId != SplitType.Equal && dto.SplitTypeId != SplitType.Template)
                throw new ValidationException("Only Equal and Template split types are supported for recurring expenses.");

            var re = new RecurringExpense
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                PayerId = dto.PayerId,
                Description = dto.Description,
                TotalAmount = dto.TotalAmount,
                Category = dto.Category,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate,
                NextRunDate = dto.StartDate, // Next run is the first start date
                SplitTypeId = dto.SplitTypeId,
                SplitTemplateId = dto.SplitTemplateId
            };

            _context.RecurringExpenses.Add(re);
            await _context.SaveChangesAsync();

            return Ok(new RecurringExpenseDto
            {
                Id = re.Id,
                GroupId = re.GroupId,
                PayerId = re.PayerId,
                Description = re.Description,
                TotalAmount = re.TotalAmount,
                Category = re.Category,
                Frequency = re.Frequency,
                StartDate = re.StartDate,
                NextRunDate = re.NextRunDate,
                SplitTypeId = re.SplitTypeId,
                SplitTemplateId = re.SplitTemplateId
            });
        }

        [HttpGet("{id}/preview")]
        public async Task<IActionResult> PreviewNextCycle(Guid groupId, Guid id)
        {
            var re = await _context.RecurringExpenses
                .FirstOrDefaultAsync(r => r.Id == id && r.GroupId == groupId);

            if (re == null)
                throw new NotFoundException("Recurring expense not found.");

            // Use the shared generator to get the exact expense that would be created
            var expensePreview = await _generator.GenerateExpenseAsync(re);

            // Map to DTO
            var dto = new ExpenseDto
            {
                Id = Guid.Empty, // Not created yet
                GroupId = expensePreview.GroupId,
                PayerId = expensePreview.PayerId,
                Description = expensePreview.Description,
                TotalAmount = expensePreview.TotalAmount,
                Date = expensePreview.Date,
                Category = expensePreview.Category,
                Participants = expensePreview.Participants.Select(s => new ExpenseParticipantDto
                {
                    UserId = s.UserId,
                    OwedAmount = s.OwedAmount
                }).ToList()
            };

            return Ok(dto);
        }
    }
}
