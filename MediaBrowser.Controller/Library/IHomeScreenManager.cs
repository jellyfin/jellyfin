using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Library
{
    public interface IHomeScreenManager
    {
        public delegate QueryResult<BaseItemDto> GetResultsDelegate(HomeScreenSectionPayload payload);

        void RegisterResultsDelegate<T>() where T : IHomeScreenSection;

        IEnumerable<IHomeScreenSection> GetSectionTypes();

        QueryResult<BaseItemDto> InvokeResultsDelegate(string key, HomeScreenSectionPayload payload);

        bool GetUserFeatureEnabled(Guid userId);

        void SetUserFeatureEnabled(Guid userId, bool enabled);
    }

    public interface IHomeScreenSection
    {
        public string Section { get; }

        public string DisplayText { get; set; }

        public int Limit { get; }

        public string Route { get; }

        public string AdditionalData { get; set; }

        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload);
    }

    public class HomeScreenSectionInfo
    {
        public string Section { get; set; }

        public string DisplayText { get; set; }

        public int Limit { get; set; }

        public string Route { get; set; }

        public string AdditionalData { get; set; }

    }

    public static class HomeScreenSectionExtensions
    {

        public static HomeScreenSectionInfo AsInfo(this IHomeScreenSection section)
        {
            return new HomeScreenSectionInfo
            {
                Section = section.Section,
                DisplayText = section.DisplayText,
                AdditionalData = section.AdditionalData,
                Route = section.Route,
                Limit = section.Limit
            };
        }
    }
}
