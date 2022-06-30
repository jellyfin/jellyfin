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
    [ApiController]
    [Route("[controller]")]
    public class ModularHomeViewsController : ControllerBase
    {
        private readonly ILogger<ModularHomeViewsController> _logger;
        private readonly IHomeScreenManager _homeScreenManager;

        public ModularHomeViewsController(ILogger<ModularHomeViewsController> logger, IHomeScreenManager homeScreenManager)
        {
            _logger = logger;
            _homeScreenManager = homeScreenManager;
        }

        [HttpGet("{viewName}")]
        public ActionResult GetView([FromRoute] string viewName)
        {
            return ServeView(viewName);
        }

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
                    Limit = section.Limit
                };

                items.Add(item);
            }

            return new QueryResult<HomeScreenSectionInfo>(null, items.Count, items);
        }

        // TODO: Add support for saving the section types being enabled/disabled.

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

            var view = pages.FirstOrDefault(pageInfo => pageInfo.Name == viewName, null);

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
