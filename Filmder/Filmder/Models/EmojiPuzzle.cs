using System.ComponentModel.DataAnnotations;

namespace Filmder.Models;

public class EmojiPuzzle
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Movie { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Emoji { get; set; } = string.Empty;

    [Required]
    public Difficulty Difficulty { get; set; }

    [Required]
    [StringLength(200)]
    public string Option1 { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Option2 { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Option3 { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Option4 { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}