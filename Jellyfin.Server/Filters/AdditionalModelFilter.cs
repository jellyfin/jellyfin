using System;
using System.Linq;
using Jellyfin.Extensions;
using Jellyfin.Server.Migrations;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;
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
            context.SchemaGenerator.GenerateSchema(typeof(LibraryUpdateInfo), context.SchemaRepository);
            context.SchemaGenerator.GenerateSchema(typeof(IPlugin), context.SchemaRepository);
            context.SchemaGenerator.GenerateSchema(typeof(PlayRequest), context.SchemaRepository);
            context.SchemaGenerator.GenerateSchema(typeof(PlaystateRequest), context.SchemaRepository);
            context.SchemaGenerator.GenerateSchema(typeof(TimerEventInfo), context.SchemaRepository);
            context.SchemaGenerator.GenerateSchema(typeof(SendCommand), context.SchemaRepository);
            context.SchemaGenerator.GenerateSchema(typeof(GeneralCommandType), context.SchemaRepository);

            context.SchemaGenerator.GenerateSchema(typeof(GroupUpdate<object>), context.SchemaRepository);

            context.SchemaGenerator.GenerateSchema(typeof(SessionMessageType), context.SchemaRepository);
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
