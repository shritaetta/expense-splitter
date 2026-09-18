using System;

namespace ExpenseSplitter.Api.DTOs
{
    public class CreateUserDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
    }
}
