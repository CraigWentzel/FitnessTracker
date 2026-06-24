using FitnessTracker.Core.Interfaces;
using FitnessTracker.Core.Models;
using FitnessTracker.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitnessTracker.Web.Pages;

/// <summary>
/// Server-rendered training dashboard. Pulls the same data the API controllers
/// expose, but renders it directly into the page rather than requiring a
/// separate JS app - simplest possible path to a real, working frontend.
/// </summary>
public class DashboardModel : PageModel
{
    private readonly IActivityRepository _activityRepo;
    private readonly IThresholdRepository _thresholdRepo;
    private readonly TrainingLoadCalculator _loadCalculator;

    public DashboardModel(
        IActivityRepository activityRepo,
        IThresholdRepository thresholdRepo,
        TrainingLoadCalculator loadCalculator)
    {
        _activityRepo = activityRepo;
        _thresholdRepo = thresholdRepo;
        _loadCalculator = loadCalculator;
    }

    [BindProperty(SupportsGet = true)]
    public int AthleteId { get; set; } = 1;

    public bool HasAnyActivities { get; private set; }

    public List<FitnessTrendPoint> Trend { get; private set; } = new();
    public double LatestForm { get; private set; }
    public double LatestChronic { get; private set; }
    public double LatestAcute { get; private set; }

    public Dictionary<SportType, SportSummary> SportSummaries { get; private set; } = new();
    public List<Activity> RecentActivities { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var to = DateTime.UtcNow.Date;
        var from = to.AddDays(-90);

        var allActivities = await _activityRepo.GetByAthleteAsync(AthleteId, from: from, to: to);
        HasAnyActivities = allActivities.Count > 0;

        if (!HasAnyActivities) return;

        var allThresholds = new List<Threshold>();
        foreach (var sport in new[] { SportType.Run, SportType.Bike, SportType.Swim })
            allThresholds.AddRange(await _thresholdRepo.GetHistoryAsync(AthleteId, sport));

        Trend = _loadCalculator.CalculateFitnessTrend(allActivities, allThresholds, from, to);

        var latest = Trend.LastOrDefault();
        if (latest is not null)
        {
            LatestForm = latest.Form;
            LatestChronic = latest.ChronicLoad;
            LatestAcute = latest.AcuteLoad;
        }

        foreach (var sport in new[] { SportType.Run, SportType.Bike, SportType.Swim })
        {
            var sportActivities = allActivities.Where(a => a.Sport == sport).ToList();
            if (sportActivities.Count == 0) continue;

            SportSummaries[sport] = new SportSummary
            {
                ActivityCount = sportActivities.Count,
                TotalDistanceKm = Math.Round(sportActivities.Sum(a => a.DistanceMeters) / 1000.0, 1),
                TotalMovingTimeHours = Math.Round(sportActivities.Sum(a => a.MovingTime.TotalHours), 1),
                AvgPaceSecPerKm = sportActivities.Where(a => a.AvgPaceSecPerKm.HasValue)
                    .Select(a => a.AvgPaceSecPerKm!.Value).DefaultIfEmpty(0).Average(),
                AvgSpeedKph = sportActivities.Where(a => a.AvgSpeedKph.HasValue)
                    .Select(a => a.AvgSpeedKph!.Value).DefaultIfEmpty(0).Average(),
                AvgPowerWatts = sportActivities.Where(a => a.AvgPowerWatts.HasValue)
                    .Select(a => a.AvgPowerWatts!.Value).DefaultIfEmpty(0).Average(),
            };
        }

        RecentActivities = allActivities.Take(8).ToList();
    }

    public class SportSummary
    {
        public int ActivityCount { get; set; }
        public double TotalDistanceKm { get; set; }
        public double TotalMovingTimeHours { get; set; }
        public double AvgPaceSecPerKm { get; set; }
        public double AvgSpeedKph { get; set; }
        public double AvgPowerWatts { get; set; }
    }
}
