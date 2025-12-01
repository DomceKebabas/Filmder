using System.ComponentModel.DataAnnotations;

namespace Filmder.DTOs;

public class RegisterDto
{
    [Required]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [MaxLength(20, ErrorMessage = "Username cannot exceed 20 characters")]
    public string Username { get; set; } = "";
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";
    
    [Required]
    [MinLength(4)]
    public string Password { get; set; } = "";
}