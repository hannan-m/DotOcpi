using Microsoft.Extensions.Options;

namespace DotOcpi.Client.Sync;

internal sealed class PullSyncOptionsValidator : IValidateOptions<PullSyncOptions>
{
    private static readonly HashSet<string> ValidModules = new(StringComparer.Ordinal)
    {
        "locations",
        "sessions",
        "cdrs",
        "tariffs",
    };

    public ValidateOptionsResult Validate(string? name, PullSyncOptions options)
    {
        var errors = new List<string>();

        if (options.DefaultInterval <= TimeSpan.Zero)
            errors.Add("DefaultInterval must be positive.");

        if (options.MaxJitter < TimeSpan.Zero)
            errors.Add("MaxJitter must not be negative.");

        ValidateModuleList(options.EnabledModules, "EnabledModules", errors);

        foreach (var (moduleId, moduleOpts) in options.ModuleOverrides)
        {
            if (!ValidModules.Contains(moduleId))
                errors.Add($"ModuleOverrides key '{moduleId}' is not a valid pullable module.");

            if (moduleOpts.Interval.HasValue && moduleOpts.Interval.Value <= TimeSpan.Zero)
                errors.Add($"ModuleOverrides['{moduleId}'].Interval must be positive.");
        }

        foreach (var (cpoId, cpoOpts) in options.CpoOverrides)
        {
            if (cpoOpts.DefaultInterval.HasValue && cpoOpts.DefaultInterval.Value <= TimeSpan.Zero)
                errors.Add($"CpoOverrides['{cpoId}'].DefaultInterval must be positive.");

            if (cpoOpts.EnabledModules is not null)
                ValidateModuleList(cpoOpts.EnabledModules, $"CpoOverrides['{cpoId}'].EnabledModules", errors);

            foreach (var (moduleId, moduleOpts) in cpoOpts.ModuleOverrides)
            {
                if (!ValidModules.Contains(moduleId))
                    errors.Add($"CpoOverrides['{cpoId}'].ModuleOverrides key '{moduleId}' is not a valid pullable module.");

                if (moduleOpts.Interval.HasValue && moduleOpts.Interval.Value <= TimeSpan.Zero)
                    errors.Add($"CpoOverrides['{cpoId}'].ModuleOverrides['{moduleId}'].Interval must be positive.");
            }
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }

    private static void ValidateModuleList(IReadOnlyList<string> modules, string path, List<string> errors)
    {
        foreach (var moduleId in modules)
        {
            if (!ValidModules.Contains(moduleId))
                errors.Add($"{path} contains invalid module '{moduleId}'. Valid: locations, sessions, cdrs, tariffs.");
        }
    }
}
