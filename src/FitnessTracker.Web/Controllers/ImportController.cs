using FitnessTracker.Core.Interfaces;
using FitnessTracker.Core.Models;
using FitnessTracker.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitnessTracker.Web.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{
    private readonly IFitFileParser _parser;
    private readonly IActivityRepository _activityRepo;
    private readonly IThresholdRepository _thresholdRepo;
    private readonly TrainingLoadCalculator _loadCalculator;

    public ImportController(
        IFitFileParser parser,
        IActivityRepository activityRepo,
        IThresholdRepository thresholdRepo,
        TrainingLoadCalculator loadCalculator)
    {
        _parser = parser;
        _activityRepo = activityRepo;
        _thresholdRepo = thresholdRepo;
        _loadCalculator = loadCalculator;
    }

    /// <summary>
    /// Upload one or more .FIT files exported from Garmin Connect.
    /// Duplicate activities (same athlete/sport/start-time) are skipped, not re-imported.
    /// </summary>
    [HttpPost("fit-files")]
    [RequestSizeLimit(50_000_000)] // 50MB - generous for a batch of FIT files, which are typically a few hundred KB each
    public async Task<IActionResult> UploadFitFiles([FromForm] int athleteId, [FromForm] List<IFormFile> files)
    {
        if (files is null || files.Count == 0)
            return BadRequest("No files were uploaded.");

        var imported = new List<object>();
        var skipped = new List<string>();
        var failed = new List<object>();

        foreach (var file in files)
        {
            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var activities = _parser.Parse(ms.ToArray(), file.FileName);

                foreach (var activity in activities)
                {
                    activity.AthleteId = athleteId;

                    if (await _activityRepo.ExistsAsync(athleteId, activity.StartTimeUtc, activity.Sport))
                    {
                        skipped.Add(file.FileName);
                        continue;
                    }

                    // Compute training load at import time using whatever threshold was
                    // current on the activity's date, so historical activities are scored
                    // against the threshold that applied then, not today's threshold.
                    var threshold = await _thresholdRepo.GetLatestAsync(athleteId, activity.Sport, activity.StartTimeUtc);
                    activity.TrainingLoad = _loadCalculator.CalculateLoad(activity, threshold);

                    var saved = await _activityRepo.AddAsync(activity);
                    imported.Add(new { saved.Id, saved.Sport, saved.StartTimeUtc, saved.DistanceMeters });
                }
            }
            catch (Exception ex)
            {
                failed.Add(new { file.FileName, error = ex.Message });
            }
        }

        return Ok(new { importedCount = imported.Count, imported, skippedCount = skipped.Count, skipped, failedCount = failed.Count, failed });
    }
}
