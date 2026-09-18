using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    public class SettlementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBalanceCalculator _balanceCalculator;
        private readonly ISettlementCalculator _settlementCalculator;
        private readonly ILogger<SettlementsController> _logger;

        public SettlementsController(AppDbContext context, IBalanceCalculator balanceCalculator, ISettlementCalculator settlementCalculator, ILogger<SettlementsController> logger)
        {
            _context = context;
            _balanceCalculator = balanceCalculator;
            _settlementCalculator = settlementCalculator;
            _logger = logger;
        }

        [HttpGet("suggested")]
        public async Task<IActionResult> GetSuggestedSettlements(Guid groupId)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            // 1. Get net balances (already accounts for past expenses and past settlements)
            var balances = await _balanceCalculator.CalculateNetBalancesAsync(groupId);

            // 2. Run simplification algorithm
            var suggested = _settlementCalculator.CalculateSettlements(balances);

            return Ok(suggested);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSettlement(Guid groupId, [FromBody] CreateSettlementDto dto)
        {
            if (dto.Amount <= 0)
                throw new ValidationException("Settlement amount must be positive.");

            if (dto.PayerId == dto.PayeeId)
                throw new ValidationException("Payer and Payee cannot be the same person.");

            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            // Validate members are in group
            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && (gm.UserId == dto.PayerId || gm.UserId == dto.PayeeId))
                .Select(gm => gm.UserId)
                .ToListAsync();

            if (!members.Contains(dto.PayerId))
                throw new ValidationException("Payer is not a member of the group.");
            if (!members.Contains(dto.PayeeId))
                throw new ValidationException("Payee is not a member of the group.");

            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                PayerId = dto.PayerId,
                PayeeId = dto.PayeeId,
                Amount = dto.Amount,
                Date = DateTimeOffset.UtcNow
            };

            _context.Settlements.Add(settlement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Settlement of {Amount} marked as paid from {PayerId} to {PayeeId} in group {GroupId}", settlement.Amount, settlement.PayerId, settlement.PayeeId, groupId);

            return Ok(new SettlementDto
            {
                Id = settlement.Id,
                GroupId = settlement.GroupId,
                PayerId = settlement.PayerId,
                PayeeId = settlement.PayeeId,
                Amount = settlement.Amount,
                Date = settlement.Date
            });
        }
    }
}
