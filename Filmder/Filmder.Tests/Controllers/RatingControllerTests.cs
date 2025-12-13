using System.Security.Claims;
using Filmder.Controllers;
using Filmder.DTOs;
using Filmder.Models;
using Filmder.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Filmder.Tests.Controllers;

public class RatingControllerTests
{
    [Fact]
    public async Task RateMovie_NewRating_CreatesRatingSuccessfully()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        context.Users.Add(new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" });
        await context.SaveChangesAsync();

        var controller = new RatingController(context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        var dto = new RateMovieDto
        {
            MovieId = 1,
            Score = 9,
            Comment = "Amazing movie!"
        };

        // Act
        var result = await controller.RateMovie(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var rating = context.Ratings.First(r => r.UserId == "user1" && r.MovieId == 1);
        rating.Score.Should().Be(9);
        rating.Comment.Should().Be("Amazing movie!");
    }

    [Fact]
    public async Task RateMovie_UpdateExistingRating_UpdatesSuccessfully()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);
        
        var existingRating = new Rating
        {
            UserId = "user1",
            MovieId = 1,
            Score = 7,
            Comment = "Good"
        };
        context.Ratings.Add(existingRating);
        await context.SaveChangesAsync();

        var controller = new RatingController(context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        var dto = new RateMovieDto
        {
            MovieId = 1,
            Score = 10,
            Comment = "Masterpiece!"
        };

        // Act
        var result = await controller.RateMovie(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var updatedRating = context.Ratings.First(r => r.UserId == "user1" && r.MovieId == 1);
        updatedRating.Score.Should().Be(10);
        updatedRating.Comment.Should().Be("Masterpiece!");
    }

    [Fact]
    public async Task RateMovie_NonExistentMovie_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        context.Users.Add(new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" });
        await context.SaveChangesAsync();

        var controller = new RatingController(context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        var dto = new RateMovieDto
        {
            MovieId = 999,
            Score = 8
        };

        // Act
        var result = await controller.RateMovie(dto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRatings_ForMovie_ReturnsAllRatings()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user1 = new AppUser { Id = "user1", Email = "test1@example.com", UserName = "user1" };
        var user2 = new AppUser { Id = "user2", Email = "test2@example.com", UserName = "user2" };
        context.Users.AddRange(user1, user2);

        context.Ratings.AddRange(
            new Rating { UserId = "user1", MovieId = 1, Score = 9, Comment = "Great!" },
            new Rating { UserId = "user2", MovieId = 1, Score = 7, Comment = "Good" }
        );
        await context.SaveChangesAsync();

        var controller = new RatingController(context);

        // Act
        var result = await controller.GetRatings(1);

        // Assert
        var actionResult = result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var ratings = actionResult.Value as IEnumerable<object>;
        ratings.Should().NotBeNull();
        ratings.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRatings_NoRatings_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        var controller = new RatingController(context);

        // Act
        var result = await controller.GetRatings(1);

        // Assert
        var actionResult = result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var ratings = actionResult.Value as IEnumerable<object>;
        ratings.Should().NotBeNull();
        ratings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAverageRating_WithRatings_ReturnsCorrectAverage()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user1 = new AppUser { Id = "user1", Email = "test1@example.com", UserName = "user1" };
        var user2 = new AppUser { Id = "user2", Email = "test2@example.com", UserName = "user2" };
        var user3 = new AppUser { Id = "user3", Email = "test3@example.com", UserName = "user3" };
        context.Users.AddRange(user1, user2, user3);

        context.Ratings.AddRange(
            new Rating { UserId = "user1", MovieId = 1, Score = 8 },
            new Rating { UserId = "user2", MovieId = 1, Score = 10 },
            new Rating { UserId = "user3", MovieId = 1, Score = 6 }
        );
        await context.SaveChangesAsync();

        var controller = new RatingController(context);

        // Act
        var result = await controller.GetAverageRating(1);

        // Assert
        var actionResult = result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var value = actionResult.Value!;
        var averageScore = value.GetType().GetProperty("averageScore")?.GetValue(value);
        var totalRatings = value.GetType().GetProperty("totalRatings")?.GetValue(value);
        
        averageScore.Should().Be(8.0);
        totalRatings.Should().Be(3);
    }

    [Fact]
    public async Task GetAverageRating_NoRatings_ReturnsZero()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        var controller = new RatingController(context);

        // Act
        var result = await controller.GetAverageRating(1);

        // Assert
        var actionResult = result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var value = actionResult.Value!;
        var averageScore = value.GetType().GetProperty("averageScore")?.GetValue(value);
        var totalRatings = value.GetType().GetProperty("totalRatings")?.GetValue(value);
        
        averageScore.Should().Be(0.0);
        totalRatings.Should().Be(0);
    }

    [Fact]
    public async Task RateMovie_WithoutComment_CreatesRatingWithoutComment()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        context.Users.Add(new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" });
        await context.SaveChangesAsync();

        var controller = new RatingController(context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        var dto = new RateMovieDto
        {
            MovieId = 1,
            Score = 8
        };

        // Act
        var result = await controller.RateMovie(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var rating = context.Ratings.First(r => r.UserId == "user1" && r.MovieId == 1);
        rating.Score.Should().Be(8);
        rating.Comment.Should().BeNull();
    }

    [Fact]
    public async Task RateMovie_ScoreAtBoundary_AcceptsValidScores()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        context.Users.Add(new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" });
        await context.SaveChangesAsync();

        var controller = new RatingController(context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        // Test minimum score
        var dto1 = new RateMovieDto { MovieId = 1, Score = 1 };
        var result1 = await controller.RateMovie(dto1);
        result1.Should().BeOfType<OkObjectResult>();

        // Test maximum score
        var dto2 = new RateMovieDto { MovieId = 2, Score = 10 };
        var result2 = await controller.RateMovie(dto2);
        result2.Should().BeOfType<OkObjectResult>();

        // Assert
        var rating1 = context.Ratings.First(r => r.MovieId == 1);
        var rating2 = context.Ratings.First(r => r.MovieId == 2);
        
        rating1.Score.Should().Be(1);
        rating2.Score.Should().Be(10);
    }

    [Fact]
    public async Task GetRatings_OrderedByCreatedDate_ReturnsNewestFirst()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "user1" };
        context.Users.Add(user);

        var oldRating = new Rating 
        { 
            UserId = "user1", 
            MovieId = 1, 
            Score = 7,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        
        var newRating = new Rating 
        { 
            UserId = "user1", 
            MovieId = 1, 
            Score = 9,
            CreatedAt = DateTime.UtcNow
        };

        context.Ratings.AddRange(oldRating, newRating);
        await context.SaveChangesAsync();

        var controller = new RatingController(context);

        // Act
        var result = await controller.GetRatings(1);

        // Assert
        var actionResult = result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var ratings = actionResult.Value as IEnumerable<object>;
        ratings.Should().HaveCount(2);
    }
}