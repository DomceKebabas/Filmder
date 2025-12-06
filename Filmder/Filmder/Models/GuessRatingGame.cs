using System.ComponentModel.DataAnnotations;

namespace Filmder.Models;

public class GuessRatingGame
{
    public int Id { get; set; }
    
    [Required]
    public int GroupId { get; set; }
    public Group Group { get; set; }
    
    [Required]
    public string UserId { get; set; }
    public AppUser User { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int TotalMovies { get; set; }     
    public DateTime ExpiresAt { get; set; }

    public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    
    public ICollection<MovieRatingGuess> Guesses { get; set; } = new List<MovieRatingGuess>();
}