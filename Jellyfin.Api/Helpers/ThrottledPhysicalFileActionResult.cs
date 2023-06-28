using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers;

/// <summary>
///     Acts like a <see cref="PhysicalFileResult"/> but limits the transfer speed of the requested file.
/// </summary>
public class ThrottledPhysicalFileActionResult : PhysicalFileResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottledPhysicalFileActionResult"/> class.
    /// </summary>
    /// <param name="fileName">The Filename.</param>
    /// <param name="contentType">The Content Type.</param>
    public ThrottledPhysicalFileActionResult(string fileName, string contentType) : base(fileName, contentType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottledPhysicalFileActionResult"/> class.
    /// </summary>
    /// <param name="fileName">The Filename.</param>
    /// <param name="contentType">The Content Type.</param>
    public ThrottledPhysicalFileActionResult(string fileName, MediaTypeHeaderValue contentType) : base(fileName, contentType)
    {
    }

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ThrottledPhysicalFileActionResult>>();
        return executor.ExecuteAsync(context, this);
    }
}
