using FitnessTracker.Core.Interfaces;
using FitnessTracker.Core.Models;
using FitnessTracker.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitnessTracker.Web.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IActivityRepository _activityRepo;
    private readonly IThresholdRepository _thresholdRepo;
    private readonly TrainingLoadCalculator _loadCalculator;

    public DashboardController(
        IActivityRepository activityRepo,
        IThresholdRepository thresholdRepo,
        TrainingLoadCalculator loadCalculator)
    {
        _activityRepo = activityRepo;
        _thresholdRepo = thresholdRepo;
        _loadCalculator = loadCalculator;
    }

    /// <summary>Overall fitness trend (Acute/Chronic/Form) across all sports combined.</summary>
    [HttpGet("fitness-trend/{athleteId}")]
    public async Task<IActionResult> GetFitnessTrend(int athleteId, [FromQuery] int days = 90)
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-days);

        var activities = await _activityRepo.GetByAthleteAsync(athleteId, from: from, to: to);

        var allThresholds = new List<Threshold>();
        foreach (var sport in new[] { SportType.Run, SportType.Bike, SportType.Swim })
            allThresholds.AddRange(await _thresholdRepo.GetHistoryAsync(athleteId, sport));

        var trend = _loadCalculator.CalculateFitnessTrend(activities, allThresholds, from, to);
        return Ok(trend);
    }

    /// <summary>Per-sport summary: volume, pace/speed/power trend, and recent activities.</summary>
    [HttpGet("sport-summary/{athleteId}/{sport}")]
    public async Task<IActionResult> GetSportSummary(int athleteId, SportType sport, [FromQuery] int days = 90)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var activities = await _activityRepo.GetByAthleteAsync(athleteId, sport, from);

        if (activities.Count == 0)
            return Ok(new { sport, activityCount = 0, message = "No activities found for this period." });

        var summary = new
        {
            sport,
            activityCount = activities.Count,
            totalDistanceKm = Math.Round(activities.Sum(a => a.DistanceMeters) / 1000.0, 1),
            totalMovingTimeHours = Math.Round(activities.Sum(a => a.MovingTime.TotalHours), 1),
            avgPaceSecPerKm = activities.Where(a => a.AvgPaceSecPerKm.HasValue).Select(a => a.AvgPaceSecPerKm!.Value).DefaultIfEmpty(0).Average(),
            avgSpeedKph = activities.Where(a => a.AvgSpeedKph.HasValue).Select(a => a.AvgSpeedKph!.Value).DefaultIfEmpty(0).Average(),
            avgPowerWatts = activities.Where(a => a.AvgPowerWatts.HasValue).Select(a => a.AvgPowerWatts!.Value).DefaultIfEmpty(0).Average(),
            recentActivities = activities.Take(10).Select(a => new
            {
                a.Id,
                a.StartTimeUtc,
                a.DistanceMeters,
                a.FormattedPace,
                a.AvgSpeedKph,
                a.AvgPowerWatts,
                a.AvgHeartRate,
                a.TrainingLoad
            })
        };

        return Ok(summary);
    }

    /// <summary>Threshold progression history for charting how a sport's threshold has changed over time.</summary>
    [HttpGet("threshold-history/{athleteId}/{sport}")]
    public async Task<IActionResult> GetThresholdHistory(int athleteId, SportType sport)
    {
        var history = await _thresholdRepo.GetHistoryAsync(athleteId, sport);
        return Ok(history);
    }
}
