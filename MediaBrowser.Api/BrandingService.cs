using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    [Route("/Branding/Configuration", "GET", Summary = "Gets branding configuration")]
    public class GetBrandingOptions : IReturn<BrandingOptions>
    {
    }

    [Route("/Branding/Css", "GET", Summary = "Gets custom css")]
    [Route("/Branding/Css.css", "GET", Summary = "Gets custom css")]
    public class GetBrandingCss
    {
    }

    public class BrandingService : BaseApiService
    {
        public BrandingService(
            ILogger<BrandingService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
        }

        public object Get(GetBrandingOptions request)
        {
            return ServerConfigurationManager.GetConfiguration<BrandingOptions>("branding");
        }

        public object Get(GetBrandingCss request)
        {
            var result = ServerConfigurationManager.GetConfiguration<BrandingOptions>("branding");

            // When null this throws a 405 error under Mono OSX, so default to empty string
            return ResultFactory.GetResult(Request, result.CustomCss ?? string.Empty, "text/css");
        }
    }
}
