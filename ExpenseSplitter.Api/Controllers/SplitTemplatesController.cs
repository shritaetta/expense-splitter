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
    public class SplitTemplatesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SplitTemplatesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSplitTemplates(Guid groupId)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            var templates = await _context.SplitTemplates
                .Include(st => st.TemplateItems)
                .Where(st => st.GroupId == groupId)
                .Select(st => new SplitTemplateDto
                {
                    Id = st.Id,
                    GroupId = st.GroupId,
                    Name = st.Name,
                    Items = st.TemplateItems.Select(ti => new SplitTemplateItemDto
                    {
                        UserId = ti.UserId,
                        ShareValue = ti.ShareValue
                    }).ToList()
                })
                .ToListAsync();

            return Ok(templates);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSplitTemplate(Guid groupId, [FromBody] CreateSplitTemplateDto dto)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            if (dto.Items == null || !dto.Items.Any())
                throw new ValidationException("At least one template item is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Template name is required.");

            // Verify users are in group
            var activeMembers = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.LeftAt == null)
                .Select(gm => gm.UserId)
                .ToListAsync();

            var participantIds = dto.Items.Select(i => i.UserId).Distinct().ToList();
            var invalidParticipants = participantIds.Except(activeMembers).ToList();
            if (invalidParticipants.Any())
            {
                throw new ValidationException($"One or more users are not active members of the group: {string.Join(", ", invalidParticipants)}");
            }

            // NOTE: We explicitly DO NOT validate that ShareValue sums to 100 here!
            // Templates use CalculateWeighted which supports arbitrary weights.
            if (dto.Items.Any(i => i.ShareValue <= 0))
                throw new ValidationException("Share values must be positive.");

            var template = new SplitTemplate
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Name = dto.Name
            };

            foreach (var item in dto.Items)
            {
                template.TemplateItems.Add(new SplitTemplateItem
                {
                    TemplateId = template.Id,
                    UserId = item.UserId,
                    ShareValue = item.ShareValue
                });
            }

            _context.SplitTemplates.Add(template);
            await _context.SaveChangesAsync();

            var result = new SplitTemplateDto
            {
                Id = template.Id,
                GroupId = template.GroupId,
                Name = template.Name,
                Items = template.TemplateItems.Select(ti => new SplitTemplateItemDto
                {
                    UserId = ti.UserId,
                    ShareValue = ti.ShareValue
                }).ToList()
            };

            return Ok(result);
        }
    }
}
