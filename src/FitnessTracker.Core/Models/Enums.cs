namespace FitnessTracker.Core.Models;

public enum SportType
{
    Unknown = 0,
    Run = 1,
    Bike = 2,
    Swim = 3,
    Other = 4
}

public enum ActivitySource
{
    FitFileImport = 0,
    StravaApi = 1,      // reserved for future v2 live-sync
    GarminApi = 2        // reserved for future v2 live-sync
}
