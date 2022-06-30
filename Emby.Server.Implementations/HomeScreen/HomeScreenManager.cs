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
    /// <summary>
    /// Manager for the Modular Home Screen.
    /// </summary>
    public class HomeScreenManager : IHomeScreenManager
    {
        private Dictionary<string, IHomeScreenSection> m_delegates = new Dictionary<string, IHomeScreenSection>();
        private Dictionary<Guid, bool> m_userFeatureEnabledStates = new Dictionary<Guid, bool>();

        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationPaths _applicationPaths;

        private const string SettingsFile = "ModularHomeSettings.json";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serviceProvider">Instance of the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        public HomeScreenManager(IServiceProvider serviceProvider, IApplicationPaths applicationPaths)
        {
            _serviceProvider = serviceProvider;
            _applicationPaths = applicationPaths;

            string userFeatureEnabledPath = Path.Combine(_applicationPaths.CachePath, "userFeatureEnabled.json");
            if (File.Exists(userFeatureEnabledPath))
            {
                m_userFeatureEnabledStates = JsonConvert.DeserializeObject<Dictionary<Guid, bool>>(File.ReadAllText(userFeatureEnabledPath)) ?? new Dictionary<Guid, bool>();
            }

            RegisterResultsDelegate<MyMediaSection>();
            RegisterResultsDelegate<ContinueWatchingSection>();
            RegisterResultsDelegate<NextUpSection>();
            RegisterResultsDelegate<LatestMoviesSection>();
            RegisterResultsDelegate<LatestShowsSection>();
        }

        /// <inheritdoc/>
        public IEnumerable<IHomeScreenSection> GetSectionTypes()
        {
            return m_delegates.Values;
        }

        /// <inheritdoc/>
        public QueryResult<BaseItemDto> InvokeResultsDelegate(string key, HomeScreenSectionPayload payload)
        {
            if (m_delegates.ContainsKey(key))
            {
                return m_delegates[key].GetResults(payload);
            }

            return new QueryResult<BaseItemDto>(Array.Empty<BaseItemDto>());
        }

        /// <inheritdoc/>
        public void RegisterResultsDelegate<T>() where T : IHomeScreenSection
        {
            T handler = ActivatorUtilities.CreateInstance<T>(_serviceProvider);

            if (handler.Section != null)
            {
                if (!m_delegates.ContainsKey(handler.Section))
                {
                    m_delegates.Add(handler.Section, handler);
                }
                else
                {
                    throw new Exception($"Section type '{handler.Section}' has already been registered to type '{m_delegates[handler.Section].GetType().FullName}'.");
                }
            }
        }

        /// <inheritdoc/>
        public bool GetUserFeatureEnabled(Guid userId)
        {
            if (m_userFeatureEnabledStates.ContainsKey(userId))
            {
                return m_userFeatureEnabledStates[userId];
            }

            m_userFeatureEnabledStates.Add(userId, false);

            return false;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public ModularHomeUserSettings? GetUserSettings(Guid userId)
        {
            string pluginSettings = Path.Combine(_applicationPaths.PluginsPath, "ModularHome", SettingsFile);

            if (System.IO.File.Exists(pluginSettings))
            {
                JArray settings = JArray.Parse(System.IO.File.ReadAllText(pluginSettings));

                if (settings.Select(x => JsonConvert.DeserializeObject<ModularHomeUserSettings>(x.ToString())).Any(x => x != null && x.UserId.Equals(userId)))
                {
                    return settings.Select(x => JsonConvert.DeserializeObject<ModularHomeUserSettings>(x.ToString())).First(x => x != null && x.UserId.Equals(userId));
                }
            }

            return new ModularHomeUserSettings
            {
                UserId = userId
            };
        }

        /// <inheritdoc/>
        public bool UpdateUserSettings(Guid userId, ModularHomeUserSettings userSettings)
        {
            string pluginSettings = Path.Combine(_applicationPaths.PluginsPath, "ModularHome", SettingsFile);
            FileInfo fInfo = new FileInfo(pluginSettings);
            fInfo.Directory?.Create();

            JArray settings = new JArray();
            List<ModularHomeUserSettings?> newSettings = new List<ModularHomeUserSettings?>();

            if (File.Exists(pluginSettings))
            {
                settings = JArray.Parse(System.IO.File.ReadAllText(pluginSettings));
                newSettings = settings.Select(x => JsonConvert.DeserializeObject<ModularHomeUserSettings>(x.ToString())).ToList();
                newSettings.RemoveAll(x => x != null && x.UserId.Equals(userId));

                newSettings.Add(userSettings);

                settings.Clear();
            }

            foreach (ModularHomeUserSettings? userSetting in newSettings)
            {
                settings.Add(JObject.FromObject(userSetting ?? new ModularHomeUserSettings()));
            }

            File.WriteAllText(pluginSettings, settings.ToString(Formatting.Indented));

            return true;
        }
    }
}
