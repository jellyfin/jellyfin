using System;
using System.Linq;
using Jellyfin.Extensions;
using Jellyfin.Server.Migrations;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Session;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters
{
    /// <summary>
    /// Add models not directly used by the API, but used for discovery and websockets.
    /// </summary>
    public class AdditionalModelFilter : IDocumentFilter
    {
        // Array of options that should not be visible in the api spec.
        private static readonly Type[] _ignoredConfigurations = { typeof(MigrationOptions) };
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdditionalModelFilter"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public AdditionalModelFilter(IServerConfigurationManager serverConfigurationManager)
        {
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <inheritdoc />
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            context.SchemaGenerator.GenerateSchema(typeof(IPlugin), context.SchemaRepository);

            var websocketTypes = typeof(WebSocketMessage).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(WebSocketMessage))
                            && !t.IsGenericType
                            && t != typeof(WebSocketMessageInfo))
                .ToList();

            context.SchemaRepository.AddDefinition(
                nameof(InboundWebSocketMessage),
                new OpenApiSchema
                {
                    Type = "object",
                    Description = "Represents the list of possible inbound websocket types",
                    AllOf = websocketTypes
                        .Where(t => typeof(IInboundWebSocketMessage).IsAssignableFrom(t))
                        .Select(t => context.SchemaGenerator.GenerateSchema(t, context.SchemaRepository))
                        .ToList()
                });

            context.SchemaRepository.AddDefinition(
                nameof(OutboundWebSocketMessage),
                new OpenApiSchema
                {
                    Type = "object",
                    Description = "Represents the list of possible outbound websocket types",
                    OneOf = websocketTypes
                        .Where(t => typeof(IOutboundWebSocketMessage).IsAssignableFrom(t))
                        .Select(t => context.SchemaGenerator.GenerateSchema(t, context.SchemaRepository))
                        .ToList()
                });

            context.SchemaGenerator.GenerateSchema(typeof(ServerDiscoveryInfo), context.SchemaRepository);

            foreach (var configuration in _serverConfigurationManager.GetConfigurationStores())
            {
                if (_ignoredConfigurations.IndexOf(configuration.ConfigurationType) != -1)
                {
                    continue;
                }

                context.SchemaGenerator.GenerateSchema(configuration.ConfigurationType, context.SchemaRepository);
            }

            context.SchemaRepository.AddDefinition(nameof(TranscodeReason), new OpenApiSchema
            {
                Type = "string",
                Enum = Enum.GetNames<TranscodeReason>()
                    .Select(e => new OpenApiString(e))
                    .Cast<IOpenApiAny>()
                    .ToArray()
            });
        }
    }
}
