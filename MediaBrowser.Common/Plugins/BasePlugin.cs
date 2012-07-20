using System.IO;
using MediaBrowser.Common.Json;

namespace MediaBrowser.Common.Plugins
{
    public abstract class BasePlugin<TConfigurationType> : IPlugin
        where TConfigurationType : BasePluginConfiguration, new()
    {
        public string Path { get; set; }
        public TConfigurationType Configuration { get; private set; }

        private string ConfigurationPath
        {
            get
            {
                return System.IO.Path.Combine(Path, "config.js");
            }
        }
        
        public void Init()
        {
            Configuration = GetConfiguration();

            if (Configuration.Enabled)
            {
                InitInternal();
            }
        }

        protected abstract void InitInternal();

        private TConfigurationType GetConfiguration()
        {
            if (!File.Exists(ConfigurationPath))
            {
                return new TConfigurationType();
            }

            return JsonSerializer.DeserializeFromFile<TConfigurationType>(ConfigurationPath);
        }
    }

    public interface IPlugin
    {
        string Path { get; set; }

        void Init();
    }
}
