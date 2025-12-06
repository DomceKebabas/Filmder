using System.Text.Json;
using Filmder.DTOs;
using Filmder.Models;

namespace Filmder.Services;

public class GeminiAiService : IAIService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GeminiAiService(IConfiguration config)
    {
        _http = new HttpClient();
        _apiKey = config["Gemini:ApiKey"] ?? throw new Exception("API Key is missing");
    }


    public async Task<string> GenerateText(string prompt)
    {
        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("X-goog-api-key", _apiKey);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(httpRequest);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return $"Error {response.StatusCode}: {json}";

        var result = JsonSerializer.Deserialize<GeminiResponse>(json);
        var raw = result?.candidates?[0]?.content?.parts?[0]?.text ?? "No response";

        return CleanAiResponse(raw);
    }


    private string CleanAiResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return text
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();
    }


    public Task<string> EmojiSequence(Difficulty difficulty)
    {
        var randomSeed = DateTime.UtcNow.Ticks % 10000;
        var random = new Random((int)randomSeed);

        var eraIndex = random.Next(0, 6);
        var genreIndex = random.Next(0, 8);

        var eras = new[] { "1990s", "2000s", "2010s", "2020s", "1980s", "2015-2023" };
        var genres = new[] { "Action", "Comedy", "Drama", "Thriller", "Sci-Fi", "Horror", "Romance", "Animation" };

        var targetEra = eras[eraIndex];
        var targetGenre = genres[genreIndex];

        var bannedMovies = difficulty switch
        {
            Difficulty.Easy => "Frozen, Toy Story, Finding Nemo, The Lion King",
            Difficulty.Medium => "Truman Show, The Social Network",
            Difficulty.Hard => "The Godfather",
            _ => ""
        };

        string prompt = $@"
        You are generating content for a movie-guessing game.

        RANDOMIZATION SEED: {randomSeed}
        TARGET ERA: {targetEra}
        TARGET GENRE: {targetGenre}

        THE GAME:
        1. Show the player an emoji sequence that represents a real movie.
        2. Give them four answer options (1 correct, 3 incorrect).

        YOUR TASK:
        - Pick a movie from the {targetEra} era in the {targetGenre} genre
        - The movie MUST match the requested difficulty level
        - Create an emoji sequence with at least 8 emojis that hints at the movie
        - Create 3 incorrect options that are plausible but NOT from the same franchise
        - Output ONLY valid JSON
        - NEVER repeat the same movies - use the randomization seed to ensure variety

        CRITICAL VARIETY RULES:
        - Based on seed {randomSeed}, you MUST choose different movies each time
        - If seed ends in 0-1: Pick movies about RELATIONSHIPS or EMOTIONS
        - If seed ends in 2-3: Pick movies about TECHNOLOGY or FUTURE
        - If seed ends in 4-5: Pick movies about CRIME or MYSTERY
        - If seed ends in 6-7: Pick movies about ADVENTURE or EXPLORATION
        - If seed ends in 8-9: Pick movies about COMEDY or ABSURDITY

        BANNED MOVIES (overused - DO NOT USE):
        {bannedMovies}

        DIFFICULTY DEFINITIONS (VERY IMPORTANT):
        - EASY: (EVERYONE SHOULD BE ABLE TO GUESS BECAUSE IT'S SO EASY)
          - Choose extremely popular, mainstream movies from {targetEra}
          - Examples: Pixar films, major Disney releases, huge blockbusters
          - Movies that broke box office records or won major Oscars
          - Films everyone has heard of even if they haven't seen them
          - Emoji should be obvious and easy to decode
          - Wrong options should be clearly different from the right movie

        - MEDIUM: (NOT EVERYONE SHOULD BE ABLE TO GUESS BUT IT'S MANAGABLE)
          - Choose well-known but not ultra-mainstream movies from {targetEra}
          - Movies with a dedicated fan base, Oscar nominees, cult classics
          - Films that were successful but not necessarily #1 box office
          - Emoji sequence should require some thinking but still solvable
          - Wrong options should be believable and from related genres

        - HARD: (ONLY REAL MOVIE FANS CAN GUESS RIGHT)
          - Choose critically acclaimed but less mainstream movies from {targetEra}
          - Indie darlings, international cinema, art house films
          - Movies film buffs love but casual viewers might not know
          - Emoji sequence should be clever and indirect
          - Wrong options must be very convincing alternatives (same era/tone)

        VARIETY EXAMPLES BY SEED:
        
        Seed ending in 0-1 (Relationships):
        - Easy: The Notebook (2004), Titanic (1997), La La Land (2016)
        - Medium: Eternal Sunshine of the Spotless Mind (2004), Her (2013), Marriage Story (2019)
        - Hard: Blue Valentine (2010), Carol (2015), Phantom Thread (2017)

        Seed ending in 2-3 (Technology):
        - Easy: The Social Network (2010), Ready Player One (2018), WALL-E (2008)
        - Medium: Ex Machina (2014), Minority Report (2002), Black Mirror: Bandersnatch (2018)
        - Hard: Primer (2004), Moon (2009), Upgrade (2018)

        Seed ending in 4-5 (Crime):
        - Easy: The Departed (2006), Catch Me If You Can (2002), Knives Out (2019)
        - Medium: Prisoners (2013), Zodiac (2007), Wind River (2017)
        - Hard: Memories of Murder (2003), The Secret in Their Eyes (2009), Burning (2018)

        Seed ending in 6-7 (Adventure):
        - Easy: Indiana Jones series, Jurassic Park (1993), Pirates of the Caribbean (2003)
        - Medium: Life of Pi (2012), The Revenant (2015), Mad Max: Fury Road (2015)
        - Hard: The Fall (2006), Samsara (2011), Embrace of the Serpent (2015)

        Seed ending in 8-9 (Comedy):
        - Easy: The Hangover (2009), Bridesmaids (2011), Superbad (2007)
        - Medium: In Bruges (2008), The Grand Budapest Hotel (2014), Hunt for the Wilderpeople (2016)
        - Hard: The Lobster (2015), Sorry to Bother You (2018), Submarine (2010)

        EMOJI CREATION RULES:
        - Use exactly 8-12 emojis
        - First 2-3 emojis: hint at the GENRE
        - Middle emojis: hint at KEY PLOT ELEMENTS or MEMORABLE SCENES
        - Last 2-3 emojis: hint at the ENDING or MAIN THEME
        - Make emojis SPECIFIC to the movie, not generic
        - For EASY: emojis should be literal and obvious
        - For MEDIUM: emojis should be metaphorical but clear
        - For HARD: emojis should be abstract and require deep knowledge

        WRONG OPTIONS RULES:
        - All 4 options must be from the SAME era (within 5 years)
        - All 4 options must be from SIMILAR genres
        - Wrong options should sound plausible based on the emojis
        - NEVER use movies from the same franchise
        - Wrong options should be REAL movies (verify they exist)

        JSON FORMAT (MANDATORY):
        {{
          ""movie"": ""Correct movie title (Year)"",
          ""emoji"": ""🎬🎭🎪🎨🎯🎲🎰🎮"",
          ""options"": [
            ""Correct movie title"",
            ""Wrong option 1 (different movie, same era/genre)"",
            ""Wrong option 2 (different movie, same era/genre)"",
            ""Wrong option 3 (different movie, same era/genre)""
          ]
        }}

        OUTPUT RULES (ABSOLUTELY REQUIRED):
        - CORRECT MOVIE TITLE SHOULD BE IN RANDOM SPOTS NOT ONLY IN THE FIRST ONE.
        - Output ONLY valid JSON
        - Do NOT wrap JSON in ``` or ```json
        - No markdown
        - No extra text before or after the JSON
        - No explanations
        - No comments
        - Movie title should include year in parentheses
        - Emoji string should be 8-12 emojis, no spaces between them

        EXAMPLES OF GOOD VARIETY:

        ❌ BAD (always Marvel):
        Game 1: Avengers
        Game 2: Iron Man
        Game 3: Spider-Man

        ✅ GOOD (variety):
        Game 1: Godfather (1998)
        Game 2: Parasite (2019)
        Game 3: Whiplash (2014)

        ❌ BAD (same wrong options):
        Options: [Inception, The Matrix, Interstellar, Tenet]

        ✅ GOOD (varied wrong options):
        Options: [Her, Lost in Translation, Amélie, Moonrise Kingdom]

        Now generate the puzzle with MAXIMUM VARIETY.
        Difficulty: {difficulty}
        Target Era: {targetEra}
        Target Genre: {targetGenre}
        Randomization Seed: {randomSeed}
        
        Remember: Use the seed and target era/genre to ensure you pick a DIFFERENT movie than last time!
        ";

        return GenerateText(prompt);
    }

    public async Task<PersonalityMatchResultDto> MatchPersonalityToCharacters(PersonalityQuizSubmissionDto submission)
    {
        var answersText = string.Join("\n", submission.Answers.Select((a, i) =>
            $"Question {a.QuestionId}: {a.Answer}"));

        var randomSeed = DateTime.UtcNow.Ticks % 1000000;
        var answerHash = string.Join("", submission.Answers.Select(a => a.Answer)).GetHashCode();
        var diversitySeed = (randomSeed + answerHash) % 100;

        string prompt = $@"
    You are a personality analyzer that matches users to movie/TV characters based on their quiz answers.

    USER'S QUIZ ANSWERS:
    {answersText}

    DIVERSITY SEED: {diversitySeed}
    
    CRITICAL INSTRUCTIONS FOR CHARACTER SELECTION:
    1. You MUST choose characters from DIFFERENT movies/shows - NO repeats from the same franchise
    2. You MUST vary character types - mix protagonists, antagonists, side characters, anti-heroes
    3. You MUST consider the DIVERSITY SEED ({diversitySeed}) to ensure different results each time
    4. PRIORITIZE lesser-known but meaningful character matches over obvious popular choices
    5. Focus on characters from movies/shows released in the last 30 years (1995-2025)
    6. Avoid using the same characters repeatedly - be creative and explore different films
    7. You MUST return EXACTLY 5 character matches

    IMPORTANT VARIETY RULES: (Focus, but if character matches from different category make sure to give it.)
    - If seed is 0-20: Focus on DRAMATIC/INTENSE characters from dramas, thrillers
    - If seed is 21-40: Focus on COMEDIC/LIGHTHEARTED characters from comedies, rom-coms  
    - If seed is 41-60: Focus on ACTION/ADVENTURE characters from action, sci-fi, fantasy
    - If seed is 61-80: Focus on COMPLEX/NUANCED characters from indie films, character studies
    - If seed is 81-100: Focus on ICONIC/LEGENDARY characters from classic and modern cinema

    BANNED CHARACTERS (overused - DO NOT USE):
    - Tony Stark / Iron Man
    - Elle Woods
    - Any character from Marvel Cinematic Universe (unless extremely fitting)
    - Characters from Game of Thrones (unless extremely fitting)

    YOUR TASK:
    1. Analyze the personality traits revealed by these answers
    2. Match the user to exactly 5 movie or TV series characters
    3. Ensure ALL 5 characters are from COMPLETELY DIFFERENT movies/shows/franchises
    4. Calculate a match percentage (60-99%) for each character - make them meaningful differences
    5. Provide a brief explanation for each match that references SPECIFIC quiz answers
    6. Find real image URLs for each character from TMDB or IMDb
    7. Create a short personality profile summary

    MATCHING CRITERIA:
    - Consider values, decision-making style, social preferences, conflict handling, life outlook
    - Choose characters that would resonate with THIS SPECIFIC user based on their answers
    - Match percentages should reflect genuine compatibility (don't just give 95%+ to everyone)
    - First match: 88-95% (best fit)
    - Second match: 82-88% (very strong fit)  
    - Third match: 75-82% (strong fit)
    - Fourth match: 68-75% (good fit with some differences)
    - Fifth match: 60-68% (decent fit but notable differences)

    IMAGE URL REQUIREMENTS:
    - Provide REAL working image URLs for each character
    - Use high-quality images from TMDB: https://image.tmdb.org/t/p/w500/[path]
    - Or from IMDb: https://m.media-amazon.com/images/M/[path]
    - If no real URL available, use: https://via.placeholder.com/300x450?text=[CharacterName]

    EXAMPLE OF GOOD DIVERSE MATCHES (5 characters from 5 different movies):
    ❌ BAD (all superheroes): Iron Man, Captain America, Thor, Black Widow, Hulk
    ✅ GOOD (varied): Amélie (Amélie), Mark Zuckerberg (The Social Network), Luna Lovegood (Harry Potter), Joel (Eternal Sunshine), Mia (La La Land)

    ❌ BAD (all from same franchise): Luke Skywalker, Han Solo, Princess Leia, Darth Vader, Yoda
    ✅ GOOD (different universes): Theodore (Her), Nina (Black Swan), Patrick Bateman (American Psycho), Juno (Juno), Llewyn Davis (Inside Llewyn Davis)

    JSON FORMAT (MANDATORY):
    {{
      ""matches"": [
        {{
          ""characterName"": ""Character Name"",
          ""movieOrSeries"": ""Movie/Series Title (Year)"",
          ""matchPercentage"": 91,
          ""explanation"": ""Based on your answer about [specific answer], you share this character's [specific trait]. Like when you said [quote from answer], this mirrors how [character] approaches [situation]."",
          ""imageUrl"": ""https://image.tmdb.org/t/p/w500/characterimage.jpg""
        }},
        {{
          ""characterName"": ""Different Character from DIFFERENT Movie"",
          ""movieOrSeries"": ""Different Movie Title (Year)"",
          ""matchPercentage"": 85,
          ""explanation"": ""Your perspective on [specific topic] aligns with this character's worldview. When you mentioned [specific answer], it reflects [character trait]."",
          ""imageUrl"": ""https://image.tmdb.org/t/p/w500/characterimage2.jpg""
        }},
        {{
          ""characterName"": ""Third Character from YET ANOTHER Movie"",
          ""movieOrSeries"": ""Third Different Movie Title (Year)"",
          ""matchPercentage"": 78,
          ""explanation"": ""Though different in some ways, your approach to [topic] resembles this character's journey in [movie]. Your answer about [specific] shows this connection."",
          ""imageUrl"": ""https://image.tmdb.org/t/p/w500/characterimage3.jpg""
        }},
        {{
          ""characterName"": ""Fourth Character from ANOTHER Different Movie"",
          ""movieOrSeries"": ""Fourth Different Movie Title (Year)"",
          ""matchPercentage"": 71,
          ""explanation"": ""Your answer about [specific topic] reveals a similarity with this character's [trait]. This connection becomes clear when [specific example]."",
          ""imageUrl"": ""https://image.tmdb.org/t/p/w500/characterimage4.jpg""
        }},
        {{
          ""characterName"": ""Fifth Character from YET ANOTHER Different Movie"",
          ""movieOrSeries"": ""Fifth Different Movie Title (Year)"",
          ""matchPercentage"": 64,
          ""explanation"": ""While not your strongest match, your perspective on [topic] shares elements with this character. Your answer about [specific] hints at this parallel."",
          ""imageUrl"": ""https://image.tmdb.org/t/p/w500/characterimage5.jpg""
        }}
      ],
      ""personalityProfile"": ""A concise 2-3 sentence summary that references SPECIFIC answers from the quiz and explains the overall personality pattern seen""
    }}

    OUTPUT RULES (ABSOLUTELY REQUIRED):
    - Output ONLY valid JSON
    - Do NOT wrap JSON in ``` or ```json
    - No markdown formatting
    - No extra text before or after the JSON
    - No explanations outside the JSON
    - No comments
    - Match percentages must be integers between 60-95 (be realistic, not everyone is 95%!)
    - Order matches from highest to lowest percentage
    - ImageUrl must be a valid URL or placeholder
    - ALL 5 characters MUST be from DIFFERENT movies/shows
    - Explanations MUST reference specific quiz answers
    - You MUST return exactly 5 matches, no more, no less

    Now analyze the personality and provide EXACTLY 5 DIVERSE, VARIED matches from 5 DIFFERENT movies/shows.
    Remember: VARIETY IS KEY - no repeating franchises or character types!
    Focus: BE AS CREATIVE AS YOU CAN.
    You must return 5 characters!
    ";

        string raw = await GenerateText(prompt);

        var settings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var result = JsonSerializer.Deserialize<PersonalityMatchResultDto>(raw, settings);
            return result ?? new PersonalityMatchResultDto();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to parse AI response: {ex.Message}");
            Console.WriteLine($"Raw response: {raw}");

            return new PersonalityMatchResultDto
            {
                PersonalityProfile = "Unable to analyze personality at this time. Please try again.",
                Matches = new List<CharacterMatchDto>()
            };
        }
    }

    private EmojiPuzzleDto? ParseMovieJson(string json)
    {
        var settings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<EmojiPuzzleDto>(json, settings);
    }

    public async Task<EmojiPuzzleDto?> EmojiSequenceParsed(Difficulty difficulty)
    {
        string raw = await EmojiSequence(difficulty);
        return ParseMovieJson(raw);
    }

    public async Task<TasteExplanationDto> ExplainUserTaste(List<UserMovieTasteDto> watchedMovies)
    {
        if (!watchedMovies.Any())
        {
            return new TasteExplanationDto
            {
                OverallTasteProfile = "Not enough data to analyze your taste yet. Watch and rate more movies!",
                WatchingPersonality = "Explorer - Still discovering your preferences"
            };
        }

        var moviesText = string.Join("\n", watchedMovies.Select(m =>
            $"- {m.MovieName} ({m.ReleaseYear}) - Genre: {m.Genre}, Director: {m.Director}, Your Rating: {m.UserRating}/10{(string.IsNullOrEmpty(m.UserComment) ? "" : $", Comment: \"{m.UserComment}\"")}"));

        var randomSeed = DateTime.UtcNow.Ticks % 100000;

        string prompt = $@"
        You are a film critic and psychologist analyzing a user's movie taste based on their viewing history and ratings.

        USER'S WATCHED MOVIES AND RATINGS:
        {moviesText}

        ANALYSIS SEED: {randomSeed}

        YOUR TASK:
        Provide a deep, insightful analysis of this user's movie taste. Be specific, thoughtful, and reference their actual movies.

        ANALYSIS REQUIREMENTS:
        1. Overall Taste Profile: 2-3 sentences summarizing their cinematic preferences
        2. Provide 4-6 specific insights about their taste in different categories
        3. Identify 3-5 favorite themes that appear across their movies
        4. List 2-4 preferred directors if patterns emerge
        5. Define their ""Watching Personality"" (e.g., ""The Intellectual Explorer"", ""The Emotional Storyteller"", ""The Thrill Seeker"")

        INSIGHT CATEGORIES (choose 4-6 most relevant):
        - Genre Preferences
        - Directorial Style
        - Narrative Complexity
        - Emotional Tone
        - Time Period Preferences
        - Character Development Focus
        - Cinematography Appreciation
        - Thematic Interests
        - Cultural Perspectives
        - Pacing Preferences

        IMPORTANT RULES:
        - Reference SPECIFIC movies from their list in your explanations
        - Notice patterns in their high vs low ratings
        - If they rated something low, mention what they might not enjoy
        - Be personal and insightful, not generic
        - Connect their movie preferences to deeper personality traits
        - Mention if they have eclectic taste vs. focused preferences

        JSON FORMAT (MANDATORY):
        {{
        ""overallTasteProfile"": ""2-3 sentence summary that references specific movies they watched"",
        ""insights"": [
            {{
            ""category"": ""Genre Preferences"",
            ""explanation"": ""Detailed explanation referencing specific movies and ratings"",
            ""exampleMovies"": [""Movie1"", ""Movie2"", ""Movie3""]
            }},
            {{
            ""category"": ""Another Category"",
            ""explanation"": ""Another specific insight"",
            ""exampleMovies"": [""Movie4"", ""Movie5""]
            }}
        ],
        ""favoriteThemes"": [""Theme 1"", ""Theme 2"", ""Theme 3""],
        ""preferredDirectors"": [""Director 1"", ""Director 2""],
        ""watchingPersonality"": ""Creative personality type name with 1 sentence explanation""
        }}

        OUTPUT RULES:
        - Output ONLY valid JSON
        - No markdown, no code blocks, no extra text
        - Reference actual movies from the user's list
        - Make it personal and insightful
        - 4-6 insights minimum

        Now analyze this user's taste with depth and specificity!
        ";

        string raw = await GenerateText(prompt);

        var settings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var result = JsonSerializer.Deserialize<TasteExplanationDto>(raw, settings);
            return result ?? new TasteExplanationDto
            {
                OverallTasteProfile = "Unable to analyze taste at this time.",
                WatchingPersonality = "Analysis pending"
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to parse taste explanation: {ex.Message}");
            Console.WriteLine($"Raw response: {raw}");

            return new TasteExplanationDto
            {
                OverallTasteProfile = "Unable to generate taste analysis. Please try again.",
                WatchingPersonality = "Analysis error"
            };
        }
    }

    public async Task<PersonalizedPlaylistDto> GeneratePersonalizedPlaylist(List<UserMovieTasteDto> recentActivity, int count = 10)
    {
        if (!recentActivity.Any())
        {
            return new PersonalizedPlaylistDto
            {
                PlaylistName = "Discover Movies",
                Description = "Start watching and rating movies to get personalized recommendations!",
                Movies = new List<PlaylistMovieDto>()
            };
        }

        var recentText = string.Join("\n", recentActivity.Take(20).Select(m =>
            $"- {m.MovieName} ({m.ReleaseYear}) - {m.Genre}, Rating: {m.UserRating}/10, Watched: {m.WatchedAt:yyyy-MM-dd}"));

        var randomSeed = DateTime.UtcNow.Ticks % 100000;

        string prompt = $@"
        You are a film curator creating a personalized movie playlist for a user based on their recent viewing history.

        USER'S RECENT ACTIVITY (last 20 movies):
        {recentText}

        DIVERSITY SEED: {randomSeed}

        YOUR TASK:
        Create a playlist of {count} movie recommendations that match this user's taste while introducing variety.

        PLAYLIST CREATION RULES:
        1. Base recommendations on patterns in their recent viewing
        2. Consider what they rated highly (7+ out of 10)
        3. Introduce variety - don't just recommend the same genre
        4. Include mix of:
        - Similar movies to their favorites (50%)
        - Adjacent genres they might enjoy (30%)
        - Surprising picks based on deeper patterns (20%)
        5. Consider release year diversity (classics + modern)
        6. Give each movie a recommendation score (70-98)

        IMPORTANT VARIETY RULES:
        - If seed is 0-25: Focus on SIMILAR genres to their recent favorites
        - If seed is 26-50: MIX different genres that share themes/tone
        - If seed is 51-75: Include HIDDEN GEMS and underrated films
        - If seed is 76-100: Add SURPRISING picks that expand their taste

        MOVIE RECOMMENDATION REQUIREMENTS:
        - Recommend REAL movies (verify they exist)
        - NO movies from their recent activity list
        - Vary release years (don't just recommend 2020s movies)
        - For each movie, explain WHY based on their viewing history
        - Recommendation scores should reflect confidence (higher for safer picks, lower for adventurous ones)

        PLAYLIST NAMING:
        - Create a catchy, personalized playlist name (e.g., ""Your Sci-Fi Thriller Journey"", ""Hidden Emotional Gems"")
        - NOT generic names like ""Recommended for You""

        JSON FORMAT (MANDATORY):
        {{
        ""playlistName"": ""Creative, Personalized Name"",
        ""description"": ""2-3 sentence description of what ties this playlist together and why it matches their taste"",
        ""reasoning"": ""1-2 sentences explaining the overall curation strategy"",
        ""movies"": [
            {{
            ""movieId"": 0,
            ""movieName"": ""Real Movie Title"",
            ""genre"": ""Genre"",
            ""releaseYear"": 2015,
            ""rating"": 8.2,
            ""posterUrl"": ""https://image.tmdb.org/t/p/w500/path.jpg"",
            ""whyRecommended"": ""Specific explanation referencing their viewing history (e.g., 'Since you loved [Movie X] and rated [Movie Y] highly, this shares [specific element]')"",
            ""recommendationScore"": 92
            }}
        ]
        }}

        CRITICAL REQUIREMENTS:
        - Return EXACTLY {count} movies
        - Movies array must have {count} items
        - Each movie needs ALL fields filled
        - movieId can be 0 (we'll match to database later)
        - whyRecommended MUST reference specific movies from their history
        - posterUrl should be realistic TMDB format or placeholder
        - Output ONLY valid JSON, no markdown, no code blocks

        Now create a thoughtful, diverse playlist of {count} movies!
        ";

        string raw = await GenerateText(prompt);

        var settings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var result = JsonSerializer.Deserialize<PersonalizedPlaylistDto>(raw, settings);

            if (result == null || !result.Movies.Any())
            {
                return new PersonalizedPlaylistDto
                {
                    PlaylistName = "Personalized Picks",
                    Description = "Recommendations based on your viewing history",
                    Movies = new List<PlaylistMovieDto>()
                };
            }

            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to parse playlist: {ex.Message}");
            Console.WriteLine($"Raw response: {raw}");
            LogAiError("Playlist Generation", raw);

            return new PersonalizedPlaylistDto
            {
                PlaylistName = "Discover More",
                Description = "Unable to generate playlist at this time",
                Movies = new List<PlaylistMovieDto>()
            };
        }
    }
    private void LogAiError(string context, string response)
    {
        Console.WriteLine($"[AI ERROR] {context}");
        Console.WriteLine($"[AI RESPONSE] {response.Substring(0, Math.Min(500, response.Length))}...");
    }
    
    public async Task<MovieTriviaDto> GenerateMovieTrivia(string movieName, int releaseYear, string genre, string director, string description, int questionCount = 10)
    {
        var randomSeed = DateTime.UtcNow.Ticks % 100000;
        
        string prompt = $@"
        You are creating a movie trivia quiz for '{movieName}' ({releaseYear}).

        MOVIE INFORMATION:
        - Title: {movieName}
        - Year: {releaseYear}
        - Genre: {genre}
        - Director: {director}
        - Description: {description}

        RANDOMIZATION SEED: {randomSeed}

        YOUR TASK:
        Generate {questionCount} multiple-choice trivia questions about this specific movie.

        QUESTION REQUIREMENTS:
        1. Questions must be SPECIFIC to this movie (not generic film questions)
        2. Mix of difficulty levels:
        - 30% Easy (obvious from watching once)
        - 50% Medium (requires attention to detail)
        - 20% Hard (for true fans)
        3. Question categories to include:
        - Plot details and key events
        - Character names and relationships
        - Memorable quotes (if applicable)
        - Production facts (if relevant to movie knowledge)
        - Themes and symbolism
        - Specific scenes or sequences
        4. Each question must have EXACTLY 4 options
        5. Only ONE option is correct
        6. Wrong options should be plausible but clearly incorrect
        7. Avoid questions that require external knowledge (like box office numbers)

        IMPORTANT RULES:
        - Questions should test MOVIE KNOWLEDGE, not trivia about actors' personal lives
        - Avoid yes/no questions - always provide 4 distinct options
        - Make wrong answers believable (not obviously fake)
        - Randomize correct answer position (don't always make it option A)

        EXAMPLE GOOD QUESTIONS:
        - ""What was the name of the city where the heist took place?""
        - ""Which character said the line '[memorable quote]'?""
        - ""What object did [character] use to [plot event]?""
        - ""In which year was the movie set?""

        EXAMPLE BAD QUESTIONS:
        - ""Is this a good movie?"" (opinion)
        - ""How much did the movie gross?"" (external trivia)
        - ""What other movies has the director made?"" (not about THIS movie)

        JSON FORMAT (MANDATORY):
        {{
        ""movieId"": 0,
        ""movieName"": ""{movieName}"",
        ""questions"": [
            {{
            ""question"": ""Specific question about the movie?"",
            ""options"": [
                ""Option A"",
                ""Option B"",
                ""Option C"",
                ""Option D""
            ],
            ""correctAnswerIndex"": 2
            }}
        ]
        }}

        OUTPUT RULES:
        - Output ONLY valid JSON
        - No markdown, no code blocks, no extra text
        - Generate EXACTLY {questionCount} questions
        - correctAnswerIndex must be 0, 1, 2, or 3
        - Vary the correctAnswerIndex across questions (don't always use 0)

        Now generate {questionCount} high-quality trivia questions for '{movieName}'!
        ";

        string raw = await GenerateText(prompt);

        var settings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var result = JsonSerializer.Deserialize<MovieTriviaDto>(raw, settings);
            
            if (result == null || !result.Questions.Any())
            {
                return new MovieTriviaDto
                {
                    MovieName = movieName,
                    Questions = new List<TriviaQuestionDto>()
                };
            }

            result.MovieName = movieName;
            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to parse trivia: {ex.Message}");
            Console.WriteLine($"Raw response: {raw}");
            
            return new MovieTriviaDto
            {
                MovieName = movieName,
                Questions = new List<TriviaQuestionDto>()
            };
        }
    }
}


public class GeminiResponse
{
    public required Candidate[] candidates { get; set; }
}

public class Candidate
{
    public required Content content { get; set; }
}

public class Content
{
    public required Part[] parts { get; set; }
}

public class Part
{
    public required string text { get; set; }
}

