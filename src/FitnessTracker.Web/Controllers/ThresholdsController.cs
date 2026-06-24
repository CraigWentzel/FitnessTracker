using FitnessTracker.Core.Interfaces;
using FitnessTracker.Core.Models;
using FitnessTracker.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitnessTracker.Web.Controllers;

public record RunThresholdRequest(int AthleteId, double TestDistanceKm, int TestTimeSeconds);
public record BikeFtpRequest(int AthleteId, double AvgPowerWatts, int TestDurationMinutes); // 20 or 8
public record SwimCssRequest(int AthleteId, double Distance1M, int Time1Seconds, double Distance2M, int Time2Seconds);

[ApiController]
[Route("api/thresholds")]
public class ThresholdsController : ControllerBase
{
    private readonly ThresholdEstimator _estimator;
    private readonly IThresholdRepository _thresholdRepo;

    public ThresholdsController(ThresholdEstimator estimator, IThresholdRepository thresholdRepo)
    {
        _estimator = estimator;
        _thresholdRepo = thresholdRepo;
    }

    [HttpPost("run")]
    public async Task<IActionResult> CalculateRunThreshold(RunThresholdRequest req)
    {
        var paceSecPerKm = _estimator.EstimateRunThresholdPaceSecPerKm(req.TestDistanceKm, TimeSpan.FromSeconds(req.TestTimeSeconds));

        var threshold = await _thresholdRepo.AddAsync(new Threshold
        {
            AthleteId = req.AthleteId,
            Sport = SportType.Run,
            EffectiveDate = DateTime.UtcNow,
            ThresholdPaceSecPerKm = paceSecPerKm,
            Source = $"Riegel estimate from {req.TestDistanceKm}km test"
        });

        return Ok(threshold);
    }

    [HttpPost("bike")]
    public async Task<IActionResult> CalculateBikeFtp(BikeFtpRequest req)
    {
        var ftp = req.TestDurationMinutes switch
        {
            20 => _estimator.EstimateFtpFrom20MinTest(req.AvgPowerWatts),
            8 => _estimator.EstimateFtpFrom8MinTest(req.AvgPowerWatts),
            _ => throw new ArgumentException("Test duration must be 20 or 8 minutes.")
        };

        var threshold = await _thresholdRepo.AddAsync(new Threshold
        {
            AthleteId = req.AthleteId,
            Sport = SportType.Bike,
            EffectiveDate = DateTime.UtcNow,
            FtpWatts = ftp,
            Source = $"{req.TestDurationMinutes}-min test ({(req.TestDurationMinutes == 20 ? "95%" : "90%")} rule)"
        });

        return Ok(threshold);
    }

    [HttpPost("swim")]
    public async Task<IActionResult> CalculateSwimCss(SwimCssRequest req)
    {
        var cssSecPer100m = _estimator.EstimateCssSecPer100m(
            req.Distance1M, TimeSpan.FromSeconds(req.Time1Seconds),
            req.Distance2M, TimeSpan.FromSeconds(req.Time2Seconds));

        var threshold = await _thresholdRepo.AddAsync(new Threshold
        {
            AthleteId = req.AthleteId,
            Sport = SportType.Swim,
            EffectiveDate = DateTime.UtcNow,
            ThresholdPaceSecPerKm = cssSecPer100m * 10, // convert sec/100m to sec/km for consistent storage
            Source = $"CSS from {req.Distance1M}m/{req.Distance2M}m trials"
        });

        return Ok(threshold);
    }
}
