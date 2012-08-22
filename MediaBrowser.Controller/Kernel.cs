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
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Progress;

namespace MediaBrowser.Controller
{
    public class Kernel : BaseKernel<ServerConfiguration, ServerApplicationPaths>
    {
        public static Kernel Instance { get; private set; }

        public ItemController ItemController { get; private set; }

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
        public IEnumerable<BaseMetadataProvider> MetadataProviders { get; private set; }

        /// <summary>
        /// Gets the list of currently registered entity resolvers
        /// </summary>
        [ImportMany(typeof(IBaseItemResolver))]
        private IEnumerable<IBaseItemResolver> EntityResolversEnumerable { get; set; }
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
            await ReloadRoot().ConfigureAwait(false);

            progress.Report(new TaskProgress() { Description = "Loading Complete", PercentComplete = 100 });
        }

        protected override void OnComposablePartsLoaded()
        {
            // The base class will start up all the plugins
            base.OnComposablePartsLoaded();

            // Sort the resolvers by priority
            EntityResolvers = EntityResolversEnumerable.OrderBy(e => e.Priority).ToArray();

            // Sort the providers by priority
            MetadataProviders = MetadataProviders.OrderBy(e => e.Priority);
            
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
            if (e.File.FileInfo.IsHidden || e.File.FileInfo.IsSystemFile)
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
        public async Task ReloadRoot()
        {
            if (!Directory.Exists(MediaRootFolderPath))
            {
                Directory.CreateDirectory(MediaRootFolderPath);
            }

            DirectoryWatchers.Stop();

            RootFolder = await ItemController.GetItem(MediaRootFolderPath).ConfigureAwait(false) as Folder;

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

            return list;
        }

        /// <summary>
        /// Runs all metadata providers for an entity
        /// </summary>
        internal async Task ExecuteMetadataProviders(BaseEntity item, ItemResolveEventArgs args)
        {
            // Get all supported providers
            BaseMetadataProvider[] supportedProviders = Kernel.Instance.MetadataProviders.Where(i => i.Supports(item)).ToArray();

            // Run them sequentially in order of priority
            for (int i = 0; i < supportedProviders.Length; i++)
            {
                var provider = supportedProviders[i];

                if (provider.RequiresInternet && !Configuration.EnableInternetProviders)
                {
                    continue;
                }

                await provider.FetchAsync(item, args).ConfigureAwait(false);
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
        private async void ExtractFFMpeg(string exe)
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
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
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
