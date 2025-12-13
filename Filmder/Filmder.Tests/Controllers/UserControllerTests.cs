using System.Security.Claims;
using Filmder.Controllers;
using Filmder.DTOs;
using Filmder.Models;
using Filmder.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Filmder.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<UserManager<AppUser>> _mockUserManager;

    public UserControllerTests()
    {
        _mockUserManager = MockHelpers.GetMockUserManager();
    }

    [Fact]
    public async Task ReturnLoggedInUser_ValidUser_ReturnsUserProfile()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        var user = new AppUser
        {
            Id = "user1",
            Email = "test@example.com",
            UserName = "testuser",
            ProfilePictureUrl = "https://example.com/pic.jpg"
        };

        _mockUserManager.Setup(x => x.FindByIdAsync("user1"))
            .ReturnsAsync(user);

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "testuser");

        // Act
        var result = await controller.ReturnLoggedInUser();

        // Assert
        var actionResult = result.Value;
        actionResult.Should().NotBeNull();
        actionResult.Id.Should().Be("user1");
        actionResult.Email.Should().Be("test@example.com");
        actionResult.Username.Should().Be("testuser");
        actionResult.ProfilePictureUrl.Should().Be("https://example.com/pic.jpg");
    }

    [Fact]
    public async Task ReturnLoggedInUser_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();

        _mockUserManager.Setup(x => x.FindByIdAsync("user1"))
            .ReturnsAsync((AppUser?)null);

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "testuser");

        // Act
        var result = await controller.ReturnLoggedInUser();

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetLoggedInUserStatsAsync_WithRatingsAndMovies_ReturnsCompleteStats()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);

        context.Ratings.AddRange(
            new Rating { UserId = "user1", MovieId = 1, Score = 9 },
            new Rating { UserId = "user1", MovieId = 2, Score = 8 },
            new Rating { UserId = "user1", MovieId = 3, Score = 10 }
        );

        context.SwipeHistories.AddRange(
            new SwipeHistory { UserId = "user1", MovieId = 1, IsLike = true },
            new SwipeHistory { UserId = "user1", MovieId = 4, IsLike = true }
        );

        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        // Act
        var result = await controller.GetLoggedInUserStatsAsync();

        // Assert
        var actionResult = result.Value;
        actionResult.Should().NotBeNull();
        actionResult.TotalRatings.Should().Be(3);
        actionResult.AverageRating.Should().BeApproximately(9.0, 0.1);
        actionResult.TotalMoviesWatched.Should().BeGreaterThanOrEqualTo(3);
        actionResult.FavoriteMovies.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetLoggedInUserStatsAsync_NoActivity_ReturnsZeroStats()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        // Act
        var result = await controller.GetLoggedInUserStatsAsync();

        // Assert
        var actionResult = result.Value;
        actionResult.Should().NotBeNull();
        actionResult.TotalMoviesWatched.Should().Be(0);
        actionResult.TotalRatings.Should().Be(0);
        actionResult.AverageRating.Should().BeNull();
        actionResult.TopGenres.Should().BeEmpty();
        actionResult.FavoriteMovies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLoggedInUserStatsAsync_CalculatesTopGenres_ReturnsCorrectGenres()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);

        // User rates SciFi movies highly
        context.Ratings.AddRange(
            new Rating { UserId = "user1", MovieId = 1, Score = 9 }, // The Matrix - SciFi
            new Rating { UserId = "user1", MovieId = 2, Score = 10 }, // Inception - SciFi
            new Rating { UserId = "user1", MovieId = 3, Score = 7 }  // The Godfather - Crime
        );

        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        // Act
        var result = await controller.GetLoggedInUserStatsAsync();

        // Assert
        var actionResult = result.Value;
        actionResult.Should().NotBeNull();
        actionResult.TopGenres.Should().NotBeEmpty();
        actionResult.TopGenres[0].Should().Be("SciFi");
    }

    [Fact]
    public async Task AddMovieToUser_NewMovie_AddsSuccessfully()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        var request = new AddMovieRequest
        {
            MovieId = 1,
            RatingScore = 9,
            Comment = "Great movie!"
        };

        // Act
        var result = await controller.AddMovieToUser(request);

        // Assert
        result.Should().BeOfType<OkResult>();
        
        var userMovie = context.UserMovies.FirstOrDefault(um => um.UserId == "user1" && um.MovieId == 1);
        userMovie.Should().NotBeNull();
        
        var rating = context.Ratings.FirstOrDefault(r => r.UserId == "user1" && r.MovieId == 1);
        rating.Should().NotBeNull();
        rating!.Score.Should().Be(9);
        rating.Comment.Should().Be("Great movie!");
    }

    [Fact]
    public async Task AddMovieToUser_WithoutRating_AddsMovieOnly()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        var request = new AddMovieRequest
        {
            MovieId = 1
        };

        // Act
        var result = await controller.AddMovieToUser(request);

        // Assert
        result.Should().BeOfType<OkResult>();
        
        var userMovie = context.UserMovies.FirstOrDefault(um => um.UserId == "user1" && um.MovieId == 1);
        userMovie.Should().NotBeNull();
        
        var rating = context.Ratings.FirstOrDefault(r => r.UserId == "user1" && r.MovieId == 1);
        rating.Should().BeNull();
    }

    [Fact]
    public async Task AddMovieToUser_AlreadyExists_ReturnsNoContent()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);
        
        context.UserMovies.Add(new UserMovie
        {
            UserId = "user1",
            MovieId = 1,
            WatchedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        var request = new AddMovieRequest
        {
            MovieId = 1,
            RatingScore = 8
        };

        // Act
        var result = await controller.AddMovieToUser(request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddMovieToUser_NonExistentMovie_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryContext();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        var request = new AddMovieRequest
        {
            MovieId = 999,
            RatingScore = 8
        };

        // Act
        var result = await controller.AddMovieToUser(request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetLoggedInUserStatsAsync_FavoriteMovies_OrderedByScore()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);

        context.Ratings.AddRange(
            new Rating { UserId = "user1", MovieId = 1, Score = 7 },
            new Rating { UserId = "user1", MovieId = 2, Score = 10 },
            new Rating { UserId = "user1", MovieId = 3, Score = 9 },
            new Rating { UserId = "user1", MovieId = 4, Score = 8 }
        );

        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        // Act
        var result = await controller.GetLoggedInUserStatsAsync();

        // Assert
        var actionResult = result.Value;
        actionResult.Should().NotBeNull();
        actionResult.FavoriteMovies.Should().HaveCount(4);
        
        // Should be ordered by score descending
        actionResult.FavoriteMovies[0].Score.Should().Be(10);
        actionResult.FavoriteMovies[1].Score.Should().Be(9);
        actionResult.FavoriteMovies[2].Score.Should().Be(8);
        actionResult.FavoriteMovies[3].Score.Should().Be(7);
    }

    [Fact]
    public async Task GetLoggedInUserStatsAsync_LimitsFavoriteMovies_ReturnsMaxFive()
    {
        // Arrange
        var context = TestDbContextFactory.CreateContextWithMovies();
        
        var user = new AppUser { Id = "user1", Email = "test@example.com", UserName = "test" };
        context.Users.Add(user);

        // Add more than 5 ratings
        for (int i = 1; i <= 4; i++)
        {
            context.Ratings.Add(new Rating { UserId = "user1", MovieId = i, Score = 10 - i });
        }

        await context.SaveChangesAsync();

        var controller = new UserController(_mockUserManager.Object, context);
        MockHelpers.SetupControllerContext(controller, "user1", "test@example.com", "test");

        // Act
        var result = await controller.GetLoggedInUserStatsAsync();

        // Assert
        var actionResult = result.Value;
        actionResult.Should().NotBeNull();
        actionResult.FavoriteMovies.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task ReturnLoggedInUser_NoUserId_ReturnsBadRequest()
    {
        var context = TestDbContextFactory.CreateContextWithMovies();
        var mockUserManager = MockHelpers.GetMockUserManager();
        var controller = new UserController(mockUserManager.Object, context);
        
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, "test@example.com")
                }, "TestAuth"))
            }
        };

        var result = await controller.ReturnLoggedInUser();

        result.Result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task GetLoggedInUserStatsAsync_NoUserId_ReturnsBadRequest()
    {
        var context = TestDbContextFactory.CreateContextWithMovies();
        var mockUserManager = MockHelpers.GetMockUserManager();
        var controller = new UserController(mockUserManager.Object, context);
        
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, "test@example.com")
                }, "TestAuth"))
            }
        };

        var result = await controller.GetLoggedInUserStatsAsync();

        result.Result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task AddMovieToUser_NoUserId_ReturnsBadRequest()
    {
        var context = TestDbContextFactory.CreateContextWithMovies();
        var mockUserManager = MockHelpers.GetMockUserManager();
        var controller = new UserController(mockUserManager.Object, context);
        
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, "test@example.com")
                }, "TestAuth"))
            }
        };

        var request = new AddMovieRequest { MovieId = 1 };

        var result = await controller.AddMovieToUser(request);

        result.Should().BeOfType<BadRequestResult>();
    }
}