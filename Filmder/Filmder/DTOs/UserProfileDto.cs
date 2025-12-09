using System.ComponentModel.DataAnnotations;

namespace Filmder.DTOs;

public class UserProfileDto
{
    [Required]
    public required string Id { get; set; }
    [Required]
    public required string Username { get; set; }
    [Required]
    public required string Email { get; set; }
    public string? ProfilePictureUrl { get; set; }
}