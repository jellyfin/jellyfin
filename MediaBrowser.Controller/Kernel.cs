using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Weather;
using MediaBrowser.Model.Authentication;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Progress;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

        public override KernelContext KernelContext
        {
            get { return KernelContext.Server; }
        }

        /// <summary>
        /// Gets the list of currently registered weather prvoiders
        /// </summary>
        [ImportMany(typeof(BaseWeatherProvider))]
        public IEnumerable<BaseWeatherProvider> WeatherProviders { get; private set; }

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
        /// Gets the list of currently registered entity resolvers
        /// </summary>
        [ImportMany(typeof(BaseImageProcessor))]
        public IEnumerable<BaseImageProcessor> ImageProcessors { get; private set; }

        /// <summary>
        /// Creates a kernel based on a Data path, which is akin to our current programdata path
        /// </summary>
        public Kernel()
            : base()
        {
            Instance = this;
        }

        /// <summary>
        /// Performs initializations that only occur once
        /// </summary>
        protected override void InitializeInternal(IProgress<TaskProgress> progress)
        {
            base.InitializeInternal(progress);

            ItemController = new ItemController();
            DirectoryWatchers = new DirectoryWatchers();

            ItemController.PreBeginResolvePath += ItemController_PreBeginResolvePath;
            ItemController.BeginResolvePath += ItemController_BeginResolvePath;

            ExtractFFMpeg();
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        protected override async Task ReloadInternal(IProgress<TaskProgress> progress)
        {
            await base.ReloadInternal(progress).ConfigureAwait(false);

            ReportProgress(progress, "Loading Users");
            ReloadUsers();

            ReportProgress(progress, "Loading Media Library");

            await ReloadRoot(allowInternetProviders: false).ConfigureAwait(false);
        }

        /// <summary>
        /// Completely disposes the Kernel
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            DirectoryWatchers.Stop();

            ItemController.PreBeginResolvePath -= ItemController_PreBeginResolvePath;
            ItemController.BeginResolvePath -= ItemController_BeginResolvePath;
        }

        protected override void OnComposablePartsLoaded()
        {
            // The base class will start up all the plugins
            base.OnComposablePartsLoaded();

            // Sort the resolvers by priority
            EntityResolvers = EntityResolversEnumerable.OrderBy(e => e.Priority).ToArray();

            // Sort the providers by priority
            MetadataProviders = MetadataProvidersEnumerable.OrderBy(e => e.Priority).ToArray();
        }

        /// <summary>
        /// Fires when a path is about to be resolved, but before child folders and files 
        /// have been collected from the file system.
        /// This gives us a chance to cancel it if needed, resulting in the path being ignored
        /// </summary>
        void ItemController_PreBeginResolvePath(object sender, PreBeginResolveEventArgs e)
        {
            // Ignore hidden files and folders
            if (e.IsHidden || e.IsSystemFile)
            {
                e.Cancel = true;
            }

            // Ignore any folders named "trailers"
            else if (Path.GetFileName(e.Path).Equals("trailers", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
            }

            // Don't try and resolve files within the season metadata folder
            else if (Path.GetFileName(e.Path).Equals("metadata", StringComparison.OrdinalIgnoreCase) && e.IsDirectory)
            {
                if (e.Parent is Season || e.Parent is Series)
                {
                    e.Cancel = true;
                }
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

        /// <summary>
        /// Gets the default user to use when EnableUserProfiles is false
        /// </summary>
        public User GetDefaultUser()
        {
            User user = Users.FirstOrDefault();

            return user;
        }

        /// <summary>
        /// Persists a User
        /// </summary>
        public void SaveUser(User user)
        {

        }

        /// <summary>
        /// Authenticates a User and returns a result indicating whether or not it succeeded
        /// </summary>
        public AuthenticationResult AuthenticateUser(User user, string password)
        {
            var result = new AuthenticationResult();

            // When EnableUserProfiles is false, only the default User can login
            if (!Configuration.EnableUserProfiles)
            {
                result.Success = user.Id == GetDefaultUser().Id;
            }
            else if (string.IsNullOrEmpty(user.Password))
            {
                result.Success = true;
            }
            else
            {
                result.Success = GetMD5((password ?? string.Empty)).ToString().Equals(user.Password);
            }

            // Update LastActivityDate and LastLoginDate, then save
            if (result.Success)
            {
                user.LastActivityDate = user.LastLoginDate = DateTime.UtcNow;
                SaveUser(user);
            }

            return result;
        }

        public async Task ReloadItem(BaseItem item)
        {
            var folder = item as Folder;

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
            var list = new List<User>();

            // Return a dummy user for now since all calls to get items requre a userId
            var user = new User { };

            user.Name = "Default User";
            user.Id = Guid.Parse("5d1cf7fce25943b790d140095457a42b");
            user.PrimaryImagePath = "D:\\Video\\TV\\Archer (2009)\\backdrop.jpg";
            list.Add(user);

            user = new User { };
            user.Name = "Abobader";
            user.Id = Guid.NewGuid();
            user.LastLoginDate = DateTime.UtcNow.AddDays(-1);
            user.LastActivityDate = DateTime.UtcNow.AddHours(-3);
            user.Password = GetMD5("1234").ToString();
            list.Add(user);

            user = new User { };
            user.Name = "Scottisafool";
            user.Id = Guid.NewGuid();
            list.Add(user);

            user = new User { };
            user.Name = "Redshirt";
            user.Id = Guid.NewGuid();
            list.Add(user);

            /*user = new User();
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
            list.Add(user);*/

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
                using (var fileStream = new FileStream(exe, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
    }
}
