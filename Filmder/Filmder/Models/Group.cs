using System.ComponentModel.DataAnnotations;

namespace Filmder.Models;

public class Group
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string OwnerId { get; set; } = string.Empty;
    
    public AppUser Owner { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<AppUser> Members { get; set; } = new List<AppUser>();
    public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<SharedMovie> GroupMovie { get; set; } = new List<SharedMovie>();
}