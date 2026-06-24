using FitnessTracker.Core.Interfaces;
using FitnessTracker.Core.Services;
using FitnessTracker.Infrastructure.Data;
using FitnessTracker.Infrastructure.FitParsing;
using FitnessTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──
builder.Services.AddDbContext<FitnessTrackerDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repositories ──
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IThresholdRepository, ThresholdRepository>();

// ── Domain services ──
builder.Services.AddScoped<IFitFileParser, GarminFitFileParser>();
builder.Services.AddScoped<TrainingLoadCalculator>();
builder.Services.AddScoped<ThresholdEstimator>();

// ── API / Swagger ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Dashboard frontend (Razor Pages) ──
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // serves /wwwroot - Chart.js, css, etc.
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.MapGet("/", () => Results.Redirect("/Dashboard"));

app.Run();
