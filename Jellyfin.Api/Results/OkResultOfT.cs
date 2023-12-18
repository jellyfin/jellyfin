#pragma warning disable SA1649 // File name should match type name.

using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Results;

/// <summary>
/// Ok result with type specified.
/// </summary>
/// <typeparam name="T">The type to return.</typeparam>
public class OkResult<T> : OkObjectResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OkResult{T}"/> class.
    /// </summary>
    /// <param name="value">The value to return.</param>
    public OkResult(T value)
        : base(value)
    {
    }
}
