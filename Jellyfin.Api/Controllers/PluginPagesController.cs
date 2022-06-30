using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class PluginPagesController : BaseJellyfinApiController
    {
        private readonly IPluginPagesManager _pluginPagesManager;

        public PluginPagesController(IPluginPagesManager pluginPagesManager) : base()
        {
            _pluginPagesManager = pluginPagesManager;
        }

        [HttpGet("User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<PluginPage>> GetHomeScreenSections()
        {
            List<PluginPage> pages = _pluginPagesManager.GetPages().ToList();

            return new QueryResult<PluginPage>(
                0,
                pages.Count,
                pages);
        }

    }
}
