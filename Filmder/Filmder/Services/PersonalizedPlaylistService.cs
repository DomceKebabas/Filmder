using Filmder.DTOs;
using Filmder.Models;
using Filmder.Repositories;

namespace Filmder.Services;

public class PersonalizedPlaylistService : IPersonalizedPlaylistService
{
    private readonly IPersonalizedPlaylistRepository _repository;
    private readonly IAIService _aiService;

    public PersonalizedPlaylistService(IPersonalizedPlaylistRepository repository, IAIService aiService)
    {
        _repository = repository;
        _aiService = aiService;
    }

    public async Task<PersonalizedPlaylistResultDto> GeneratePlaylistAsync(string userId, int count)
    {
        try
        {
            var userRatings = await _repository.GetUserRatingsAsync(userId);

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var recentUserMovies = await _repository.GetRecentUserMoviesAsync(userId, thirtyDaysAgo, 20);

            var recentActivity = recentUserMovies.Select(um => new UserMovieTasteDto
            {
                MovieName = um.Movie.Name,
                Genre = um.Movie.Genre.ToString(),
                ReleaseYear = um.Movie.ReleaseYear,
                Director = um.Movie.Director,
                UserRating = userRatings.TryGetValue(um.MovieId, out var rating) ? rating.Score : null,
                UserComment = userRatings.GetValueOrDefault(um.MovieId)?.Comment,
                WatchedAt = um.WatchedAt
            }).ToList();

            if (recentActivity.Count < 5)
            {
                var userMoviesWithRatings = await _repository.GetAllUserMoviesAsync(userId, 20);

                recentActivity = userMoviesWithRatings
                    .Select(um => new UserMovieTasteDto
                    {
                        MovieName = um.Movie.Name,
                        Genre = um.Movie.Genre.ToString(),
                        ReleaseYear = um.Movie.ReleaseYear,
                        Director = um.Movie.Director,
                        UserRating = userRatings.TryGetValue(um.MovieId, out var rating) ? rating.Score : null,
                        UserComment = userRatings.GetValueOrDefault(um.MovieId)?.Comment,
                        WatchedAt = um.WatchedAt
                    })
                    .OrderByDescending(m => m.UserRating ?? 0)
                    .ThenByDescending(m => m.WatchedAt)
                    .ToList();
            }

            if (!recentActivity.Any() && userRatings.Any())
            {
                var ratedMovieIds = userRatings.Keys.ToList();
                var ratedMovies = await _repository.GetMoviesByIdsAsync(ratedMovieIds);

                recentActivity = ratedMovies
                    .Select(m => new UserMovieTasteDto
                    {
                        MovieName = m.Name,
                        Genre = m.Genre.ToString(),
                        ReleaseYear = m.ReleaseYear,
                        Director = m.Director,
                        UserRating = userRatings[m.Id].Score,
                        UserComment = userRatings[m.Id].Comment,
                        WatchedAt = userRatings[m.Id].CreatedAt
                    })
                    .OrderByDescending(m => m.UserRating)
                    .Take(20)
                    .ToList();
            }

            if (!recentActivity.Any())
            {
                return new PersonalizedPlaylistResultDto
                {
                    Success = false,
                    StatusCode = 404,
                    ErrorMessage = "Not enough viewing history. Watch and rate some movies to get personalized recommendations!"
                };
            }

            var playlist = await _aiService.GeneratePersonalizedPlaylist(recentActivity, count);

            var watchedMovieIds = await _repository.GetWatchedMovieIdsAsync(userId);

            foreach (var playlistMovie in playlist.Movies)
            {
                var dbMovie = await _repository.FindMatchingMovieAsync(
                    playlistMovie.MovieName,
                    playlistMovie.ReleaseYear,
                    watchedMovieIds);

                if (dbMovie != null)
                {
                    playlistMovie.MovieId = dbMovie.Id;
                    playlistMovie.MovieName = dbMovie.Name;
                    playlistMovie.Genre = dbMovie.Genre.ToString();
                    playlistMovie.ReleaseYear = dbMovie.ReleaseYear;
                    playlistMovie.Rating = dbMovie.Rating;
                    playlistMovie.PosterUrl = dbMovie.PosterUrl ?? "";
                }
            }

            return new PersonalizedPlaylistResultDto
            {
                Success = true,
                Playlist = playlist
            };
        }
        catch (Exception ex)
        {
            return new PersonalizedPlaylistResultDto
            {
                Success = false,
                StatusCode = 500,
                ErrorMessage = $"Failed to generate playlist: {ex.Message}"
            };
        }
    }

    public async Task<QuickPicksResultDto> GetQuickPicksAsync(string userId)
    {
        try
        {
            var highRatedMovieIds = await _repository.GetHighRatedMovieIdsAsync(userId, 7);

            var favoriteGenres = await _repository.GetFavoriteGenresAsync(highRatedMovieIds, 3);

            if (!favoriteGenres.Any())
            {
                return new QuickPicksResultDto
                {
                    Success = false,
                    StatusCode = 404,
                    ErrorMessage = "Not enough data for quick picks. Rate some movies first!"
                };
            }

            var watchedIds = await _repository.GetWatchedMovieIdsAsync(userId);

            var quickPicks = await _repository.GetUnwatchedMoviesByGenresAsync(watchedIds, favoriteGenres, 7.5);

            var randomPicks = quickPicks
                .OrderBy(m => Guid.NewGuid())
                .Take(5)
                .Select(m => new QuickPickMovieDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Genre = m.Genre.ToString(),
                    ReleaseYear = m.ReleaseYear,
                    Rating = m.Rating,
                    PosterUrl = m.PosterUrl,
                    Duration = m.Duration,
                    Director = m.Director
                })
                .ToList();

            return new QuickPicksResultDto
            {
                Success = true,
                Title = "Quick Picks For You",
                Description = $"Based on your love for {string.Join(", ", favoriteGenres.Select(g => g.ToString()))} movies",
                Movies = randomPicks
            };
        }
        catch (Exception ex)
        {
            return new QuickPicksResultDto
            {
                Success = false,
                StatusCode = 500,
                ErrorMessage = $"Failed to get quick picks: {ex.Message}"
            };
        }
    }
}