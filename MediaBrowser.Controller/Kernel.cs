using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller
{
    public class Kernel
    {
        public static Kernel Instance { get; private set; }

        public string DataPath { get; private set; }

        public HttpServer HttpServer { get; private set; }
        public ItemController ItemController { get; private set; }
        public UserController UserController { get; private set; }
        public PluginController PluginController { get; private set; }

        public Configuration Configuration { get; private set; }
        public IEnumerable<IPlugin> Plugins { get; private set; }
        public IEnumerable<User> Users { get; private set; }
        public Folder RootFolder { get; private set; }

        private DirectoryWatchers DirectoryWatchers { get; set; }

        private string MediaRootFolderPath
        {
            get
            {
                return Path.Combine(DataPath, "Root");
            }
        }

        /// <summary>
        /// Creates a kernal based on a Data path, which is akin to our current programdata path
        /// </summary>
        public Kernel(string dataPath)
        {
            Instance = this;

            DataPath = dataPath;

            Logger.LoggerInstance = new FileLogger(Path.Combine(DataPath, "Logs"));

            ItemController = new ItemController();
            UserController = new UserController(Path.Combine(DataPath, "Users"));
            PluginController = new PluginController(Path.Combine(DataPath, "Plugins"));
            DirectoryWatchers = new DirectoryWatchers();

            ItemController.PreBeginResolvePath += ItemController_PreBeginResolvePath;
            ItemController.BeginResolvePath += ItemController_BeginResolvePath;

            // Add support for core media types - audio, video, etc
            AddBaseItemType<Folder, FolderResolver>();
            AddBaseItemType<Audio, AudioResolver>();
            AddBaseItemType<Video, VideoResolver>();
        }

        /// <summary>
        /// Tells the kernel to start spinning up
        /// </summary>
        public void Init()
        {
            ReloadConfiguration();

            ReloadHttpServer();

            ReloadPlugins();

            // Get users from users folder
            // Load root media folder
            Parallel.Invoke(ReloadUsers, ReloadRoot);
            var b = true;
        }

        private void ReloadConfiguration()
        {
            // Deserialize config
            Configuration = GetConfiguration(DataPath);

            Logger.LoggerInstance.LogSeverity = Configuration.LogSeverity;
        }

        private void ReloadPlugins()
        {
            // Find plugins
            Plugins = PluginController.GetAllPlugins();

            Parallel.For(0, Plugins.Count(), i =>
            {
                Plugins.ElementAt(i).Init();
            });
        }

        private void ReloadHttpServer()
        {
            if (HttpServer != null)
            {
                HttpServer.Dispose();
            }

            HttpServer = new HttpServer("http://+:" + Configuration.HttpServerPortNumber + "/mediabrowser/");
        }

        /// <summary>
        /// Registers a new BaseItem subclass
        /// </summary>
        public void AddBaseItemType<TBaseItemType, TResolverType>()
            where TBaseItemType : BaseItem, new()
            where TResolverType : BaseItemResolver<TBaseItemType>, new()
        {
            ItemController.AddResovler<TBaseItemType, TResolverType>();
        }

        /// <summary>
        /// Fires when a path is about to be resolved, but before child folders and files 
        /// have been collected from the file system.
        /// This gives us a chance to cancel it if needed, resulting in the path being ignored
        /// </summary>
        void ItemController_PreBeginResolvePath(object sender, PreBeginResolveEventArgs e)
        {
            if (e.IsHidden || e.IsSystemFile)
            {
                // Ignore hidden files and folders
                e.Cancel = true;
            }

            else if (Path.GetFileName(e.Path).Equals("trailers", StringComparison.OrdinalIgnoreCase))
            {
                // Ignore any folders named "trailers"
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Fires when a path is about to be resolved, but after child folders and files 
        /// This gives us a chance to cancel it if needed, resulting in the path being ignored
        /// </summary>
        void ItemController_BeginResolvePath(object sender, ItemResolveEventArgs e)
        {
            if (e.IsFolder)
            {
                if (e.ContainsFile(".ignore"))
                {
                    // Ignore any folders containing a file called .ignore
                    e.Cancel = true;
                }
            }
        }

        private void ReloadUsers()
        {
            Users = UserController.GetAllUsers();
        }

        /// <summary>
        /// Reloads the root media folder
        /// </summary>
        public void ReloadRoot()
        {
            if (!Directory.Exists(MediaRootFolderPath))
            {
                Directory.CreateDirectory(MediaRootFolderPath);
            }

            DirectoryWatchers.Stop();

            RootFolder = ItemController.GetItem(MediaRootFolderPath) as Folder;

            DirectoryWatchers.Start();
        }

        private static MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();
        public static Guid GetMD5(string str)
        {
            lock (md5Provider)
            {
                return new Guid(md5Provider.ComputeHash(Encoding.Unicode.GetBytes(str)));
            }
        }

        private static Configuration GetConfiguration(string directory)
        {
            string file = Path.Combine(directory, "config.js");

            if (!File.Exists(file))
            {
                return new Configuration();
            }

            return JsonSerializer.Deserialize<Configuration>(file);
        }

        public void ReloadItem(BaseItem item)
        {
            Folder folder = item as Folder;

            if (folder != null && folder.IsRoot)
            {
                ReloadRoot();
            }
            else
            {
                if (!Directory.Exists(item.Path) && !File.Exists(item.Path))
                {
                    ReloadItem(item.Parent);
                    return;
                }

                BaseItem newItem = ItemController.GetItem(item.Parent, item.Path);

                List<BaseItem> children = item.Parent.Children.ToList();

                int index = children.IndexOf(item);

                children.RemoveAt(index);

                children.Insert(index, newItem);

                item.Parent.Children = children.ToArray();
            }
        }
    }
}
