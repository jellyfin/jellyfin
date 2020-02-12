using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetCultures
    /// </summary>
    [Route("/Localization/Cultures", "GET", Summary = "Gets known cultures")]
    public class GetCultures : IReturn<CultureDto[]>
    {
    }

    /// <summary>
    /// Class GetCountries
    /// </summary>
    [Route("/Localization/Countries", "GET", Summary = "Gets known countries")]
    public class GetCountries : IReturn<CountryInfo[]>
    {
    }

    /// <summary>
    /// Class ParentalRatings
    /// </summary>
    [Route("/Localization/ParentalRatings", "GET", Summary = "Gets known parental ratings")]
    public class GetParentalRatings : IReturn<ParentalRating[]>
    {
    }

    /// <summary>
    /// Class ParentalRatings
    /// </summary>
    [Route("/Localization/Options", "GET", Summary = "Gets localization options")]
    public class GetLocalizationOptions : IReturn<LocalizationOption[]>
    {
    }

    /// <summary>
    /// Class CulturesService
    /// </summary>
    [Authenticated(AllowBeforeStartupWizard = true)]
    public class LocalizationService : BaseApiService
    {
        /// <summary>
        /// The _localization
        /// </summary>
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationService"/> class.
        /// </summary>
        /// <param name="localization">The localization.</param>
        public LocalizationService(
            ILogger<LocalizationService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ILocalizationManager localization)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _localization = localization;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetParentalRatings request)
        {
            var result = _localization.GetParentalRatings();

            return ToOptimizedResult(result);
        }

        public object Get(GetLocalizationOptions request)
        {
            var result = _localization.GetLocalizationOptions();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCountries request)
        {
            var result = _localization.GetCountries();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCultures request)
        {
            var result = _localization.GetCultures();

            return ToOptimizedResult(result);
        }
    }

}
