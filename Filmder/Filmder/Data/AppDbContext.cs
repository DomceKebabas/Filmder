using Microsoft.EntityFrameworkCore;
using Filmder.Models;
using Filmder.MovieParty;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Filmder.Data;

public class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<MovieScore> MovieScores { get; set; }
    public DbSet<SwipeHistory> SwipeHistories { get; set; }
    public DbSet<SharedMovie> SharedMovies { get; set; } 
    public DbSet<GuessRatingGame> RatingGuessingGames { get; set; }
    public DbSet<MovieRatingGuess> MovieRatingGuesses { get; set; }
    public DbSet<UserMovie> UserMovies { get; set; }
    public DbSet<PersonalityQuestion> PersonalityQuestions { get; set; }
    public DbSet<PersonalityAnswer> PersonalityAnswers { get; set; }
    public DbSet<PersonalityMatchResult> PersonalityMatchResults { get; set; }
    public DbSet<HigherLowerGame> HigherLowerGames { get; set; }
    public DbSet<HigherLowerGuess> HigherLowerGuesses { get; set; }
    public DbSet<WatchParty> WatchParties { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<WatchParty>(entity =>
        {
            entity.HasKey(wp => wp.Id);
    
            entity.HasOne(wp => wp.Group)
                .WithMany()
                .HasForeignKey(wp => wp.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
    
            entity.HasOne(wp => wp.Host)
                .WithMany()
                .HasForeignKey(wp => wp.HostUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    
        modelBuilder
            .Entity<Movie>()
            .Property(m => m.Genre)
            .HasConversion<string>();
        
        modelBuilder.Entity<UserMovie>()
            .HasKey(um => new { um.UserId, um.MovieId });
    
        modelBuilder.Entity<UserMovie>()
            .HasOne(um => um.User)
            .WithMany(u => u.UserMovies)
            .HasForeignKey(um => um.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    
        modelBuilder.Entity<UserMovie>()
            .HasOne(um => um.Movie)
            .WithMany(m => m.UserMovies)
            .HasForeignKey(um => um.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    
        modelBuilder.Entity<GuessRatingGame>()
            .HasMany(g => g.Movies)
            .WithMany()
            .UsingEntity(j => j.ToTable("GuessRatingGameMovies"));
    
        modelBuilder.Entity<GuessRatingGame>()
            .HasOne(g => g.Group)
            .WithMany()
            .HasForeignKey(g => g.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    
        modelBuilder.Entity<GuessRatingGame>()
            .HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    
        modelBuilder.Entity<GuessRatingGame>()
            .HasMany(g => g.Guesses)
            .WithOne(guess => guess.GuessRatingGame)
            .HasForeignKey(guess => guess.GuessRatingGameId)
            .OnDelete(DeleteBehavior.SetNull);
    
        modelBuilder.Entity<MovieRatingGuess>()
            .HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    
        modelBuilder.Entity<MovieRatingGuess>()
            .HasOne(g => g.Movie)
            .WithMany()
            .HasForeignKey(g => g.MovieId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<PersonalityQuestion>()
            .HasMany(pq => pq.Answers)
            .WithOne(pa => pa.Question)
            .HasForeignKey(pa => pa.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    
        modelBuilder.Entity<PersonalityAnswer>()
            .HasOne(pa => pa.User)
            .WithMany()
            .HasForeignKey(pa => pa.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    
        modelBuilder.Entity<PersonalityMatchResult>()
            .HasOne(pmr => pmr.User)
            .WithMany()
            .HasForeignKey(pmr => pmr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<HigherLowerGame>()
            .HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HigherLowerGame>()
            .HasOne(g => g.CurrentMovie)
            .WithMany()
            .HasForeignKey(g => g.CurrentMovieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HigherLowerGame>()
            .HasOne(g => g.NextMovie)
            .WithMany()
            .HasForeignKey(g => g.NextMovieId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HigherLowerGuess>()
            .HasOne(g => g.Game)
            .WithMany(g => g.Guesses)
            .HasForeignKey(g => g.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HigherLowerGuess>()
            .HasOne(g => g.Movie1)
            .WithMany()
            .HasForeignKey(g => g.Movie1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HigherLowerGuess>()
            .HasOne(g => g.Movie2)
            .WithMany()
            .HasForeignKey(g => g.Movie2Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}