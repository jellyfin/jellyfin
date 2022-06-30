using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emby.Server.Implementations.HomeScreen.Sections;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emby.Server.Implementations.HomeScreen
{
    public class HomeScreenManager : IHomeScreenManager
    {
        private Dictionary<string, IHomeScreenSection> m_delegates = new Dictionary<string, IHomeScreenSection>();
        private Dictionary<Guid, bool> m_userFeatureEnabledStates = new Dictionary<Guid, bool>();

        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationPaths _applicationPaths;

        public HomeScreenManager(IServiceProvider serviceProvider, IApplicationPaths applicationPaths)
        {
            _serviceProvider = serviceProvider;
            _applicationPaths = applicationPaths;

            string userFeatureEnabledPath = Path.Combine(_applicationPaths.CachePath, "userFeatureEnabled.json");
            if (File.Exists(userFeatureEnabledPath))
            {
                m_userFeatureEnabledStates = JsonConvert.DeserializeObject<Dictionary<Guid, bool>>(File.ReadAllText(userFeatureEnabledPath));
            }

            RegisterResultsDelegate<MyMediaSection>();
            RegisterResultsDelegate<ContinueWatchingSection>();
            RegisterResultsDelegate<NextUpSection>();
            RegisterResultsDelegate<LatestMoviesSection>();
            RegisterResultsDelegate<LatestShowsSection>();
        }

        public IEnumerable<IHomeScreenSection> GetSectionTypes()
        {
            return m_delegates.Values;
        }

        public QueryResult<BaseItemDto> InvokeResultsDelegate(string key, HomeScreenSectionPayload payload)
        {
            if (m_delegates.ContainsKey(key))
            {
                return m_delegates[key].GetResults(payload);
            }

            return new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>());
        }

        public void RegisterResultsDelegate<T>() where T : IHomeScreenSection
        {
            T handler = ActivatorUtilities.CreateInstance<T>(_serviceProvider);

            if (!m_delegates.ContainsKey(handler.Section))
            {
                m_delegates.Add(handler.Section, handler);
            }
            else
            {
                throw new Exception($"Section type '{handler.Section}' has already been registered to type '{m_delegates[handler.Section].GetType().FullName}'.");
            }
        }

        public bool GetUserFeatureEnabled(Guid userId)
        {
            if (m_userFeatureEnabledStates.ContainsKey(userId))
            {
                return m_userFeatureEnabledStates[userId];
            }

            m_userFeatureEnabledStates.Add(userId, false);

            return false;
        }

        public void SetUserFeatureEnabled(Guid userId, bool enabled)
        {
            if (!m_userFeatureEnabledStates.ContainsKey(userId))
            {
                m_userFeatureEnabledStates.Add(userId, enabled);
            }

            m_userFeatureEnabledStates[userId] = enabled;

            string userFeatureEnabledPath = Path.Combine(_applicationPaths.CachePath, "userFeatureEnabled.json");
            File.WriteAllText(userFeatureEnabledPath, JObject.FromObject(m_userFeatureEnabledStates).ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}
