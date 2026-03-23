namespace DotOcpi.Client.Sync;

/// <summary>
/// Resolves effective sync configuration for a CPO+module pair from the
/// hierarchical <see cref="PullSyncOptions"/>.
/// Resolution: CPO+module → CPO default → module-level → global default.
/// </summary>
internal static class PullSyncOptionsResolver
{
    internal static TimeSpan ResolveInterval(PullSyncOptions options, string cpoId, string moduleId)
    {
        if (
            options.CpoOverrides.TryGetValue(cpoId, out var cpoOpts)
            && cpoOpts.ModuleOverrides.TryGetValue(moduleId, out var cpoModOpts)
            && cpoModOpts.Interval.HasValue
        )
        {
            return cpoModOpts.Interval.Value;
        }

        if (cpoOpts?.DefaultInterval is { } cpoDefault)
        {
            return cpoDefault;
        }

        if (options.ModuleOverrides.TryGetValue(moduleId, out var modOpts) && modOpts.Interval.HasValue)
        {
            return modOpts.Interval.Value;
        }

        return options.DefaultInterval;
    }

    internal static IReadOnlyList<string> ResolveEnabledModules(PullSyncOptions options, string cpoId)
    {
        if (options.CpoOverrides.TryGetValue(cpoId, out var cpoOpts) && cpoOpts.EnabledModules is not null)
        {
            return cpoOpts.EnabledModules;
        }

        return options.EnabledModules;
    }
}
