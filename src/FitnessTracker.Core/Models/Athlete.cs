namespace FitnessTracker.Core.Models;

public class Athlete
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public double? WeightKg { get; set; }

    public List<Activity> Activities { get; set; } = new();
    public List<Threshold> Thresholds { get; set; } = new();
}

/// <summary>
/// A point-in-time fitness threshold for one sport. New rows are added as fitness
/// changes over time, rather than overwriting - this lets the dashboard chart
/// threshold progression, not just show the current number.
/// </summary>
public class Threshold
{
    public int Id { get; set; }
    public int AthleteId { get; set; }
    public Athlete? Athlete { get; set; }

    public SportType Sport { get; set; }
    public DateTime EffectiveDate { get; set; }

    /// <summary>Run: threshold pace in sec/km. Swim: Critical Swim Speed in sec/100m. Bike: not used here (see FtpWatts).</summary>
    public double? ThresholdPaceSecPerKm { get; set; }

    /// <summary>Bike only - Functional Threshold Power.</summary>
    public double? FtpWatts { get; set; }

    /// <summary>How this threshold was determined, e.g. "20-min test", "Race result", "Estimated from FIT data".</summary>
    public string Source { get; set; } = string.Empty;
}
