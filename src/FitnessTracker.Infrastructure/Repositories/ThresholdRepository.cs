using FitnessTracker.Core.Interfaces;
using FitnessTracker.Core.Models;
using FitnessTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessTracker.Infrastructure.Repositories;

public class ThresholdRepository : IThresholdRepository
{
    private readonly FitnessTrackerDbContext _db;

    public ThresholdRepository(FitnessTrackerDbContext db) => _db = db;

    public async Task<Threshold> AddAsync(Threshold threshold)
    {
        _db.Thresholds.Add(threshold);
        await _db.SaveChangesAsync();
        return threshold;
    }

    /// <summary>Gets the threshold that was in effect on a given date (the most recent one not after it).</summary>
    public async Task<Threshold?> GetLatestAsync(int athleteId, SportType sport, DateTime asOf)
    {
        return await _db.Thresholds
            .Where(t => t.AthleteId == athleteId && t.Sport == sport && t.EffectiveDate <= asOf)
            .OrderByDescending(t => t.EffectiveDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Threshold>> GetHistoryAsync(int athleteId, SportType sport)
    {
        return await _db.Thresholds
            .Where(t => t.AthleteId == athleteId && t.Sport == sport)
            .OrderBy(t => t.EffectiveDate)
            .ToListAsync();
    }
}
