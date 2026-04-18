using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Jellyfin.Extensions;
using Jellyfin.Server.Migrations;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters
{
    /// <summary>
    /// Add models not directly used by the API, but used for discovery and websockets.
    /// </summary>
    public class AdditionalModelFilter : IDocumentFilter
    {
        // Array of options that should not be visible in the api spec.
        private static readonly Type[] _ignoredConfigurations = [typeof(MigrationOptions), typeof(MediaBrowser.Model.Branding.BrandingOptions)];
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

            var inboundWebSocketSchemas = new List<IOpenApiSchema>();
            var inboundWebSocketDiscriminators = new Dictionary<string, OpenApiSchemaReference>();
            foreach (var type in webSocketTypes.Where(t => typeof(IInboundWebSocketMessage).IsAssignableFrom(t)))
            {
                var messageType = (SessionMessageType?)type.GetProperty(nameof(WebSocketMessage.MessageType))?.GetCustomAttribute<DefaultValueAttribute>()?.Value;
                if (messageType is null)
                {
                    continue;
                }

                var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                inboundWebSocketSchemas.Add(schema);
                if (schema is OpenApiSchemaReference schemaRef)
                {
                    inboundWebSocketDiscriminators[messageType.ToString()!] = schemaRef;
                }
            }

            var inboundWebSocketMessageSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "Represents the list of possible inbound websocket types",
                OneOf = inboundWebSocketSchemas,
                Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = nameof(WebSocketMessage.MessageType),
                    Mapping = inboundWebSocketDiscriminators
                }
            };

            context.SchemaRepository.AddDefinition(nameof(InboundWebSocketMessage), inboundWebSocketMessageSchema);

            var outboundWebSocketSchemas = new List<IOpenApiSchema>();
            var outboundWebSocketDiscriminators = new Dictionary<string, OpenApiSchemaReference>();
            foreach (var type in webSocketTypes.Where(t => typeof(IOutboundWebSocketMessage).IsAssignableFrom(t)))
            {
                var messageType = (SessionMessageType?)type.GetProperty(nameof(WebSocketMessage.MessageType))?.GetCustomAttribute<DefaultValueAttribute>()?.Value;
                if (messageType is null)
                {
                    continue;
                }

                var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                outboundWebSocketSchemas.Add(schema);
                if (schema is OpenApiSchemaReference schemaRef)
                {
                    outboundWebSocketDiscriminators.Add(messageType.ToString()!, schemaRef);
                }
            }

            // Add custom "SyncPlayGroupUpdateMessage" schema because Swashbuckle cannot generate it for us
            var syncPlayGroupUpdateMessageSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "Untyped sync play command.",
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    {
                        "Data", new OpenApiSchema
                        {
                            AllOf = new List<IOpenApiSchema>
                            {
                                new OpenApiSchemaReference(nameof(GroupUpdate<object>), null, null)
                            },
                            Description = "Group update data",
                        }
                    },
                    { "MessageId", new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid", Description = "Gets or sets the message id." } },
                    {
                        "MessageType", new OpenApiSchema
                        {
                            Enum = Enum.GetValues<SessionMessageType>().Select(type => (JsonNode)JsonValue.Create(type.ToString())!).ToList(),
                            AllOf = new List<IOpenApiSchema>
                            {
                                new OpenApiSchemaReference(nameof(SessionMessageType), null, null)
                            },
                            Description = "The different kinds of messages that are used in the WebSocket api.",
                            Default = JsonValue.Create(nameof(SessionMessageType.SyncPlayGroupUpdate)),
                            ReadOnly = true
                        }
                    },
                },
                AdditionalPropertiesAllowed = false,
            };
            context.SchemaRepository.AddDefinition("SyncPlayGroupUpdateMessage", syncPlayGroupUpdateMessageSchema);
            var syncPlayRef = new OpenApiSchemaReference("SyncPlayGroupUpdateMessage", null, null);
            outboundWebSocketSchemas.Add(syncPlayRef);
            outboundWebSocketDiscriminators[nameof(SessionMessageType.SyncPlayGroupUpdate)] = syncPlayRef;

            var outboundWebSocketMessageSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "Represents the list of possible outbound websocket types",
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
                    Type = JsonSchemaType.Object,
                    Description = "Represents the possible websocket types",
                    OneOf = new List<IOpenApiSchema>
                    {
                        new OpenApiSchemaReference(nameof(InboundWebSocketMessage), null, null),
                        new OpenApiSchemaReference(nameof(OutboundWebSocketMessage), null, null)
                    }
                });

            // Manually generate sync play GroupUpdate messages.
            var groupUpdateTypes = typeof(GroupUpdate<>).Assembly.GetTypes()
                .Where(t => t.BaseType is not null
                            && t.BaseType.IsGenericType
                            && t.BaseType.GetGenericTypeDefinition() == typeof(GroupUpdate<>))
                .ToList();

            var groupUpdateSchemas = new List<IOpenApiSchema>();
            var groupUpdateDiscriminators = new Dictionary<string, OpenApiSchemaReference>();
            foreach (var type in groupUpdateTypes)
            {
                var groupUpdateType = (GroupUpdateType?)type.GetProperty(nameof(GroupUpdate<object>.Type))?.GetCustomAttribute<DefaultValueAttribute>()?.Value;
                if (groupUpdateType is null)
                {
                    continue;
                }

                var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                groupUpdateSchemas.Add(schema);
                if (schema is OpenApiSchemaReference schemaRef)
                {
                    groupUpdateDiscriminators[groupUpdateType.ToString()!] = schemaRef;
                }
            }

            var groupUpdateSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "Represents the list of possible group update types",
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
