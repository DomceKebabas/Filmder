using Filmder.Data;
using Filmder.DTOs;
using Filmder.Interfaces;
using Filmder.Models;
using Microsoft.EntityFrameworkCore;

namespace Filmder.Services;

public class MoodRepository(AppDbContext context) : IMoodRepository
{
    public async Task<MoodMovieResponseDto> GetMovieByMoodAsync(MoodDto moodDto)
    {
        if (!Enum.TryParse<UserMood>(moodDto.Mood, true, out var parsedMood))
        {
            throw new Exception(
                "Invalid mood. Available moods: Happy, Sad, Excited, Relaxed, Scared, Romantic, Thoughtful, Adventurous, Nostalgic, Energetic"
            );
        }

        var (genres, description) = GetGenresForMood(parsedMood);

        var query = context.Movies
            .Where(m => genres.Contains(m.Genre))
            .OrderByDescending(m => m.Rating);

        var totalMovies = await query.CountAsync();
        if (totalMovies == 0)
            throw new Exception("No movies found for this mood.");

        var random = new Random();
        var randomSkip = random.Next(0, totalMovies);

        var movie = await query
            .Skip(randomSkip)
            .FirstOrDefaultAsync();

        if (movie == null)
            throw new Exception("No movies found.");

        return new MoodMovieResponseDto
        {
            Mood = parsedMood.ToString(),
            Description = description,
            RecommendedGenres = genres.Select(g => g.ToString()).ToList(),
            Movie = movie
        };
    }

    public List<object> GetAvailableMoods()
    {
        return Enum.GetValues<UserMood>()
            .Select(mood =>
            {
                var (genres, description) = GetGenresForMood(mood);
                return new
                {
                    Mood = mood.ToString(),
                    Description = description,
                    Genres = genres.Select(g => g.ToString()).ToList()
                };
            })
            .Cast<object>()
            .ToList();
    }

    private static (List<MovieGenre> genres, string description) GetGenresForMood(UserMood mood)
    {
        return mood switch
        {
            UserMood.Happy => (
                new List<MovieGenre>
                {
                    MovieGenre.Comedy,
                    MovieGenre.Animation,
                    MovieGenre.Family,
                    MovieGenre.Musical
                },
                "Feeling happy? Here's something fun and uplifting!"
            ),
            UserMood.Sad => (
                new List<MovieGenre>
                {
                    MovieGenre.Drama,
                    MovieGenre.Romance
                },
                "Sometimes we need a good emotional release. Here's something touching."
            ),
            UserMood.Excited => (
                new List<MovieGenre>
                {
                    MovieGenre.Action,
                    MovieGenre.Adventure,
                    MovieGenre.SciFi,
                    MovieGenre.Superhero
                },
                "Ready for excitement? Here's an adrenaline-pumping adventure!"
            ),
            UserMood.Relaxed => (
                new List<MovieGenre>
                {
                    MovieGenre.Documentary,
                    MovieGenre.Biography,
                    MovieGenre.Drama
                },
                "Time to unwind. Here's something calm and engaging."
            ),
            UserMood.Scared => (
                new List<MovieGenre>
                {
                    MovieGenre.Horror,
                    MovieGenre.Thriller,
                    MovieGenre.Mystery
                },
                "Want some thrills? Here's something to get your heart racing!"
            ),
            UserMood.Romantic => (
                new List<MovieGenre>
                {
                    MovieGenre.Romance,
                    MovieGenre.Drama
                },
                "In the mood for love? Here's a romantic pick for you."
            ),
            UserMood.Thoughtful => (
                new List<MovieGenre>
                {
                    MovieGenre.Biography,
                    MovieGenre.Documentary,
                    MovieGenre.Drama,
                    MovieGenre.History
                },
                "Feeling contemplative? Here's something thought-provoking."
            ),
            UserMood.Adventurous => (
                new List<MovieGenre>
                {
                    MovieGenre.Adventure,
                    MovieGenre.Action,
                    MovieGenre.Fantasy
                },
                "Seeking adventure? Here's an epic journey for you!"
            ),
            UserMood.Nostalgic => (
                new List<MovieGenre>
                {
                    MovieGenre.Drama,
                    MovieGenre.History,
                    MovieGenre.War,
                    MovieGenre.Biography
                },
                "Feeling nostalgic? Here's something from the past."
            ),
            UserMood.Energetic => (
                new List<MovieGenre>
                {
                    MovieGenre.Action,
                    MovieGenre.Superhero,
                    MovieGenre.Sport,
                    MovieGenre.Adventure
                },
                "Bursting with energy? Here's something high-octane!"
            ),
            _ => (
                new List<MovieGenre> { MovieGenre.Drama },
                "Here's a great movie for you!"
            )
        };
    }
}
