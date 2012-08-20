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
        public IEnumerable<IBaseItemResolver> EntityResolvers { get; private set; }

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
            await Task.Run(async () =>
            {
                await base.Init(progress);

                progress.Report(new TaskProgress() { Description = "Loading Users", PercentComplete = 15 });
                ReloadUsers();

                progress.Report(new TaskProgress() { Description = "Extracting FFMpeg", PercentComplete = 20 });
                await ExtractFFMpeg();

                progress.Report(new TaskProgress() { Description = "Loading Media Library", PercentComplete = 25 });
                await ReloadRoot();

                progress.Report(new TaskProgress() { Description = "Loading Complete", PercentComplete = 100 });
            });
        }

        protected override void OnComposablePartsLoaded()
        {
            AddCoreResolvers();
            AddCoreProviders();

            // The base class will start up all the plugins
            base.OnComposablePartsLoaded();
        }

        private void AddCoreResolvers()
        {
            List<IBaseItemResolver> list = EntityResolvers.ToList();

            // Add the core resolvers
            list.AddRange(new IBaseItemResolver[]{
                new AudioResolver(),
                new VideoResolver(),
                new VirtualFolderResolver(),
                new FolderResolver()
            });

            EntityResolvers = list;
        }

        private void AddCoreProviders()
        {
            List<BaseMetadataProvider> list = MetadataProviders.ToList();

            // Add the core resolvers
            list.InsertRange(0, new BaseMetadataProvider[]{
                new ImageFromMediaLocationProvider(),
                new LocalTrailerProvider(),
                new AudioInfoProvider(),
                new FolderProviderFromXml()
            });

            MetadataProviders = list;

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

            RootFolder = await ItemController.GetItem(null, MediaRootFolderPath) as Folder;

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

        public async Task ReloadItem(BaseItem item)
        {
            Folder folder = item as Folder;

            if (folder != null && folder.IsRoot)
            {
                await ReloadRoot();
            }
            else
            {
                if (!Directory.Exists(item.Path) && !File.Exists(item.Path))
                {
                    await ReloadItem(item.Parent);
                    return;
                }

                BaseItem newItem = await ItemController.GetItem(item.Parent, item.Path);

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
            var supportedProviders = Kernel.Instance.MetadataProviders.Where(i => i.Supports(item));

            // Start with non-internet providers. Run them sequentially
            foreach (BaseMetadataProvider provider in supportedProviders.Where(i => !i.RequiresInternet))
            {
                await provider.Fetch(item, args);
            }

            var internetProviders = supportedProviders.Where(i => i.RequiresInternet);

            if (internetProviders.Any())
            {
                // Now execute internet providers in parallel
                await Task.WhenAll(
                    internetProviders.Select(i => i.Fetch(item, args))
                    );
            }
        }

        /// <summary>
        /// Run these during Init.
        /// Can't run do this on-demand because there will be multiple workers accessing them at once and we'd have to lock them
        /// </summary>
        private async Task ExtractFFMpeg()
        {
            // FFMpeg.exe
            await ExtractFFMpeg(ApplicationPaths.FFMpegPath);
            await ExtractFFMpeg(ApplicationPaths.FFProbePath);
        }

        private async Task ExtractFFMpeg(string exe)
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
                    await stream.CopyToAsync(fileStream);
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
