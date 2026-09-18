using System;
using System.Linq;
using System.Security.Claims;
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
    [Route("api/[controller]")]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GroupsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized();
            }

            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Currency = dto.Currency
            };

            var groupMember = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow
            };

            _context.Groups.Add(group);
            _context.GroupMembers.Add(groupMember);
            await _context.SaveChangesAsync();

            return Ok(new GroupDto { Id = group.Id, Name = group.Name, Currency = group.Currency });
        }

        [HttpGet("{groupId}/members")]
        [RequireGroupMember]
        public async Task<IActionResult> GetGroupMembers(Guid groupId)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            var members = await _context.GroupMembers
                .Include(gm => gm.User)
                .Where(gm => gm.GroupId == groupId && gm.LeftAt == null)
                .Select(gm => new GroupMemberDto
                {
                    GroupId = gm.GroupId,
                    UserId = gm.UserId,
                    UserName = gm.User.Name,
                    JoinedAt = gm.JoinedAt,
                    LeftAt = gm.LeftAt
                })
                .ToListAsync();

            return Ok(members);
        }

        [HttpPost("{groupId}/members")]
        [RequireGroupMember]
        public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AddGroupMemberDto dto)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists)
                throw new NotFoundException("Group not found.");

            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                throw new NotFoundException("User not found.");

            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == dto.UserId);

            if (existingMember != null)
            {
                if (existingMember.LeftAt == null)
                    throw new ValidationException("User is already an active member of this group.");
                
                // User rejoining
                existingMember.LeftAt = null;
                existingMember.JoinedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var newMember = new GroupMember
                {
                    GroupId = groupId,
                    UserId = dto.UserId,
                    JoinedAt = DateTimeOffset.UtcNow
                };
                _context.GroupMembers.Add(newMember);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{groupId}/members/{userId}")]
        [RequireGroupMember]
        public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.LeftAt == null);

            if (member == null)
                throw new NotFoundException("Active group member not found.");

            member.LeftAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
