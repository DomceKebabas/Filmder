using Filmder.DTOs;
using Filmder.Models;
using Filmder.Repositories;

namespace Filmder.Services;

public class SwipeService : ISwipeService
{
    private readonly ISwipeRepository _swipeRepository;
    private readonly IMovieRepository _movieRepository;

    public SwipeService(ISwipeRepository swipeRepository, IMovieRepository movieRepository)
    {
        _swipeRepository = swipeRepository;
        _movieRepository = movieRepository;
    }

    public async Task<(Movie? movie, string? errorMessage)> GetRandomMovieAsync(string userId, string? genre, int? minYear, int? maxDuration)
    {
        var swipedMovieIds = await _swipeRepository.GetSwipedMovieIdsAsync(userId);
        
        var movie = await _movieRepository.GetRandomMovieAsync(swipedMovieIds, genre, minYear, maxDuration);
        
        if (movie == null)
        {
            return (null, "No more movies to swipe. Try adjusting filters or you've seen them all!");
        }

        return (movie, null);
    }

    public async Task<(bool success, string message)> RecordSwipeAsync(string userId, SwipeDto swipeDto)
    {
        var movieExists = await _movieRepository.ExistsAsync(swipeDto.MovieId);
        if (!movieExists)
        {
            return (false, "Movie not found");
        }

        var existingSwipe = await _swipeRepository.GetSwipeAsync(userId, swipeDto.MovieId);
        if (existingSwipe != null)
        {
            return (false, "You've already swiped on this movie");
        }

        var swipeHistory = new SwipeHistory
        {
            UserId = userId,
            MovieId = swipeDto.MovieId,
            IsLike = swipeDto.IsLike,
            SwipedAt = DateTime.UtcNow
        };

        await _swipeRepository.AddSwipeAsync(swipeHistory);

        return (true, swipeDto.IsLike ? "Movie liked!" : "Movie passed");
    }

    public async Task<List<SwipeHistoryDto>> GetSwipeHistoryAsync(string userId, bool? onlyLikes, int page, int pageSize)
    {
        var history = await _swipeRepository.GetSwipeHistoryAsync(userId, onlyLikes, page, pageSize);

        return history.Select(sh => new SwipeHistoryDto
        {
            Id = sh.Id,
            MovieId = sh.MovieId,
            MovieName = sh.Movie.Name,
            PosterUrl = sh.Movie.PosterUrl ?? "",
            IsLike = sh.IsLike,
            SwipedAt = sh.SwipedAt
        }).ToList();
    }

    public async Task<List<Movie>> GetLikedMoviesAsync(string userId, int page, int pageSize)
    {
        return await _swipeRepository.GetLikedMoviesAsync(userId, page, pageSize);
    }

    public async Task<(bool success, string message)> DeleteSwipeAsync(string userId, int swipeId)
    {
        var swipe = await _swipeRepository.GetSwipeByIdAsync(swipeId, userId);

        if (swipe == null)
        {
            return (false, "Swipe not found");
        }

        await _swipeRepository.DeleteSwipeAsync(swipe);

        return (true, "Swipe removed");
    }

    public async Task<SwipeStatsDto> GetSwipeStatsAsync(string userId)
    {
        var totalSwipes = await _swipeRepository.GetTotalSwipesCountAsync(userId);
        var totalLikes = await _swipeRepository.GetTotalLikesCountAsync(userId);
        var totalDislikes = totalSwipes - totalLikes;
        var favoriteGenre = await _swipeRepository.GetFavoriteGenreAsync(userId);

        return new SwipeStatsDto
        {
            TotalSwipes = totalSwipes,
            TotalLikes = totalLikes,
            TotalDislikes = totalDislikes,
            LikePercentage = totalSwipes > 0 ? Math.Round((double)totalLikes / totalSwipes * 100, 1) : 0,
            FavoriteGenre = favoriteGenre ?? "None yet"
        };
    }
}