using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.ModularHome.Api
{
    /// <summary>
    /// API controller for Modular Home plugin.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ModularHomeViewsController : ControllerBase
    {
        private readonly ILogger<ModularHomeViewsController> _logger;
        private readonly IHomeScreenManager _homeScreenManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger"/> interface.</param>
        /// <param name="homeScreenManager">Instance of <see cref="IHomeScreenManager"/> interface.</param>
        public ModularHomeViewsController(ILogger<ModularHomeViewsController> logger, IHomeScreenManager homeScreenManager)
        {
            _logger = logger;
            _homeScreenManager = homeScreenManager;
        }

        /// <summary>
        /// Get the view for the plugin.
        /// </summary>
        /// <param name="viewName">The view identifier.</param>
        /// <returns>View.</returns>
        [HttpGet("{viewName}")]
        public ActionResult GetView([FromRoute] string viewName)
        {
            return ServeView(viewName);
        }

        /// <summary>
        /// Get the section types that are registered in Modular Home.
        /// </summary>
        /// <returns>Array of <see cref="HomeScreenSectionInfo"/>.</returns>
        [HttpGet("Sections")]
        public QueryResult<HomeScreenSectionInfo> GetSectionTypes()
        {
            // Todo add reading whether the section is enabled or disabled by the user.
            List<HomeScreenSectionInfo> items = new List<HomeScreenSectionInfo>();

            IEnumerable<IHomeScreenSection> sections = _homeScreenManager.GetSectionTypes();

            foreach (IHomeScreenSection section in sections)
            {
                HomeScreenSectionInfo item = new HomeScreenSectionInfo
                {
                    Section = section.Section,
                    DisplayText = section.DisplayText,
                    AdditionalData = section.AdditionalData,
                    Route = section.Route,
                    Limit = section.Limit ?? 1
                };

                items.Add(item);
            }

            return new QueryResult<HomeScreenSectionInfo>(null, items.Count, items);
        }

        /// <summary>
        /// Get the user settings for Modular Home.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns><see cref="ModularHomeUserSettings"/>.</returns>
        [HttpGet("UserSettings")]
        public ActionResult<ModularHomeUserSettings> GetUserSettings([FromQuery] Guid userId)
        {
            return _homeScreenManager.GetUserSettings(userId) ?? new ModularHomeUserSettings
            {
                UserId = userId
            };
        }

        /// <summary>
        /// Update the user settings for Modular Home.
        /// </summary>
        /// <param name="obj">Instance of <see cref="ModularHomeUserSettings" />.</param>
        /// <returns>Status.</returns>
        [HttpPost("UserSettings")]
        public ActionResult UpdateSettings([FromBody] ModularHomeUserSettings obj)
        {
            _homeScreenManager.UpdateUserSettings(obj.UserId, obj);

            return Ok();
        }

        private ActionResult ServeView(string viewName)
        {
            if (Plugin.Instance == null)
            {
                return BadRequest("No plugin instance found");
            }

            IEnumerable<PluginPageInfo> pages = Plugin.Instance.GetViews();

            if (pages == null)
            {
                return NotFound("Pages is null or empty");
            }

            var view = pages.FirstOrDefault(pageInfo => pageInfo?.Name == viewName, null);

            if (view == null)
            {
                return NotFound("No matching view found");
            }

            Stream? stream = Plugin.Instance.GetType().Assembly.GetManifestResourceStream(view.EmbeddedResourcePath);

            if (stream == null)
            {
                _logger.LogError("Failed to get resource {Resource}", view.EmbeddedResourcePath);
                return NotFound();
            }

            return File(stream, MimeTypes.GetMimeType(view.EmbeddedResourcePath));
        }
    }
}
