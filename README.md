
## Overview
**Filmder** is a social movie platform backend designed to help groups decide what to watch faster.
#Project Demo: https://youtu.be/1tPNNiYGozk

Choosing a movie in a group is usually messy: everyone has different preferences, conversations get fragmented, and decision fatigue slows everything down. 
Filmder solves this by combining structured discovery flows (swipes, ratings, watchlists), social coordination (groups + real-time chat), 
and personalization features (taste analysis and AI-assisted recommendation endpoints).


## Key Features
- **Secure authentication:** JWT-based login and protected endpoints.
- **Group collaboration:** group creation, membership flows, and shared movie actions.
- **Interactive discovery:** swipe and rating features to make movie selection engaging.
- **Watchlist management:** save and track movies for future viewing.
- **Real-time communication:** SignalR hubs for chat and watch-party coordination.
- **Mini-games:** higher/lower, emoji game, movie trivia, and guess-rating endpoints.
- **AI + external integrations:** TMDB movie data and Gemini-powered recommendation/taste experiences.
- **Media storage integration:** Supabase + bucket-based profile picture storage.
- **Production-minded engineering:** centralized exception middleware, rate limiting, structured logging, and test coverage via xUnit.

## Tech Stack
- **Backend:** C#, ASP.NET Core 9 Web API, SignalR, ASP.NET Identity
- **Database:** PostgreSQL, Entity Framework Core
- **Authentication & Security:** JWT Bearer, Identity roles, ASP.NET rate limiting
- **External Services:**
  - TMDB API (movie metadata and discovery)
  - Gemini API (AI-assisted recommendation/taste features)
  - Supabase (storage integration)
  - Supabase Buckets (profile picture asset storage)
  - SMTP email provider (account recovery and communication workflows)
- **Observability & Docs:** Serilog, Swagger/OpenAPI
- **Testing & Tooling:** xUnit, Moq-style mocking patterns, Docker

## Architecture 
Filmder uses a **layered architecture** focused on separation of concerns and testability.

## What we Learned
- Designing a maintainable API using controllers + services + repositories.
- Implementing secure identity flows with JWT and role-aware access patterns.
- Balancing feature complexity (real-time chat, games, recommendations) with clean separation of concerns.
- Integrating external systems safely, including TMDB/Gemini and Supabase bucket storage for user media.
- Improving resilience with middleware-driven error handling, logging, and request throttling.
- Writing testable components and unit tests around services/controllers.

## Future Improvements
- Add a production CI/CD pipeline (build, test, security scan, deploy).
- Introduce Redis caching and background workers for heavy recommendation workloads.
- Add distributed tracing/metrics dashboards for observability.
- Expand integration/contract tests for external APIs and storage adapters.
- Provide Docker Compose for one-command local full-stack setup.

## ------------------------Setup--------------------

## Prerequisites
- .NET 9 SDK
- PostgreSQL database
- A Supabase account
- A Gemini API key
- A TMDB API key
- An SMTP email provider (Gmail works)

## Required Configuration

The app uses `appsettings.Development.json` (gitignored) for secrets.
Create this file in the `Filmder/Filmder/` directory:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "TokenKey": "",
  "Gemini": {
    "ApiKey": ""
  },
  "TmdbApiKey": "",
  "EmailSettings": {
    "SenderEmail": "",
    "SenderPassword": ""
  },
  "Supabase": {
    "Url": "",
    "Key": "",
    "Buckets": {
      "ProfilePictures": ""
    }
  }
}
```

## Running Locally
```bash
# Clone the repo
git clone https://github.com/DomceKebabas/Filmder
cd filmder/Filmder

# Apply database migrations
dotnet ef database update --project Filmder

# Run the API
dotnet run --project Filmder

# Swagger UI available at:
# http://localhost:5144/swagger
```

## Running Tests
```bash
cd Filmder
dotnet test
```
```

---
