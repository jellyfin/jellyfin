using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Jellyfin.Extensions;
using Jellyfin.Server.Migrations;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Controller.Net.WebSocketMessages.Outbound;
using MediaBrowser.Model.ApiClient;
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
            context.SchemaGenerator.GenerateSchema(typeof(IPlugin), context.SchemaRepository);

            var webSocketTypes = typeof(WebSocketMessage).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(WebSocketMessage))
                            && !t.IsGenericType
                            && t != typeof(WebSocketMessageInfo))
                .ToList();

            var inboundWebSocketSchemas = new List<OpenApiSchema>();
            var inboundWebSocketDiscriminators = new Dictionary<string, string>();
            foreach (var type in webSocketTypes.Where(t => typeof(IInboundWebSocketMessage).IsAssignableFrom(t)))
            {
                var messageType = (SessionMessageType?)type.GetProperty(nameof(WebSocketMessage.MessageType))?.GetCustomAttribute<DefaultValueAttribute>()?.Value;
                if (messageType is null)
                {
                    continue;
                }

                var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                inboundWebSocketSchemas.Add(schema);
                inboundWebSocketDiscriminators[messageType.ToString()!] = schema.Reference.ReferenceV3;
            }

            var inboundWebSocketMessageSchema = new OpenApiSchema
            {
                Type = "object",
                Description = "Represents the list of possible inbound websocket types",
                Reference = new OpenApiReference
                {
                    Id = nameof(InboundWebSocketMessage),
                    Type = ReferenceType.Schema
                },
                OneOf = inboundWebSocketSchemas,
                Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = nameof(WebSocketMessage.MessageType),
                    Mapping = inboundWebSocketDiscriminators
                }
            };

            context.SchemaRepository.AddDefinition(nameof(InboundWebSocketMessage), inboundWebSocketMessageSchema);

            var outboundWebSocketSchemas = new List<OpenApiSchema>();
            var outboundWebSocketDiscriminators = new Dictionary<string, string>();
            foreach (var type in webSocketTypes.Where(t => typeof(IOutboundWebSocketMessage).IsAssignableFrom(t)))
            {
                var messageType = (SessionMessageType?)type.GetProperty(nameof(WebSocketMessage.MessageType))?.GetCustomAttribute<DefaultValueAttribute>()?.Value;
                if (messageType is null)
                {
                    continue;
                }

                // Additional discriminator needed for GroupUpdate models...
                if (messageType == SessionMessageType.SyncPlayGroupUpdate && type != typeof(SyncPlayGroupUpdateCommandMessage))
                {
                    continue;
                }

                var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                outboundWebSocketSchemas.Add(schema);
                outboundWebSocketDiscriminators.Add(messageType.ToString()!, schema.Reference.ReferenceV3);
            }

            var outboundWebSocketMessageSchema = new OpenApiSchema
            {
                Type = "object",
                Description = "Represents the list of possible outbound websocket types",
                Reference = new OpenApiReference
                {
                    Id = nameof(OutboundWebSocketMessage),
                    Type = ReferenceType.Schema
                },
                OneOf = outboundWebSocketSchemas,
                Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = nameof(WebSocketMessage.MessageType),
                    Mapping = outboundWebSocketDiscriminators
                }
            };

            context.SchemaRepository.AddDefinition(nameof(OutboundWebSocketMessage), outboundWebSocketMessageSchema);
            context.SchemaRepository.AddDefinition(
                nameof(WebSocketMessage),
                new OpenApiSchema
                {
                    Type = "object",
                    Description = "Represents the possible websocket types",
                    Reference = new OpenApiReference
                    {
                        Id = nameof(WebSocketMessage),
                        Type = ReferenceType.Schema
                    },
                    OneOf = new[]
                    {
                        inboundWebSocketMessageSchema,
                        outboundWebSocketMessageSchema
                    }
                });

            // Manually generate sync play GroupUpdate messages.
            if (!context.SchemaRepository.Schemas.TryGetValue(nameof(GroupUpdate), out var groupUpdateSchema))
            {
                groupUpdateSchema = context.SchemaGenerator.GenerateSchema(typeof(GroupUpdate), context.SchemaRepository);
            }

            var groupUpdateOfGroupInfoSchema = context.SchemaGenerator.GenerateSchema(typeof(GroupUpdate<GroupInfoDto>), context.SchemaRepository);
            var groupUpdateOfGroupStateSchema = context.SchemaGenerator.GenerateSchema(typeof(GroupUpdate<GroupStateUpdate>), context.SchemaRepository);
            var groupUpdateOfStringSchema = context.SchemaGenerator.GenerateSchema(typeof(GroupUpdate<string>), context.SchemaRepository);
            var groupUpdateOfPlayQueueSchema = context.SchemaGenerator.GenerateSchema(typeof(GroupUpdate<PlayQueueUpdate>), context.SchemaRepository);

            groupUpdateSchema.OneOf = new List<OpenApiSchema>
            {
                groupUpdateOfGroupInfoSchema,
                groupUpdateOfGroupStateSchema,
                groupUpdateOfStringSchema,
                groupUpdateOfPlayQueueSchema
            };

            groupUpdateSchema.Discriminator = new OpenApiDiscriminator
            {
                PropertyName = nameof(GroupUpdate.Type),
                Mapping = new Dictionary<string, string>
                {
                    { GroupUpdateType.UserJoined.ToString(), groupUpdateOfStringSchema.Reference.ReferenceV3 },
                    { GroupUpdateType.UserLeft.ToString(), groupUpdateOfStringSchema.Reference.ReferenceV3 },
                    { GroupUpdateType.GroupJoined.ToString(), groupUpdateOfGroupInfoSchema.Reference.ReferenceV3 },
                    { GroupUpdateType.GroupLeft.ToString(), groupUpdateOfStringSchema.Reference.ReferenceV3 },
                    { GroupUpdateType.StateUpdate.ToString(), groupUpdateOfGroupStateSchema.Reference.ReferenceV3 },
                    { GroupUpdateType.PlayQueue.ToString(), groupUpdateOfPlayQueueSchema.Reference.ReferenceV3 },
                    { GroupUpdateType.NotInGroup.ToString(), groupUpdateOfStringSchema.Reference.ReferenceV3 },
                    { GroupUpdateType.GroupDoesNotExist.ToString(), groupUpdateOfStringSchema.Reference.ReferenceV3 },
                    { GroupUpdateType.LibraryAccessDenied.ToString(), groupUpdateOfStringSchema.Reference.ReferenceV3 }
                }
            };

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
