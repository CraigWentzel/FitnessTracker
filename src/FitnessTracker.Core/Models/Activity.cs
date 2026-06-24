namespace FitnessTracker.Core.Models;

/// <summary>
/// A single workout session (run, bike, or swim), parsed from a FIT file.
/// This is the central entity the whole dashboard is built on.
/// </summary>
public class Activity
{
    public int Id { get; set; }

    public int AthleteId { get; set; }
    public Athlete? Athlete { get; set; }

    public SportType Sport { get; set; }
    public ActivitySource Source { get; set; } = ActivitySource.FitFileImport;

    public DateTime StartTimeUtc { get; set; }

    /// <summary>Total elapsed time, including stops.</summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>Time actually moving (excludes pauses at traffic lights, rest at the wall, etc).</summary>
    public TimeSpan MovingTime { get; set; }

    public double DistanceMeters { get; set; }

    public double? AvgHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }

    /// <summary>Run/Swim: seconds per km equivalent. Bike: not used (see AvgPowerWatts/AvgSpeedKph).</summary>
    public double? AvgPaceSecPerKm { get; set; }

    /// <summary>Bike only - average speed.</summary>
    public double? AvgSpeedKph { get; set; }

    /// <summary>Bike only - if a power meter was used.</summary>
    public double? AvgPowerWatts { get; set; }
    public double? NormalizedPowerWatts { get; set; }

    public double? TotalElevationGainMeters { get; set; }

    /// <summary>Latitude/longitude of the activity start, used for weather lookups.</summary>
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }

    /// <summary>Original filename, kept for traceability/debugging re-imports.</summary>
    public string? SourceFileName { get; set; }

    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;

    // ── Derived / calculated fields (computed at import time, stored for fast dashboard reads) ──

    /// <summary>Training Stress Score equivalent - a unitless measure of how hard this session was.</summary>
    public double? TrainingLoad { get; set; }

    /// <summary>Helper - pace formatted as min:sec per km, e.g. "5:12/km". Null for bike.</summary>
    public string? FormattedPace =>
        AvgPaceSecPerKm is null
            ? null
            : $"{(int)(AvgPaceSecPerKm / 60)}:{(int)(AvgPaceSecPerKm % 60):D2}/km";
}
