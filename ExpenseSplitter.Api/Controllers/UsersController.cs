using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.DTOs;
using ExpenseSplitter.Api.Exceptions;

namespace ExpenseSplitter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}/groups")]
        public async Task<IActionResult> GetUserGroups(Guid id)
        {
            var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (callerId == null || callerId != id.ToString())
            {
                return Forbid();
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
                throw new NotFoundException("User not found.");

            var groups = await _context.GroupMembers
                .Where(gm => gm.UserId == id && gm.LeftAt == null)
                .Select(gm => gm.Group)
                .Select(g => new GroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Currency = g.Currency
                })
                .ToListAsync();

            return Ok(groups);
        }
    }
}
