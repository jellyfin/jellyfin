using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Server.ServerSetupApp;
using Jellyfin.Server.StartupUiPreview;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

// Bind to all interfaces by default so the preview is reachable from other machines on the LAN.
// Override with STARTUP_UI_PREVIEW_URL, e.g. http://10.40.0.10:5000.
var url = Environment.GetEnvironmentVariable("STARTUP_UI_PREVIEW_URL") ?? "http://0.0.0.0:5000";

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

// Morestachio's ByteCounterStream writes to the response synchronously, matching the live SetupServer.
builder.WebHost.ConfigureKestrel(options => options.AllowSynchronousIO = true);

var app = builder.Build();

var templatePath = Path.Combine(AppContext.BaseDirectory, "index.mstemplate.html");
var renderer = await StartupUiRenderer.CreateAsync(templatePath).ConfigureAwait(false);

var assemblyVersion = typeof(StartupUiRenderer).Assembly.GetName().Version;
var version = assemblyVersion is { Major: > 0 } ? assemblyVersion : new Version(12, 0, 0, 0);

// Cycle through sample activities on each refresh so every header state is easy to eyeball.
string[] sampleActivities =
[
    StartupActivity.CheckingStorage,
    StartupActivity.Initializing,
    StartupActivity.PreparingMigrations,
    StartupActivity.Migration(3, 12),
    StartupActivity.InitializingServices,
];
var activityIndex = 0;

app.MapGet("/", async (HttpContext context, bool? error, bool? remote, bool? network, bool? ready, string? activity) =>
{
    var isError = error ?? false;
    // ready=true returns HTTP 200, which the page treats as "server is up" -> success countdown.
    var isReady = ready ?? false;
    var localNetworkRequest = !(remote ?? false);
    var networkReady = network ?? true;
    var currentActivity = activity ?? sampleActivities[activityIndex++ % sampleActivities.Length];

    context.Response.StatusCode = isError
        ? StatusCodes.Status500InternalServerError
        : isReady
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;
    context.Response.ContentType = "text/html";

    var model = new Dictionary<string, object>
    {
        { "isInReportingMode", isError },
        { "currentActivity", currentActivity },
        { "retryValue", TimeSpan.FromSeconds(5) },
        { "version", version },
        { "logs", SampleData.BuildSampleLog(isError) },
        { "networkManagerReady", networkReady },
        { "localNetworkRequest", localNetworkRequest },
    };

    try
    {
        await renderer.RenderAsync(model, context.Response.Body).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        Console.WriteLine("RENDER ERROR: " + ex);
        await context.Response.WriteAsync("<pre>" + ex + "</pre>").ConfigureAwait(false);
    }
});

Console.WriteLine($"Startup UI preview listening on {url}");
Console.WriteLine("  normal:            /            (activity cycles each refresh)");
Console.WriteLine("  pin an activity:   /?activity=Loading%20plugins");
Console.WriteLine("  server ready:      /?ready=true   (shows the success countdown after ~5s)");
Console.WriteLine("  error mode:        /?error=true");
Console.WriteLine("  remote client:     /?remote=true");
Console.WriteLine("  network not ready: /?network=false");

await app.RunAsync(url).ConfigureAwait(false);
