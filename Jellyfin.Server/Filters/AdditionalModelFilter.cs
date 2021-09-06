using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters
{
    /// <summary>
    /// Add models not directly used by the API, but used for discovery and websockets.
    /// </summary>
    public class AdditionalModelFilter : IDocumentFilter
    {
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
        }
    }
}
