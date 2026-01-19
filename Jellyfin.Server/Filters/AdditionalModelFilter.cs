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
        private static readonly Type[] _ignoredConfigurations = { typeof(MigrationOptions), typeof(MediaBrowser.Model.Branding.BrandingOptions) };
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

                var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                outboundWebSocketSchemas.Add(schema);
                outboundWebSocketDiscriminators.Add(messageType.ToString()!, schema.Reference.ReferenceV3);
            }

            // Add custom "SyncPlayGroupUpdateMessage" schema because Swashbuckle cannot generate it for us
            var syncPlayGroupUpdateMessageSchema = new OpenApiSchema
            {
                Type = "object",
                Description = "Untyped sync play command.",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    {
                        "Data", new OpenApiSchema
                        {
                            AllOf =
                            [
                                new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = nameof(GroupUpdate<object>) } }
                            ],
                            Description = "Group update data",
                            Nullable = false,
                        }
                    },
                    { "MessageId", new OpenApiSchema { Type = "string", Format = "uuid", Description = "Gets or sets the message id." } },
                    {
                        "MessageType", new OpenApiSchema
                        {
                            Enum = Enum.GetValues<SessionMessageType>().Select(type => new OpenApiString(type.ToString())).ToList<IOpenApiAny>(),
                            AllOf =
                            [
                                new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = nameof(SessionMessageType) } }
                            ],
                            Description = "The different kinds of messages that are used in the WebSocket api.",
                            Default = new OpenApiString(nameof(SessionMessageType.SyncPlayGroupUpdate)),
                            ReadOnly = true
                        }
                    },
                },
                AdditionalPropertiesAllowed = false,
                Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "SyncPlayGroupUpdateMessage" }
            };
            context.SchemaRepository.AddDefinition("SyncPlayGroupUpdateMessage", syncPlayGroupUpdateMessageSchema);
            outboundWebSocketSchemas.Add(syncPlayGroupUpdateMessageSchema);
            outboundWebSocketDiscriminators[nameof(SessionMessageType.SyncPlayGroupUpdate)] = syncPlayGroupUpdateMessageSchema.Reference.ReferenceV3;

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
            var groupUpdateTypes = typeof(GroupUpdate<>).Assembly.GetTypes()
                .Where(t => t.BaseType is not null
                            && t.BaseType.IsGenericType
                            && t.BaseType.GetGenericTypeDefinition() == typeof(GroupUpdate<>))
                .ToList();

            var groupUpdateSchemas = new List<OpenApiSchema>();
            var groupUpdateDiscriminators = new Dictionary<string, string>();
            foreach (var type in groupUpdateTypes)
            {
                var groupUpdateType = (GroupUpdateType?)type.GetProperty(nameof(GroupUpdate<object>.Type))?.GetCustomAttribute<DefaultValueAttribute>()?.Value;
                if (groupUpdateType is null)
                {
                    continue;
                }

                var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                groupUpdateSchemas.Add(schema);
                groupUpdateDiscriminators[groupUpdateType.ToString()!] = schema.Reference.ReferenceV3;
            }

            var groupUpdateSchema = new OpenApiSchema
            {
                Type = "object",
                Description = "Represents the list of possible group update types",
                Reference = new OpenApiReference
                {
                    Id = nameof(GroupUpdate<object>),
                    Type = ReferenceType.Schema
                },
                OneOf = groupUpdateSchemas,
                Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = nameof(GroupUpdate<object>.Type),
                    Mapping = groupUpdateDiscriminators
                }
            };

            context.SchemaRepository.Schemas[nameof(GroupUpdate<object>)] = groupUpdateSchema;

            context.SchemaGenerator.GenerateSchema(typeof(ServerDiscoveryInfo), context.SchemaRepository);

            foreach (var configuration in _serverConfigurationManager.GetConfigurationStores())
            {
                if (_ignoredConfigurations.IndexOf(configuration.ConfigurationType) != -1)
                {
                    continue;
                }

                context.SchemaGenerator.GenerateSchema(configuration.ConfigurationType, context.SchemaRepository);
            }
        }
    }
}
