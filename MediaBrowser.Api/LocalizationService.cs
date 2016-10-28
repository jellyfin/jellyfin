using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetCultures
    /// </summary>
    [Route("/Localization/Cultures", "GET", Summary = "Gets known cultures")]
    public class GetCultures : IReturn<List<CultureDto>>
    {
    }

    /// <summary>
    /// Class GetCountries
    /// </summary>
    [Route("/Localization/Countries", "GET", Summary = "Gets known countries")]
    public class GetCountries : IReturn<List<CountryInfo>>
    {
    }

    /// <summary>
    /// Class ParentalRatings
    /// </summary>
    [Route("/Localization/ParentalRatings", "GET", Summary = "Gets known parental ratings")]
    public class GetParentalRatings : IReturn<List<ParentalRating>>
    {
    }

    /// <summary>
    /// Class ParentalRatings
    /// </summary>
    [Route("/Localization/Options", "GET", Summary = "Gets localization options")]
    public class GetLocalizationOptions : IReturn<List<LocalizatonOption>>
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
        public LocalizationService(ILocalizationManager localization)
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
            var result = _localization.GetParentalRatings().ToList();

            return ToOptimizedResult(result);
        }

        public object Get(GetLocalizationOptions request)
        {
            var result = _localization.GetLocalizationOptions().ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCountries request)
        {
            var result = _localization.GetCountries().ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCultures request)
        {
            var result = _localization.GetCultures().ToList();

            return ToOptimizedResult(result);
        }
    }

}
