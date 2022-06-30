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
    /// <summary>
    /// Plugin Pages API controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class PluginPagesController : BaseJellyfinApiController
    {
        private readonly IPluginPagesManager _pluginPagesManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pluginPagesManager">Instance of <see href="IPluginPagesManager" /> interface.</param>
        public PluginPagesController(IPluginPagesManager pluginPagesManager) : base()
        {
            _pluginPagesManager = pluginPagesManager;
        }

        /// <summary>
        /// Get pages this plugin serves for users
        /// </summary>
        /// <returns></returns>
        [HttpGet("User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<PluginPage>> GetPluginPages()
        {
            List<PluginPage> pages = _pluginPagesManager.GetPages().ToList();

            return new QueryResult<PluginPage>(
                0,
                pages.Count,
                pages);
        }

    }
}
