using MediaBrowser.Common.Implementations.HttpServer;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MoreLinq;
using ServiceStack.ServiceHost;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetCultures
    /// </summary>
    [Route("/Localization/Cultures", "GET")]
    public class GetCultures : IReturn<List<CultureDto>>
    {
    }

    /// <summary>
    /// Class GetCountries
    /// </summary>
    [Route("/Localization/Countries", "GET")]
    public class GetCountries : IReturn<List<CountryInfo>>
    {
    }

    /// <summary>
    /// Class ParentalRatings
    /// </summary>
    [Route("/Localization/ParentalRatings", "GET")]
    public class GetParentalRatings : IReturn<List<ParentalRating>>
    {
    }

    /// <summary>
    /// Class CulturesService
    /// </summary>
    public class LocalizationService : BaseRestService
    {
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetParentalRatings request)
        {
            var ratings =
                Ratings.RatingsDict.Select(k => new ParentalRating { Name = k.Key, Value = k.Value });

            var result = ratings.OrderBy(p => p.Value).Where(p => p.Value > 0).ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCountries request)
        {
            var result = CultureInfo.GetCultures(CultureTypes.SpecificCultures)

                .Select(c => new RegionInfo(c.LCID))
                .OrderBy(c => c.DisplayName)

                // Try to eliminate dupes
                .DistinctBy(c => c.TwoLetterISORegionName)

                .Select(c => new CountryInfo
                {
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    TwoLetterISORegionName = c.TwoLetterISORegionName,
                    ThreeLetterISORegionName = c.ThreeLetterISORegionName
                })
                .ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCultures request)
        {
            var result = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .OrderBy(c => c.DisplayName)

                // Try to eliminate dupes
                .DistinctBy(c => c.TwoLetterISOLanguageName + c.ThreeLetterISOLanguageName)

                .Select(c => new CultureDto
                {
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    ThreeLetterISOLanguageName = c.ThreeLetterISOLanguageName,
                    TwoLetterISOLanguageName = c.TwoLetterISOLanguageName
                })
                .ToList();

            return ToOptimizedResult(result);
        }
    }

}
