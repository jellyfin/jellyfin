using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class UpdateDisplayPreferences
    /// </summary>
    [Route("/DisplayPreferences/{DisplayPreferencesId}", "POST")]
    [Api(("Updates a user's display preferences for an item"))]
    public class UpdateDisplayPreferences : DisplayPreferences, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "DisplayPreferencesId", Description = "DisplayPreferences Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid DisplayPreferencesId { get; set; }
    }

    [Route("/DisplayPreferences/{Id}", "GET")]
    [Api(("Gets a user's display preferences for an item"))]
    public class GetDisplayPreferences : IReturn<DisplayPreferences>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }
    }
    
    /// <summary>
    /// Class DisplayPreferencesService
    /// </summary>
    public class DisplayPreferencesService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;
        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        public DisplayPreferencesService(IUserManager userManager, IJsonSerializer jsonSerializer)
        {
            _userManager = userManager;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Get(GetDisplayPreferences request)
        {
            var task = _userManager.GetDisplayPreferences(request.Id);

            return ToOptimizedResult(task.Result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateDisplayPreferences request)
        {
            // We need to parse this manually because we told service stack not to with IRequiresRequestStream
            // https://code.google.com/p/servicestack/source/browse/trunk/Common/ServiceStack.Text/ServiceStack.Text/Controller/PathInfo.cs
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var displayPreferencesId = new Guid(pathInfo.GetArgumentValue<string>(1));

            // Serialize to json and then back so that the core doesn't see the request dto type
            var displayPreferences = _jsonSerializer.DeserializeFromString<DisplayPreferences>(_jsonSerializer.SerializeToString(request));

            var task = _userManager.SaveDisplayPreferences(displayPreferences, CancellationToken.None);

            Task.WaitAll(task);
        }
    }
}
