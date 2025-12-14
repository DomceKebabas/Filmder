using Filmder.DTOs;
using Filmder.Models;
using Filmder.Repositories;

namespace Filmder.Services;

public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepository;
    private readonly IMovieRepository _movieRepository;

    public RatingService(IRatingRepository ratingRepository, IMovieRepository movieRepository)
    {
        _ratingRepository = ratingRepository;
        _movieRepository = movieRepository;
    }

    public async Task<(bool Success, string Message)> RateMovieAsync(string userId, RateMovieDto dto)
    {
        var movie = await _movieRepository.GetByIdAsync(dto.MovieId);
        if (movie == null)
        {
            return (false, "Movie not found");
        }

        var existingRating = await _ratingRepository.GetByUserAndMovieAsync(userId, dto.MovieId);

        if (existingRating != null)
        {
            existingRating.Score = dto.Score;
            existingRating.Comment = dto.Comment;
            existingRating.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            var rating = new Rating
            {
                UserId = userId,
                MovieId = dto.MovieId,
                Score = dto.Score,
                Comment = dto.Comment
            };
            await _ratingRepository.AddAsync(rating);
        }

        await _ratingRepository.SaveChangesAsync();

        return (true, "Rating saved successfully");
    }

    public async Task<List<RatingResponseDto>> GetRatingsByMovieAsync(int movieId)
    {
        var ratings = await _ratingRepository.GetByMovieIdWithUserAsync(movieId);

        return ratings.Select(r => new RatingResponseDto
        {
            Id = r.Id,
            Score = r.Score,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
            UserEmail = r.User.Email
        }).ToList();
    }

    public async Task<AverageRatingDto> GetAverageRatingAsync(int movieId)
    {
        var ratings = await _ratingRepository.GetByMovieIdAsync(movieId);

        if (!ratings.Any())
        {
            return new AverageRatingDto { AverageScore = 0, TotalRatings = 0 };
        }

        var averageScore = ratings.Average(r => r.Score);
        var totalRatings = ratings.Count;

        return new AverageRatingDto
        {
            AverageScore = Math.Round(averageScore, 1),
            TotalRatings = totalRatings
        };
    }
}