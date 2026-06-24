using FitnessTracker.Core.Models;

namespace FitnessTracker.Core.Interfaces;

public interface IActivityRepository
{
    Task<Activity> AddAsync(Activity activity);
    Task<bool> ExistsAsync(int athleteId, DateTime startTimeUtc, SportType sport);
    Task<List<Activity>> GetByAthleteAsync(int athleteId, SportType? sport = null, DateTime? from = null, DateTime? to = null);
    Task<Activity?> GetByIdAsync(int id);
}

public interface IThresholdRepository
{
    Task<Threshold> AddAsync(Threshold threshold);
    Task<Threshold?> GetLatestAsync(int athleteId, SportType sport, DateTime asOf);
    Task<List<Threshold>> GetHistoryAsync(int athleteId, SportType sport);
}

public interface IFitFileParser
{
    /// <summary>Parses a raw .FIT file's bytes into one or more Activity records (a FIT file is normally one session).</summary>
    List<Activity> Parse(byte[] fitFileBytes, string originalFileName);
}
