using System.Text;
using System.Threading.RateLimiting;
using Filmder.Data;
using Filmder.Interfaces;
using Filmder.Middleware;
using Filmder.Models;
using Filmder.Repositories;
using Filmder.Services;
using Filmder.Signal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()          
    .WriteTo.Console()                   
    .WriteTo.File(
        "Logs/log.txt",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error
    )                                    
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Filmder API",
        Description = "API for Filmder application"
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ExpensiveDaily", opt =>
    {
        opt.PermitLimit = 200;
        opt.Window = TimeSpan.FromHours(24);
    });

    options.AddTokenBucketLimiter("DefaultBucket", opt =>
    {
        opt.TokenLimit = 50;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        opt.TokensPerPeriod = 10;
        opt.AutoReplenishment = true;
    });

    options.AddSlidingWindowLimiter("SlidingLimiter", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromSeconds(30);
        opt.SegmentsPerWindow = 3;
        opt.QueueLimit = 5;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

    builder.Services.AddHttpClient<IAIService, GeminiAiService>();
    builder.Services.AddSingleton<TmdbApiService>();

    builder.Services.AddTransient<IEmailSender, EmailSender>();
    builder.Services.AddScoped<SupabaseService>();

    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<MovieImportService>();
    builder.Services.AddScoped<IMovieCacheService, MovieCacheService>();

    builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
    builder.Services.AddScoped<IWatchlistService, WatchlistService>();

    builder.Services.AddScoped<ITasteExplainerRepository, TasteExplainerRepository>();
    builder.Services.AddScoped<ITasteExplainerService, TasteExplainerService>();

    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
    builder.Services.AddScoped<IAccountService, AccountService>();

    builder.Services.AddScoped<IEmojiPuzzleRepository, EmojiPuzzleRepository>();
    builder.Services.AddScoped<IEmojiGameService, EmojiGameService>();

    builder.Services.AddScoped<IGameRepository, GameRepository>();
    builder.Services.AddScoped<IGameService, GameService>();

    builder.Services.AddScoped<IGroupRepository, GroupRepository>();
    builder.Services.AddScoped<IGroupService, GroupService>();

    builder.Services.AddScoped<IGroupStatsRepository, GroupStatsRepository>();
    builder.Services.AddScoped<IGroupStatsService, GroupStatsService>();

    builder.Services.AddScoped<IGuessRatingGameRepository, GuessRatingGameRepository>();
    builder.Services.AddScoped<IGuessRatingGameService, GuessRatingGameService>();

    builder.Services.AddScoped<IHigherLowerRepository, HigherLowerRepository>();
    builder.Services.AddScoped<IHigherLowerService, HigherLowerService>();

    builder.Services.AddScoped<IMessageRepository, MessageRepository>();
    builder.Services.AddScoped<IMessageService, MessageService>();

    builder.Services.AddScoped<IPersonalizedPlaylistRepository, PersonalizedPlaylistRepository>();
    builder.Services.AddScoped<IPersonalizedPlaylistService, PersonalizedPlaylistService>();

    builder.Services.AddScoped<IPersonalityMatchRepository, PersonalityMatchRepository>();
    builder.Services.AddScoped<IPersonalityMatchService, PersonalityMatchService>();

    builder.Services.AddScoped<IMovieTriviaRepository, MovieTriviaRepository>();
    builder.Services.AddScoped<IMovieTriviaService, MovieTriviaService>();

    builder.Services.AddScoped<IMovieRepository, MovieRepository>();
    builder.Services.AddScoped<IRatingRepository, RatingRepository>();

    builder.Services.AddScoped<IMovieService, MovieService>();
    builder.Services.AddScoped<IRatingService, RatingService>();

    builder.Services.AddScoped<ISwipeRepository, SwipeRepository>();
    builder.Services.AddScoped<ISwipeService, SwipeService>();

    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();

    builder.Services.AddScoped<IMoodRepository, MoodRepository>();
    builder.Services.AddScoped<IMoodService, MoodService>();

    builder.Services.AddSignalR();

    builder.Services.AddIdentityCore<AppUser>(opt =>
        {
            opt.Password.RequireDigit = false;
            opt.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddRoleManager<RoleManager<IdentityRole>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager<SignInManager<AppUser>>()
        .AddDefaultTokenProviders();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        var tokenKey = builder.Configuration["TokenKey"] ?? throw new Exception("token key not found - program.cs");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(origin =>
                    origin.StartsWith("chrome-extension://") ||
                    origin == "http://localhost:5173"
                );
        });
    });

    var app = builder.Build();

    app.UseRateLimiter();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseStaticFiles();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Filmder API V1"); });
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHub<WatchPartyHub>("/watchPartyHub");
    app.MapHub<ChatHub>("/chatHub");
    app.MapControllers();

    app.Run();