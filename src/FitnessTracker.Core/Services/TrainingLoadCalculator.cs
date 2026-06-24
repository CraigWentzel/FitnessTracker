using FitnessTracker.Core.Models;

namespace FitnessTracker.Core.Services;

/// <summary>
/// Computes a unitless "Training Load" score per activity, and rolls those scores up
/// into Acute (7-day) and Chronic (28-day) load, plus a Form score - the same logic
/// behind tools like TrainingPeaks' TSS/CTL/ATL, simplified for a portfolio build.
///
/// Why this matters for the dashboard:
/// - A single run/ride/swim's "load" lets you compare effort across sports on one scale.
/// - Chronic Load (28-day rolling avg) ≈ your current fitness level.
/// - Acute Load (7-day rolling avg) ≈ how fatigued you are right now.
/// - Form = Chronic - Acute. Positive = fresh/tapered. Very negative = overreaching.
/// </summary>
public class TrainingLoadCalculator
{
    /// <summary>
    /// Estimates training load for one activity using heart-rate-based effort if available,
    /// falling back to a duration-only estimate when no HR data exists (common on swims
    /// without a chest strap, or older recordings).
    /// </summary>
    public double CalculateLoad(Activity activity, Threshold? threshold)
    {
        var durationMinutes = activity.MovingTime.TotalMinutes;
        if (durationMinutes <= 0) return 0;

        // Bike with power data: load scales off Normalized Power vs FTP (intensity factor).
        if (activity.Sport == SportType.Bike && activity.NormalizedPowerWatts is > 0 && threshold?.FtpWatts is > 0)
        {
            var intensityFactor = activity.NormalizedPowerWatts.Value / threshold.FtpWatts.Value;
            // TSS formula: (duration_sec * NP * IF) / (FTP * 3600) * 100
            return durationMinutes * 60 * activity.NormalizedPowerWatts.Value * intensityFactor
                   / (threshold.FtpWatts.Value * 3600) * 100;
        }

        // Run/Swim with a known threshold pace: load scales off how close to threshold pace you held.
        if (activity.AvgPaceSecPerKm is > 0 && threshold?.ThresholdPaceSecPerKm is > 0)
        {
            // Faster pace = lower seconds/km = higher intensity factor.
            var intensityFactor = threshold.ThresholdPaceSecPerKm.Value / activity.AvgPaceSecPerKm.Value;
            return durationMinutes * Math.Pow(intensityFactor, 2) * 1.0; // squared to reward sustained intensity
        }

        // Fallback: no threshold set yet, or no HR/power data. Use a flat duration-based estimate
        // so the dashboard still shows *something* meaningful rather than a blank chart.
        // ~1 load point per minute at "moderate" effort is a reasonable rough default.
        return durationMinutes * 0.8;
    }

    /// <summary>
    /// Rolls up daily load values into Acute (7d), Chronic (28d), and Form for each day
    /// in the range - this is what feeds the "overall fitness" trend chart.
    /// </summary>
    public List<FitnessTrendPoint> CalculateFitnessTrend(List<Activity> activities, List<Threshold> thresholds, DateTime from, DateTime to)
    {
        var dailyLoad = activities
            .GroupBy(a => a.StartTimeUtc.Date)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.TrainingLoad ?? 0));

        var trend = new List<FitnessTrendPoint>();
        double chronicLoad = 0, acuteLoad = 0;

        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            var todayLoad = dailyLoad.GetValueOrDefault(day, 0);

            // Exponentially weighted rolling averages (standard approach, smoother than a flat window).
            acuteLoad = ExponentialAverage(acuteLoad, todayLoad, days: 7);
            chronicLoad = ExponentialAverage(chronicLoad, todayLoad, days: 28);

            trend.Add(new FitnessTrendPoint
            {
                Date = day,
                DailyLoad = todayLoad,
                AcuteLoad = Math.Round(acuteLoad, 1),
                ChronicLoad = Math.Round(chronicLoad, 1),
                Form = Math.Round(chronicLoad - acuteLoad, 1)
            });
        }

        return trend;
    }

    private static double ExponentialAverage(double previousAverage, double todayValue, int days)
    {
        var alpha = 2.0 / (days + 1);
        return alpha * todayValue + (1 - alpha) * previousAverage;
    }
}

public class FitnessTrendPoint
{
    public DateTime Date { get; set; }
    public double DailyLoad { get; set; }
    public double AcuteLoad { get; set; }
    public double ChronicLoad { get; set; }
    public double Form { get; set; }
}
