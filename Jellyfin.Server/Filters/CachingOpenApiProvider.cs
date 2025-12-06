using System;
using AsyncKeyedLock;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters;

/// <summary>
/// OpenApi provider with caching.
/// </summary>
internal sealed class CachingOpenApiProvider : ISwaggerProvider
{
    private const string CacheKey = "openapi.json";

    private static readonly MemoryCacheEntryOptions _cacheOptions = new() { SlidingExpiration = TimeSpan.FromMinutes(5) };
    private static readonly AsyncNonKeyedLocker _lock = new(1);
    private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(1);

    private readonly IMemoryCache _memoryCache;
    private readonly SwaggerGenerator _swaggerGenerator;
    private readonly SwaggerGeneratorOptions _swaggerGeneratorOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingOpenApiProvider"/> class.
    /// </summary>
    /// <param name="optionsAccessor">The options accessor.</param>
    /// <param name="apiDescriptionsProvider">The api descriptions provider.</param>
    /// <param name="schemaGenerator">The schema generator.</param>
    /// <param name="memoryCache">The memory cache.</param>
    public CachingOpenApiProvider(
        IOptions<SwaggerGeneratorOptions> optionsAccessor,
        IApiDescriptionGroupCollectionProvider apiDescriptionsProvider,
        ISchemaGenerator schemaGenerator,
        IMemoryCache memoryCache)
    {
        _swaggerGeneratorOptions = optionsAccessor.Value;
        _swaggerGenerator = new SwaggerGenerator(_swaggerGeneratorOptions, apiDescriptionsProvider, schemaGenerator);
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public OpenApiDocument GetSwagger(string documentName, string? host = null, string? basePath = null)
    {
        if (_memoryCache.TryGetValue(CacheKey, out OpenApiDocument? openApiDocument) && openApiDocument is not null)
        {
            return AdjustDocument(openApiDocument, host, basePath);
        }

        using var acquired = _lock.LockOrNull(_lockTimeout);
        if (_memoryCache.TryGetValue(CacheKey, out openApiDocument) && openApiDocument is not null)
        {
            return AdjustDocument(openApiDocument, host, basePath);
        }

        if (acquired is null)
        {
            throw new InvalidOperationException("OpenApi document is generating");
        }

        openApiDocument = _swaggerGenerator.GetSwagger(documentName);
        _memoryCache.Set(CacheKey, openApiDocument, _cacheOptions);
        return AdjustDocument(openApiDocument, host, basePath);
    }

    private OpenApiDocument AdjustDocument(OpenApiDocument document, string? host, string? basePath)
    {
        document.Servers = _swaggerGeneratorOptions.Servers.Count != 0
            ? _swaggerGeneratorOptions.Servers
            : string.IsNullOrEmpty(host) && string.IsNullOrEmpty(basePath)
                ? []
                : [new OpenApiServer { Url = $"{host}{basePath}" }];

        return document;
    }
}
