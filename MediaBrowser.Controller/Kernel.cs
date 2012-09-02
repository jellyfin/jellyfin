using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Weather;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Progress;

namespace MediaBrowser.Controller
{
    public class Kernel : BaseKernel<ServerConfiguration, ServerApplicationPaths>
    {
        public static Kernel Instance { get; private set; }

        public ItemController ItemController { get; private set; }
        public WeatherClient WeatherClient { get; private set; }

        public IEnumerable<User> Users { get; private set; }
        public Folder RootFolder { get; private set; }

        private DirectoryWatchers DirectoryWatchers { get; set; }

        private string MediaRootFolderPath
        {
            get
            {
                return ApplicationPaths.RootFolderPath;
            }
        }

        /// <summary>
        /// Gets the list of currently registered metadata prvoiders
        /// </summary>
        [ImportMany(typeof(BaseMetadataProvider))]
        private IEnumerable<BaseMetadataProvider> MetadataProvidersEnumerable { get; set; }
        
        /// <summary>
        /// Once MEF has loaded the resolvers, sort them by priority and store them in this array
        /// Given the sheer number of times they'll be iterated over it'll be faster to loop through an array
        /// </summary>
        private BaseMetadataProvider[] MetadataProviders { get; set; }

        /// <summary>
        /// Gets the list of currently registered entity resolvers
        /// </summary>
        [ImportMany(typeof(IBaseItemResolver))]
        private IEnumerable<IBaseItemResolver> EntityResolversEnumerable { get; set; }

        /// <summary>
        /// Once MEF has loaded the resolvers, sort them by priority and store them in this array
        /// Given the sheer number of times they'll be iterated over it'll be faster to loop through an array
        /// </summary>
        internal IBaseItemResolver[] EntityResolvers { get; private set; }

        /// <summary>
        /// Creates a kernel based on a Data path, which is akin to our current programdata path
        /// </summary>
        public Kernel()
            : base()
        {
            Instance = this;

            ItemController = new ItemController();
            DirectoryWatchers = new DirectoryWatchers();
            WeatherClient = new WeatherClient();

            ItemController.PreBeginResolvePath += ItemController_PreBeginResolvePath;
            ItemController.BeginResolvePath += ItemController_BeginResolvePath;
        }

        public async override Task Init(IProgress<TaskProgress> progress)
        {
            ExtractFFMpeg();

            await base.Init(progress).ConfigureAwait(false);

            progress.Report(new TaskProgress() { Description = "Loading Users", PercentComplete = 15 });
            ReloadUsers();

            progress.Report(new TaskProgress() { Description = "Loading Media Library", PercentComplete = 25 });
            await ReloadRoot(allowInternetProviders: false).ConfigureAwait(false);

            progress.Report(new TaskProgress() { Description = "Loading Complete", PercentComplete = 100 });
        }

        protected override void OnComposablePartsLoaded()
        {
            // The base class will start up all the plugins
            base.OnComposablePartsLoaded();

            // Sort the resolvers by priority
            EntityResolvers = EntityResolversEnumerable.OrderBy(e => e.Priority).ToArray();
            
            // Sort the providers by priority
            MetadataProviders = MetadataProvidersEnumerable.OrderBy(e => e.Priority).ToArray();

            // Initialize the metadata providers
            Parallel.ForEach(MetadataProviders, provider =>
            {
                provider.Init();
            });
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
            if (e.ContainsFile(".ignore"))
            {
                // Ignore any folders containing a file called .ignore
                e.Cancel = true;
            }
        }

        private void ReloadUsers()
        {
            Users = GetAllUsers();
        }

        /// <summary>
        /// Reloads the root media folder
        /// </summary>
        public async Task ReloadRoot(bool allowInternetProviders = true)
        {
            if (!Directory.Exists(MediaRootFolderPath))
            {
                Directory.CreateDirectory(MediaRootFolderPath);
            }

            DirectoryWatchers.Stop();

            RootFolder = await ItemController.GetItem(MediaRootFolderPath, allowInternetProviders: allowInternetProviders).ConfigureAwait(false) as Folder;

            DirectoryWatchers.Start();
        }

