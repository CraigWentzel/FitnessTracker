# Fitness Tracker — Triathlon Dashboard

A personal training-analysis dashboard built on top of Garmin Connect's exported
`.FIT` activity files. Tracks swim, bike, and run training, computes a training
load trend (acute/chronic/form), and estimates sport-specific thresholds
(run threshold pace, bike FTP, swim CSS) from real test results.

No paid API subscriptions required — built around Garmin's free, official FIT SDK
and your own exported activity files.

## Why FIT file import instead of a live Strava/Garmin API?

Strava's developer API now requires an active Strava subscription as of June 2026.
Garmin's own developer API access is invite-only for most individual developers.
Garmin Connect, however, lets you export your full activity history as `.FIT` files
for free, any time — this project builds against that, using Garmin's own free,
official FIT C# SDK (`Garmin.FIT.Sdk` on NuGet) to parse them.

A live-sync v2 (Strava OAuth or Garmin API) is straightforward to add later — the
`ActivitySource` enum and `IFitFileParser` interface already anticipate it; you'd
add a second parser implementation behind the same `IActivityRepository` boundary.

## Project structure

```
FitnessTracker.sln
src/
  FitnessTracker.Core/            -- domain models, interfaces, calculation logic
                                       (no external dependencies - pure C#)
    Models/                      -- Activity, Athlete, Threshold, enums
    Interfaces/                  -- repository + parser contracts
    Services/
      TrainingLoadCalculator.cs  -- acute/chronic/form fitness trend math
      ThresholdEstimator.cs      -- Riegel run formula, FTP %-rules, swim CSS

  FitnessTracker.Infrastructure/ -- EF Core, FIT file parsing, repositories
    Data/FitnessTrackerDbContext.cs
    FitParsing/GarminFitFileParser.cs
    Repositories/

  FitnessTracker.Web/            -- ASP.NET Core Web API
    Controllers/
      ImportController.cs        -- POST /api/import/fit-files
      DashboardController.cs     -- GET  /api/dashboard/...
      ThresholdsController.cs    -- POST /api/thresholds/run|bike|swim
    Program.cs
```

## Getting your data

1. Log in to [Garmin Connect](https://connect.garmin.com) in a browser.
2. Go to **Account Settings → Data Management → Export Your Data** (or per-activity:
   open an activity → gear icon → **Export to FIT**).
3. Garmin emails you a download link (bulk export) or gives you a direct `.fit`
   download (single activity). Unzip if it's a bulk export.
4. Upload the resulting `.fit` files via the `/api/import/fit-files` endpoint
   (Swagger UI makes this easy to test — see below).

## Running it locally

**Prerequisites:** .NET 8 SDK, SQL Server (LocalDB is fine for development).

```bash
# from the solution root
dotnet restore
dotnet build

# create the database (run from src/FitnessTracker.Web)
dotnet tool install --global dotnet-ef   # one-time
dotnet ef migrations add InitialCreate --project ../FitnessTracker.Infrastructure --startup-project .
dotnet ef database update --project ../FitnessTracker.Infrastructure --startup-project .

dotnet run --project src/FitnessTracker.Web
```

Then open the Swagger UI (URL printed in the console, typically
`https://localhost:xxxx/swagger`) to:
1. `POST /api/import/fit-files` — upload your exported `.fit` files for an athlete ID
2. `POST /api/thresholds/run` (or `/bike`, `/swim`) — submit a recent test result to
   establish a starting threshold
3. `GET /api/dashboard/fitness-trend/{athleteId}` — see your acute/chronic/form trend
4. `GET /api/dashboard/sport-summary/{athleteId}/{sport}` — per-sport stats

You'll need to manually insert one `Athlete` row to start (no registration UI yet —
this is a single-user personal tool, not a multi-tenant product). Easiest via SSMS
or `dotnet ef` seed data.

## A note on the FIT SDK integration

`GarminFitFileParser.cs` uses Garmin's official `Decode` + `MesgBroadcaster` pattern
from the `Garmin.FIT.Sdk` NuGet package. This pattern has been stable across SDK
versions for years, but exact property getter names on `SessionMesg`/`RecordMesg`
can shift slightly between SDK releases. If you hit a compile error here on first
build, open the SDK's own `Examples/Decode` project (included in the NuGet package
source / on [GitHub](https://github.com/garmin/fit-csharp-sdk)) and cross-check the
property names against what's referenced in the parser — everything else in the
solution is independent of this detail.

## What's deliberately NOT built yet (good "v2" talking points in an interview)

- No live Strava/Garmin OAuth sync (file-import only, by design — see above)
- No authentication/multi-user support (single athlete, personal tool)
- Weather-history overlay (the `StartLatitude`/`StartLongitude` fields are captured
  per-activity specifically to make this easy to bolt on later via a free tier of
  something like Open-Meteo's historical weather API)
- No frontend yet — this is the API layer; a Blazor or React dashboard consumes it
