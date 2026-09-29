using Kmila.Shared.Data;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Stores and lists user-authored samples — exported projects the user has
/// promoted to reusable gallery entries via "import as sample". The built-in
/// examples remain baked into the app; these live in the database so they can
/// be added and removed at runtime. Creating a project from a sample simply
/// re-imports its stored bundle through <see cref="ProjectBundleService"/>.
/// </summary>
public class UserSampleService
{
    private readonly ApplicationDbContext _db;
    private readonly ProjectBundleService _bundles;
    private readonly ILogger _logger;

    /// <summary>Fires after a sample is added or removed so gallery UI can refresh.</summary>
    public event Action? OnChanged;

    /// <summary>Creates the user sample service.</summary>
    /// <param name="db">Application database used to persist user samples.</param>
    /// <param name="bundles">Service used to import/export project bundles.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public UserSampleService(
        ApplicationDbContext db,
        ProjectBundleService bundles,
        ILogger<UserSampleService>? logger = null)
    {
        _db = db;
        _bundles = bundles;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Lists saved user samples, newest first.</summary>
    public async Task<List<UserSample>> ListAsync()
    {
        try
        {
            await _db.InitDBAsync();
            return await _db.UserSamples.OrderByDescending(s => s.CreatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserSamples] List failed");
            return new List<UserSample>();
        }
    }

    /// <summary>
    /// Saves a new user sample from a project bundle JSON plus display metadata.
    /// Returns the created row, or null on failure.
    /// </summary>
    public async Task<UserSample?> AddAsync(string title, string icon, string description, string bundleJson)
    {
        if (string.IsNullOrWhiteSpace(bundleJson)) return null;
        try
        {
            await _db.InitDBAsync();
            var sample = new UserSample
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Untitled sample" : title.Trim(),
                Icon = icon?.Trim() ?? string.Empty,
                Description = description?.Trim() ?? string.Empty,
                BundleJson = bundleJson,
                CreatedAt = DateTime.UtcNow
            };
            await _db.InsertAsync(sample);
            OnChanged?.Invoke();
            return sample;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserSamples] Add failed");
            return null;
        }
    }

    /// <summary>Deletes a user sample by id.</summary>
    public async Task DeleteAsync(int id)
    {
        try
        {
            await _db.InitDBAsync();
            var row = await _db.UserSamples.Where(s => s.Id == id).FirstOrDefaultAsync();
            if (row == null) return;
            await _db.DeleteAsync(row);
            OnChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserSamples] Delete failed");
        }
    }

    /// <summary>
    /// Re-imports a stored sample as a fresh project under <paramref name="basePath"/>.
    /// Delegates to <see cref="ProjectBundleService.ImportAsync"/> (non-destructive,
    /// name-collision safe). Returns the new project or null.
    /// </summary>
    public async Task<Project?> CreateProjectFromSampleAsync(int id, string basePath)
    {
        try
        {
            await _db.InitDBAsync();
            var row = await _db.UserSamples.Where(s => s.Id == id).FirstOrDefaultAsync();
            if (row == null || string.IsNullOrWhiteSpace(row.BundleJson)) return null;
            return await _bundles.ImportAsync(row.BundleJson, basePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserSamples] Create-from-sample failed");
            return null;
        }
    }
}
