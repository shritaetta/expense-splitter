using System;
using System.Collections.Generic;

namespace ExpenseSplitter.Api.DTOs
{
    public class CreateGroupDto
    {
        public required string Name { get; set; }
        public required string Currency { get; set; }
    }

    public class GroupDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Currency { get; set; }
    }

    public class GroupMemberDto
    {
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
        public DateTimeOffset? LeftAt { get; set; }
    }

    public class AddGroupMemberDto
    {
        public Guid UserId { get; set; }
    }
}
