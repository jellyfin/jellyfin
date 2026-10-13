using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// Marks the string columns that are indexed but have no length bound.
/// </summary>
/// <remarks>
/// A string in a key or an index either has a maximum length, so that an index entry fits every database engine, or
/// is marked here. Each provider decides how to store and index a marked column, because the engines differ in the
/// text types they can index and in how long an index entry may be.
/// </remarks>
internal static class IndexedTextExtensions
{
    internal const string AnnotationName = "Jellyfin:UnboundedIndexedText";

    /// <summary>
    /// Marks the property as indexed text that is not bounded in length.
    /// </summary>
    /// <param name="builder">The property builder.</param>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <returns>The same builder instance.</returns>
    internal static PropertyBuilder<TProperty> IsUnboundedIndexedText<TProperty>(this PropertyBuilder<TProperty> builder)
    {
        return builder.HasAnnotation(AnnotationName, true);
    }

    /// <summary>
    /// Gets a value indicating whether the property is marked as indexed text that is not bounded in length.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns><c>true</c> if the property is marked; otherwise, <c>false</c>.</returns>
    internal static bool IsUnboundedIndexedText(this IReadOnlyProperty property)
    {
        return property.FindAnnotation(AnnotationName)?.Value is true;
    }
}
