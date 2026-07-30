#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixSchemaInitializer : BackgroundService
{
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromMinutes(5);

    private readonly ICustomNetflixRepository _repository;
    private readonly CustomNetflixSchemaState _schemaState;
    private readonly ILogger<CustomNetflixSchemaInitializer> _logger;

    public CustomNetflixSchemaInitializer(
        ICustomNetflixRepository repository,
        CustomNetflixSchemaState schemaState,
        ILogger<CustomNetflixSchemaInitializer> logger)
    {
        _repository = repository;
        _schemaState = schemaState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_repository.IsEnabled)
        {
            _logger.LogInformation("CustomNetflix PostgreSQL is not configured; custom endpoints will return configuration errors until it is enabled.");
            return;
        }

        var failureCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _repository.EnsureSchemaAsync(stoppingToken).ConfigureAwait(false);
                _schemaState.MarkReady();
                _logger.LogInformation("CustomNetflix PostgreSQL schema is ready.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                var retryDelay = CustomNetflixRetryPolicy.GetDelay(++failureCount, MaximumRetryDelay);
                _logger.LogWarning(
                    ex,
                    "CustomNetflix PostgreSQL schema is unavailable; retrying in {RetryDelay}.",
                    retryDelay);
                await Task.Delay(retryDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