        public static Guid GetMD5(string str)
        {
            using (var provider = new MD5CryptoServiceProvider())
            {
                return new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(str)));
            }
        }

        public async Task ReloadItem(BaseItem item)
        {
            Folder folder = item as Folder;

            if (folder != null && folder.IsRoot)
            {
                await ReloadRoot().ConfigureAwait(false);
            }
            else
            {
                if (!Directory.Exists(item.Path) && !File.Exists(item.Path))
                {
                    await ReloadItem(item.Parent).ConfigureAwait(false);
                    return;
                }

                BaseItem newItem = await ItemController.GetItem(item.Path, item.Parent).ConfigureAwait(false);

                List<BaseItem> children = item.Parent.Children.ToList();

                int index = children.IndexOf(item);

                children.RemoveAt(index);

                children.Insert(index, newItem);

                item.Parent.Children = children.ToArray();
            }
        }

        /// <summary>
        /// Finds a library item by Id
        /// </summary>
        public BaseItem GetItemById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return RootFolder;
            }

            return RootFolder.FindItemById(id);
        }

        /// <summary>
        /// Gets all users within the system
        /// </summary>
        private IEnumerable<User> GetAllUsers()
        {
            List<User> list = new List<User>();

            // Return a dummy user for now since all calls to get items requre a userId
            User user = new User();

            user.Name = "Default User";
            user.Id = Guid.Parse("5d1cf7fce25943b790d140095457a42b");

            list.Add(user);
            
            user = new User();
            user.Name = "Test User 1";
            user.Id = Guid.NewGuid();
            list.Add(user);

            user = new User();
            user.Name = "Test User 2";
            user.Id = Guid.NewGuid();
            list.Add(user);

            user = new User();
            user.Name = "Test User 3";
            user.Id = Guid.NewGuid();
            list.Add(user);

            user = new User();
            user.Name = "Test User 4";
            user.Id = Guid.NewGuid();
            list.Add(user);

            user = new User();
            user.Name = "Test User 5";
            user.Id = Guid.NewGuid();
            list.Add(user);

            user = new User();
            user.Name = "Test User 6";
            user.Id = Guid.NewGuid();
            list.Add(user);
            
            return list;
        }

        /// <summary>
        /// Runs all metadata providers for an entity
        /// </summary>
        internal async Task ExecuteMetadataProviders(BaseEntity item, ItemResolveEventArgs args, bool allowInternetProviders = true)
        {
            // Run them sequentially in order of priority
            for (int i = 0; i < MetadataProviders.Length; i++)
            {
                var provider = MetadataProviders[i];

                // Skip if internet providers are currently disabled
                if (provider.RequiresInternet && (!Configuration.EnableInternetProviders || !allowInternetProviders))
                {
                    continue;
                }

                // Skip if the provider doesn't support the current item
                if (!provider.Supports(item))
                {
                    continue;
                }

                try
                {
                    await provider.FetchAsync(item, args).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        private void ExtractFFMpeg()
        {
            ExtractFFMpeg(ApplicationPaths.FFMpegPath);
            ExtractFFMpeg(ApplicationPaths.FFProbePath);
        }

        /// <summary>
        /// Run these during Init.
        /// Can't run do this on-demand because there will be multiple workers accessing them at once and we'd have to lock them
        /// </summary>
        private void ExtractFFMpeg(string exe)
        {
            if (File.Exists(exe))
            {
                File.Delete(exe);
            }

            // Extract exe
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.Controller.FFMpeg." + Path.GetFileName(exe)))
            {
                using (FileStream fileStream = new FileStream(exe, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        protected override void DisposeComposableParts()
        {
            base.DisposeComposableParts();

            DisposeProviders();
        }

        /// <summary>
        /// Disposes all providers
        /// </summary>
        private void DisposeProviders()
        {
            if (MetadataProviders != null)
            {
                foreach (var provider in MetadataProviders)
                {
                    provider.Dispose();
                }
            }
        }
    }
}
