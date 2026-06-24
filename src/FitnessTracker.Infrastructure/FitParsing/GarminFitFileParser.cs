using Dynastream.Fit;
using FitnessTracker.Core.Interfaces;
using FitnessTracker.Core.Models;
using Activity = FitnessTracker.Core.Models.Activity;
using DateTime = System.DateTime;

namespace FitnessTracker.Infrastructure.FitParsing;

/// <summary>
/// Parses .FIT files using Garmin's official FIT C# SDK (NuGet: Garmin.FIT.Sdk).
///
/// A FIT activity file is a stream of typed "messages" (Session, Record, Lap, etc).
/// We listen for SessionMesg (one per activity - has the summary stats we want)
/// and the first RecordMesg (to get a GPS start point for weather lookups later).
///
/// NOTE: The FIT SDK's exact event/property names can shift slightly between SDK
/// versions. If this doesn't compile cleanly against the NuGet version you pull down,
/// open the SDK's own Examples/Decode project (ships in the NuGet package source) and
/// cross-check SessionMesg's property names - the decode pattern below (Decode +
/// MesgBroadcaster + event subscriptions) has been stable for years, but Garmin does
/// occasionally rename specific getters between SDK releases.
/// </summary>
public class GarminFitFileParser : IFitFileParser
{
    public List<Activity> Parse(byte[] fitFileBytes, string originalFileName)
    {
        using var stream = new MemoryStream(fitFileBytes);

        var decoder = new Decode();
        var broadcaster = new MesgBroadcaster();
        decoder.MesgEvent += broadcaster.OnMesg;

        SessionMesg? sessionMesg = null;
        RecordMesg? firstRecordMesg = null;




        broadcaster.SessionMesgEvent += (_, e) => sessionMesg = (SessionMesg)e.mesg;
        broadcaster.RecordMesgEvent += (_, e) => firstRecordMesg ??= (RecordMesg)e.mesg;

        if (!decoder.IsFIT(stream))
            throw new InvalidDataException($"'{originalFileName}' is not a valid FIT file.");

        stream.Position = 0;
        if (!decoder.CheckIntegrity(stream))
            throw new InvalidDataException($"'{originalFileName}' failed FIT integrity check (file may be corrupt or truncated).");

        stream.Position = 0;
        decoder.Read(stream);

        if (sessionMesg is null)
            throw new InvalidDataException($"'{originalFileName}' contains no Session message - cannot extract activity summary.");

        var activity = MapSessionToActivity(sessionMesg, firstRecordMesg, originalFileName);
        return new List<Activity> { activity };
    }

    private static Activity MapSessionToActivity(SessionMesg session, RecordMesg? firstRecord, string fileName)
    {
        var sport = MapSport(session.GetSport());

        var startTime = session.GetStartTime()?.GetDateTime() ?? DateTime.UtcNow;
        var totalElapsedSec = session.GetTotalElapsedTime() ?? 0;
        var totalMovingSec = session.GetTotalTimerTime() ?? totalElapsedSec;
        var distanceMeters = session.GetTotalDistance() ?? 0;

        var activity = new Activity
        {
            Sport = sport,
            Source = ActivitySource.FitFileImport,
            StartTimeUtc = startTime,
            ElapsedTime = TimeSpan.FromSeconds(totalElapsedSec),
            MovingTime = TimeSpan.FromSeconds(totalMovingSec),
            DistanceMeters = distanceMeters,
            AvgHeartRate = session.GetAvgHeartRate(),
            MaxHeartRate = session.GetMaxHeartRate(),
            TotalElevationGainMeters = session.GetTotalAscent(),
            SourceFileName = fileName
        };

        // Sport-specific derived metrics.
        switch (sport)
        {
            case SportType.Run:
            case SportType.Swim:
                if (distanceMeters > 0 && totalMovingSec > 0)
                    activity.AvgPaceSecPerKm = totalMovingSec / (distanceMeters / 1000.0);
                break;

            case SportType.Bike:
                if (distanceMeters > 0 && totalMovingSec > 0)
                    activity.AvgSpeedKph = (distanceMeters / 1000.0) / (totalMovingSec / 3600.0);
                activity.AvgPowerWatts = session.GetAvgPower();
                activity.NormalizedPowerWatts = session.GetNormalizedPower();
                break;
        }

        // GPS start point, if present, for later weather-history lookups.
        if (firstRecord is not null)
        {
            var lat = firstRecord.GetPositionLat();
            var lon = firstRecord.GetPositionLong();
            if (lat.HasValue && lon.HasValue)
            {
                // FIT stores coordinates as "semicircles" - Garmin's int32 encoding for lat/lon.
                activity.StartLatitude = lat.Value * (180.0 / Math.Pow(2, 31));
                activity.StartLongitude = lon.Value * (180.0 / Math.Pow(2, 31));
            }
        }

        return activity;
    }

    private static SportType MapSport(Sport? fitSport) => fitSport switch
    {
        Sport.Running => SportType.Run,
        Sport.Cycling => SportType.Bike,
        Sport.Swimming => SportType.Swim,
        _ => SportType.Other
    };
}
