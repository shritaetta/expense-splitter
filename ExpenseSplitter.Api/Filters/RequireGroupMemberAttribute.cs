using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitter.Api.Data;

namespace ExpenseSplitter.Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireGroupMemberAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var dbContext = context.HttpContext.RequestServices.GetService(typeof(AppDbContext)) as AppDbContext;
            if (dbContext == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var userIdString = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (context.RouteData.Values.TryGetValue("groupId", out var groupIdObj) && 
                Guid.TryParse(groupIdObj?.ToString(), out Guid groupId))
            {
                var isMember = await dbContext.GroupMembers
                    .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.LeftAt == null);

                if (!isMember)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            await next();
        }
    }
}
