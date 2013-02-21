using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
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
using MediaBrowser.Common.Extensions;
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
        #region Events
        /// <summary>
        /// Fires whenever any validation routine adds or removes items.  The added and removed items are properties of the args.
        /// *** Will fire asynchronously. ***
        /// </summary>
        public event EventHandler<ChildrenChangedEventArgs> LibraryChanged;
        public void OnLibraryChanged(ChildrenChangedEventArgs args)
        {
            if (LibraryChanged != null)
            {
                Task.Run(() => LibraryChanged(this, args));
            }
        }

        #endregion
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

        public BaseItem ResolveItem(ItemResolveEventArgs args)
        {
            // Try first priority resolvers
            for (int i = 0; i < EntityResolvers.Length; i++)
            {
                var item = EntityResolvers[i].ResolvePath(args);

                if (item != null)
                {
                    item.ResolveArgs = args;
                    return item;
                }
            }

            return null;
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
            RootFolder.ChildrenChanged += RootFolder_ChildrenChanged;

            DirectoryWatchers.Start();
        }

        void RootFolder_ChildrenChanged(object sender, ChildrenChangedEventArgs e)
        {
            Logger.LogDebugInfo("Root Folder Children Changed.  Added: " + e.ItemsAdded.Count + " Removed: " + e.ItemsRemoved.Count());
            //re-start the directory watchers
            DirectoryWatchers.Stop();
            DirectoryWatchers.Start();
            //Task.Delay(30000); //let's wait and see if more data gets filled in...
            var allChildren = RootFolder.RecursiveChildren;
            Logger.LogDebugInfo(string.Format("Loading complete.  Movies: {0} Episodes: {1} Folders: {2}", allChildren.OfType<Entities.Movies.Movie>().Count(), allChildren.OfType<Entities.TV.Episode>().Count(), allChildren.Where(i => i is Folder && !(i is Series || i is Season)).Count()));
            //foreach (var child in allChildren)
            //{
            //    Logger.LogDebugInfo("(" + child.GetType().Name + ") " + child.Name + " (" + child.Path + ")");
            //}
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
                password = password ?? string.Empty;
                result.Success = password.GetMD5().ToString().Equals(user.Password);
            }

            // Update LastActivityDate and LastLoginDate, then save
            if (result.Success)
            {
                user.LastActivityDate = user.LastLoginDate = DateTime.UtcNow;
                SaveUser(user);
            }

            return result;
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
            user.Password = ("1234").GetMD5().ToString();
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
        internal async Task ExecuteMetadataProviders(BaseEntity item, bool allowInternetProviders = true)
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
                    await provider.FetchIfNeededAsync(item).ConfigureAwait(false);
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
