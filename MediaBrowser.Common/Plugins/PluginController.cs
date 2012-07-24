using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Common.Kernel;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Manages Plugins within the PluginsPath directory
    /// </summary>
    public class PluginController
    {
        public string PluginsPath { get; set; }

        /// <summary>
        /// Gets the list of currently loaded plugins
        /// </summary>
        public IEnumerable<BasePlugin> Plugins { get; private set; }
        
        /// <summary>
        /// Initializes the controller
        /// </summary>
        public void Init(KernelContext context)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            Plugins = GetAllPlugins();

            Parallel.For(0, Plugins.Count(), i =>
            {
                var plugin = Plugins.ElementAt(i);

                plugin.ReloadConfiguration();

                if (plugin.Enabled)
                {
                    if (context == KernelContext.Server)
                    {
                        plugin.InitInServer();
                    }
                    else
                    {
                        plugin.InitInUI();
                    }
                }
            });
        }

        /// <summary>
        /// Gets all plugins within PluginsPath
        /// </summary>
        /// <returns></returns>
        private IEnumerable<BasePlugin> GetAllPlugins()
        {
            if (!Directory.Exists(PluginsPath))
            {
                Directory.CreateDirectory(PluginsPath);
            }

            List<BasePlugin> plugins = new List<BasePlugin>();

            foreach (string folder in Directory.GetDirectories(PluginsPath, "*", SearchOption.TopDirectoryOnly))
            {
                BasePlugin plugin = GetPluginFromDirectory(folder);

                plugin.Path = folder;

                if (plugin != null)
                {
                    plugins.Add(plugin);
                }
            }

            return plugins;
        }

        private BasePlugin GetPluginFromDirectory(string path)
        {
            string dll = Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (!string.IsNullOrEmpty(dll))
            {
                return GetPluginFromDll(dll);
            }

            return null;
        }

        private BasePlugin GetPluginFromDll(string path)
        {
            return GetPluginFromDll(Assembly.Load(File.ReadAllBytes(path)));
        }

        private BasePlugin GetPluginFromDll(Assembly assembly)
        {
            var plugin = assembly.GetTypes().Where(type => typeof(BasePlugin).IsAssignableFrom(type)).FirstOrDefault();

            if (plugin != null)
            {
                BasePlugin instance = plugin.GetConstructor(Type.EmptyTypes).Invoke(null) as BasePlugin;

                instance.Version = assembly.GetName().Version;

                return instance;
            }

            return null;
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);

            IEnumerable<string> dllPaths = Directory.GetFiles(PluginsPath, "*.dll", SearchOption.AllDirectories);

            string dll = dllPaths.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == assemblyName.Name);

            if (!string.IsNullOrEmpty(dll))
            {
                return Assembly.Load(File.ReadAllBytes(dll));
            }

            return null;
        }
    }
}
