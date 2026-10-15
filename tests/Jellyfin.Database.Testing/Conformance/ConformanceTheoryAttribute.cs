using System;
using System.Collections.Generic;
using Xunit;
using Xunit.v3;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// A row of the conformance kit that takes data. It runs on the engines of <see cref="ConformanceKit.Engines"/> and is
/// skipped on the others.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConformanceTheoryAttribute : TheoryAttribute, ITraitAttribute
{
    public ConformanceTheoryAttribute()
    {
        Skip = ProviderRequirement.SkipReason(ConformanceKit.Engines);
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new("Category", ConformanceKit.Category), .. ProviderRequirement.Traits(ConformanceKit.Engines)];
}
