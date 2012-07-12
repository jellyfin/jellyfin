using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MediaBrowser.Common.Plugins
{
    public class PluginController
    {
        public string PluginsPath { get; set; }

        public PluginController(string pluginFolderPath)
        {
            PluginsPath = pluginFolderPath;
        }

        public IEnumerable<IPlugin> GetAllPlugins()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            
            if (!Directory.Exists(PluginsPath))
            {
                Directory.CreateDirectory(PluginsPath);
            }

            List<IPlugin> plugins = new List<IPlugin>();

            foreach (string folder in Directory.GetDirectories(PluginsPath, "*", SearchOption.TopDirectoryOnly))
            {
                IPlugin plugin = GetPluginFromDirectory(folder);

                plugin.Path = folder;

                if (plugin != null)
                {
                    plugins.Add(plugin);
                }
            }

            return plugins;
        }

        private IPlugin GetPluginFromDirectory(string path)
        {
            string dll = Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (!string.IsNullOrEmpty(dll))
            {
                return GetPluginFromDll(dll);
            }

            return null;
        }

        private IPlugin GetPluginFromDll(string path)
        {
            return FindPlugin(Assembly.Load(File.ReadAllBytes(path)));
        }

        private IPlugin FindPlugin(Assembly assembly)
        {
            var plugin = assembly.GetTypes().Where(type => typeof(IPlugin).IsAssignableFrom(type)).FirstOrDefault();

            if (plugin != null)
            {
                return plugin.GetConstructor(Type.EmptyTypes).Invoke(null) as IPlugin;
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
