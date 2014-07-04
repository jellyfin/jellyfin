using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Branding;
using ServiceStack;

namespace MediaBrowser.Api
{
    [Route("/Branding/Configuration", "GET", Summary = "Gets branding configuration")]
    public class GetBrandingOptions : IReturn<BrandingOptions>
    {
    }
    
    public class BrandingService : BaseApiService
    {
        private readonly IConfigurationManager _config;

        public BrandingService(IConfigurationManager config)
        {
            _config = config;
        }

        public object Get(GetBrandingOptions request)
        {
            var result = _config.GetConfiguration<BrandingOptions>("branding");

            return ToOptimizedResult(result);
        }
    }
}
