using System.Net;
using System.Text.Json;
using Filmder.DTOs;
using Filmder.Models;
using Filmder.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace Filmder.Tests.Services;

public class GeminiAiServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly GeminiAiService _service;

    public GeminiAiServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns("test-api-key-12345");

        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        
        _service = new GeminiAiService(_httpClient, _mockConfig.Object);
    }

    [Fact]
    public void Constructor_WithValidApiKey_CreatesInstance()
    {
        // Arrange & Act
        var service = new GeminiAiService(_httpClient, _mockConfig.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithMissingApiKey_ThrowsException()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns((string?)null);

        // Act & Assert
        Assert.Throws<Exception>(() => new GeminiAiService(_httpClient, mockConfig.Object));
    }

    [Fact]
    public async Task GenerateText_WithValidResponse_ReturnsCleanedText()
    {
        // Arrange
        var expectedText = "This is a test response";
        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[] { new { text = $"```json\n{expectedText}\n```" } }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.GenerateText("test prompt");

        // Assert
        result.Should().Be(expectedText);
    }

    [Fact]
    public async Task GenerateText_WithError_ReturnsErrorMessage()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.BadRequest, new { error = "Bad request" });

        // Act
        var result = await _service.GenerateText("test prompt");

        // Assert
        result.Should().Contain("Error BadRequest");
    }

    [Fact]
    public async Task EmojiSequence_WithEasyDifficulty_ReturnsValidJson()
    {
        // Arrange
        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""movie"": ""The Lion King (1994)"",
                                    ""emoji"": ""🦁👑🌅🎭"",
                                    ""options"": [""The Lion King"", ""Aladdin"", ""Beauty and the Beast"", ""Frozen""]
                                }"
                            }
                        }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.EmojiSequence(Difficulty.Easy);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Lion King");
    }

    [Fact]
    public async Task MatchPersonalityToCharacters_WithValidSubmission_ReturnsMatches()
    {
        // Arrange
        var submission = new PersonalityQuizSubmissionDto
        {
            Answers = new List<PersonalityAnswerDto>
            {
                new PersonalityAnswerDto { QuestionId = 1, Answer = "Adventure" },
                new PersonalityAnswerDto { QuestionId = 2, Answer = "Strategic thinking" }
            }
        };

        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""matches"": [
                                        {
                                            ""characterName"": ""Indiana Jones"",
                                            ""movieOrSeries"": ""Raiders of the Lost Ark (1981)"",
                                            ""matchPercentage"": 85,
                                            ""explanation"": ""You love adventure"",
                                            ""imageUrl"": ""https://example.com/image.jpg""
                                        }
                                    ],
                                    ""personalityProfile"": ""Adventurous spirit""
                                }"
                            }
                        }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.MatchPersonalityToCharacters(submission);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().NotBeEmpty();
        result.Matches[0].CharacterName.Should().Be("Indiana Jones");
    }

    [Fact]
    public async Task MatchPersonalityToCharacters_WithInvalidJson_ReturnsEmptyResult()
    {
        // Arrange
        var submission = new PersonalityQuizSubmissionDto
        {
            Answers = new List<PersonalityAnswerDto>
            {
                new PersonalityAnswerDto { QuestionId = 1, Answer = "Test" }
            }
        };

        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[] { new { text = "invalid json {" } }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.MatchPersonalityToCharacters(submission);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().BeEmpty();
        result.PersonalityProfile.Should().Contain("Unable to analyze");
    }

    [Fact]
    public async Task ExplainUserTaste_WithWatchedMovies_ReturnsExplanation()
    {
        // Arrange
        var watchedMovies = new List<UserMovieTasteDto>
        {
            new UserMovieTasteDto
            {
                MovieName = "Inception",
                Genre = "Sci-Fi",
                ReleaseYear = 2010,
                Director = "Christopher Nolan",
                UserRating = 9,
                WatchedAt = DateTime.UtcNow
            }
        };

        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""overallTasteProfile"": ""Loves complex narratives"",
                                    ""insights"": [
                                        {
                                            ""category"": ""Genre Preferences"",
                                            ""explanation"": ""Prefers Sci-Fi"",
                                            ""exampleMovies"": [""Inception""]
                                        }
                                    ],
                                    ""favoriteThemes"": [""Mind-bending""],
                                    ""preferredDirectors"": [""Christopher Nolan""],
                                    ""watchingPersonality"": ""The Intellectual""
                                }"
                            }
                        }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.ExplainUserTaste(watchedMovies);

        // Assert
        result.Should().NotBeNull();
        result.OverallTasteProfile.Should().Contain("complex narratives");
        result.WatchingPersonality.Should().Be("The Intellectual");
    }

    [Fact]
    public async Task ExplainUserTaste_WithEmptyList_ReturnsDefaultResponse()
    {
        // Arrange
        var emptyList = new List<UserMovieTasteDto>();

        // Act
        var result = await _service.ExplainUserTaste(emptyList);

        // Assert
        result.Should().NotBeNull();
        result.OverallTasteProfile.Should().Contain("Not enough data");
        result.WatchingPersonality.Should().Contain("Explorer");
    }

    [Fact]
    public async Task GeneratePersonalizedPlaylist_WithRecentActivity_ReturnsPlaylist()
    {
        // Arrange
        var recentActivity = new List<UserMovieTasteDto>
        {
            new UserMovieTasteDto
            {
                MovieName = "The Matrix",
                Genre = "Sci-Fi",
                ReleaseYear = 1999,
                Director = "Wachowskis",
                UserRating = 9,
                WatchedAt = DateTime.UtcNow
            }
        };

        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""playlistName"": ""Sci-Fi Classics"",
                                    ""description"": ""Based on your love for The Matrix"",
                                    ""reasoning"": ""Curated for sci-fi fans"",
                                    ""movies"": [
                                        {
                                            ""movieId"": 0,
                                            ""movieName"": ""Blade Runner"",
                                            ""genre"": ""Sci-Fi"",
                                            ""releaseYear"": 1982,
                                            ""rating"": 8.1,
                                            ""posterUrl"": ""https://example.com/poster.jpg"",
                                            ""whyRecommended"": ""Similar dystopian themes"",
                                            ""recommendationScore"": 88
                                        }
                                    ]
                                }"
                            }
                        }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.GeneratePersonalizedPlaylist(recentActivity, 1);

        // Assert
        result.Should().NotBeNull();
        result.PlaylistName.Should().Be("Sci-Fi Classics");
        result.Movies.Should().NotBeEmpty();
        result.Movies[0].MovieName.Should().Be("Blade Runner");
    }

    [Fact]
    public async Task GenerateMovieTrivia_WithValidMovie_ReturnsTriviaQuestions()
    {
        // Arrange
        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"{
                                    ""movieId"": 0,
                                    ""movieName"": ""Inception"",
                                    ""questions"": [
                                        {
                                            ""question"": ""Who directed Inception?"",
                                            ""options"": [""Christopher Nolan"", ""Steven Spielberg"", ""James Cameron"", ""Ridley Scott""],
                                            ""correctAnswerIndex"": 0
                                        }
                                    ]
                                }"
                            }
                        }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.GenerateMovieTrivia(
            "Inception", 2010, "Sci-Fi", "Christopher Nolan", "Dream heist movie", 1);

        // Assert
        result.Should().NotBeNull();
        result.MovieName.Should().Be("Inception");
        result.Questions.Should().NotBeEmpty();
        result.Questions[0].Question.Should().Contain("directed");
    }

    [Fact]
    public async Task GenerateMovieTrivia_WithInvalidResponse_ReturnsEmptyQuestions()
    {
        // Arrange
        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[] { new { text = "invalid" } }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.GenerateMovieTrivia(
            "Test Movie", 2020, "Action", "Director", "Description", 5);

        // Assert
        result.Should().NotBeNull();
        result.Questions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Medium)]
    [InlineData(Difficulty.Hard)]
    public async Task EmojiSequence_WithDifferentDifficulties_ReturnsResponse(Difficulty difficulty)
    {
        // Arrange
        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = @"{""movie"": ""Test (2020)"", ""emoji"": ""🎬🎭"", ""options"": [""A"", ""B"", ""C"", ""D""]}" }
                        }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _service.EmojiSequence(difficulty);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object responseContent)
    {
        var json = JsonSerializer.Serialize(responseContent);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);
    }
}