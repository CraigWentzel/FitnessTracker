using FitnessTracker.Core.Interfaces;
using FitnessTracker.Core.Models;
using FitnessTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FitnessTracker.Infrastructure.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly FitnessTrackerDbContext _db;

    public ActivityRepository(FitnessTrackerDbContext db) => _db = db;

    public async Task<Activity> AddAsync(Activity activity)
    {
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    /// <summary>
    /// Prevents duplicate imports if the same FIT file (or an overlapping export)
    /// gets uploaded twice - matches on athlete + start time + sport.
    /// </summary>
    public async Task<bool> ExistsAsync(int athleteId, DateTime startTimeUtc, SportType sport)
    {
        return await _db.Activities.AnyAsync(a =>
            a.AthleteId == athleteId &&
            a.Sport == sport &&
            a.StartTimeUtc == startTimeUtc);
    }

    public async Task<List<Activity>> GetByAthleteAsync(int athleteId, SportType? sport = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Activities.Where(a => a.AthleteId == athleteId);

        if (sport.HasValue) query = query.Where(a => a.Sport == sport.Value);
        if (from.HasValue) query = query.Where(a => a.StartTimeUtc >= from.Value);
        if (to.HasValue) query = query.Where(a => a.StartTimeUtc <= to.Value);

        return await query.OrderByDescending(a => a.StartTimeUtc).ToListAsync();
    }

    public async Task<Activity?> GetByIdAsync(int id) => await _db.Activities.FindAsync(id);
}
