using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitter.Api.DTOs
{
    public class RegisterDto
    {
        [Required, EmailAddress]
        public required string Email { get; set; }
        
        [Required]
        public required string Name { get; set; }
        
        [Required, MinLength(6)]
        public required string Password { get; set; }
    }

    public class LoginDto
    {
        [Required, EmailAddress]
        public required string Email { get; set; }
        
        [Required]
        public required string Password { get; set; }
    }

    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public required UserDto User { get; set; }
    }
}
