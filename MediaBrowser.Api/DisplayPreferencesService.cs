using System.Threading;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class UpdateDisplayPreferences
    /// </summary>
    [Route("/DisplayPreferences/{DisplayPreferencesId}", "POST", Summary = "Updates a user's display preferences for an item")]
    public class UpdateDisplayPreferences : DisplayPreferences, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "DisplayPreferencesId", Description = "DisplayPreferences Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string DisplayPreferencesId { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string UserId { get; set; }
    }

    [Route("/DisplayPreferences/{Id}", "GET", Summary = "Gets a user's display preferences for an item")]
    public class GetDisplayPreferences : IReturn<DisplayPreferences>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "Client", Description = "Client", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Client { get; set; }
    }

    /// <summary>
    /// Class DisplayPreferencesService
    /// </summary>
    [Authenticated]
    public class DisplayPreferencesService : BaseApiService
    {
        /// <summary>
        /// The _display preferences manager
        /// </summary>
        private readonly IDisplayPreferencesRepository _displayPreferencesManager;
        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesService" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="displayPreferencesManager">The display preferences manager.</param>
        public DisplayPreferencesService(
            ILogger<DisplayPreferencesService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IJsonSerializer jsonSerializer,
            IDisplayPreferencesRepository displayPreferencesManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _jsonSerializer = jsonSerializer;
            _displayPreferencesManager = displayPreferencesManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Get(GetDisplayPreferences request)
        {
            var result = _displayPreferencesManager.GetDisplayPreferences(request.Id, request.UserId, request.Client);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateDisplayPreferences request)
        {
            // Serialize to json and then back so that the core doesn't see the request dto type
            var displayPreferences = _jsonSerializer.DeserializeFromString<DisplayPreferences>(_jsonSerializer.SerializeToString(request));

            _displayPreferencesManager.SaveDisplayPreferences(displayPreferences, request.UserId, request.Client, CancellationToken.None);
        }
    }
}
