using FitnessTracker.Core.Models;

namespace FitnessTracker.Core.Services;

/// <summary>
/// Estimates fitness thresholds (run threshold pace, bike FTP, swim CSS) from either
/// a structured test result or your best recent activities. These are standard,
/// well-established sports-science formulas - not invented here.
/// </summary>
public class ThresholdEstimator
{
    /// <summary>
    /// Run threshold pace from a recent best effort, using Riegel's formula to normalize
    /// any race/test distance to an equivalent threshold (~60 min) effort.
    /// Reference: Riegel, P.S. (1981), "Athletic Records and Human Endurance".
    /// </summary>
    public double EstimateRunThresholdPaceSecPerKm(double testDistanceKm, TimeSpan testTime)
    {
        const double fatigueFactor = 1.06; // standard Riegel exponent
        const double thresholdMinutes = 60.0;

        var testMinutes = testTime.TotalMinutes;
        var predictedThresholdMinutes = testMinutes * Math.Pow(thresholdMinutes / testMinutes, 1 / fatigueFactor);

        // This formula predicts total time for 60 min worth of *equivalent distance*,
        // so back-solve for pace: distance covered in that time scales the same way.
        var predictedDistanceKm = testDistanceKm * Math.Pow(thresholdMinutes / testMinutes, 1 / fatigueFactor);
        return (predictedThresholdMinutes * 60) / predictedDistanceKm;
    }

    /// <summary>
    /// Bike FTP from a 20-minute test, using the standard 95% rule.
    /// Reference: Coggan/Allen, "Training and Racing with a Power Meter".
    /// </summary>
    public double EstimateFtpFrom20MinTest(double avgPowerWatts20Min)
    {
        return avgPowerWatts20Min * 0.95;
    }

    /// <summary>
    /// Bike FTP from an 8-minute test, using the standard 90% rule (alternative protocol
    /// when a full 20-min test isn't practical).
    /// </summary>
    public double EstimateFtpFrom8MinTest(double avgPowerWatts8Min)
    {
        return avgPowerWatts8Min * 0.90;
    }

    /// <summary>
    /// Swim Critical Swim Speed (CSS) from two timed trials at different distances
    /// (classically 400m and 200m), expressed as sec/100m.
    /// Reference: Critical Speed concept applied to swimming (Wakayoshi et al., 1992).
    /// </summary>
    public double EstimateCssSecPer100m(double distance1M, TimeSpan time1, double distance2M, TimeSpan time2)
    {
        // CSS = (D2 - D1) / (T2 - T1), expressed as speed; then converted to sec/100m pace.
        var distanceDiff = Math.Max(distance1M, distance2M) - Math.Min(distance1M, distance2M);
        var timeDiff = Math.Abs((time2 - time1).TotalSeconds);

        if (distanceDiff <= 0 || timeDiff <= 0)
            throw new ArgumentException("The two trials must differ in both distance and time.");

        var speedMetersPerSec = distanceDiff / timeDiff;
        return 100 / speedMetersPerSec;
    }
}
