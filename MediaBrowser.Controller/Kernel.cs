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

        public override KernelContext KernelContext
        {
            get { return KernelContext.Server; }
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

            //watch the root folder children for changes
            RootFolder.ChildrenChanged += RootFolder_ChildrenChanged;

            System.Threading.Thread.Sleep(25000);
            var allChildren = RootFolder.RecursiveChildren;
            Logger.LogInfo(string.Format("Loading complete.  Movies: {0} Episodes: {1}", allChildren.OfType<Entities.Movies.Movie>().Count(), allChildren.OfType<Entities.TV.Episode>().Count()));
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

            DirectoryWatchers.Start();
        }

        void RootFolder_ChildrenChanged(object sender, ChildrenChangedEventArgs e)
        {
            //re-start the directory watchers
            DirectoryWatchers.Stop();
            DirectoryWatchers.Start();
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
            AuthenticationResult result = new AuthenticationResult();

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

                //item.Parent.ActualChildren = children.ToArray();
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
            user.Name = "Abobader";
            user.Id = Guid.NewGuid();
            user.LastLoginDate = DateTime.UtcNow.AddDays(-1);
            user.LastActivityDate = DateTime.UtcNow.AddHours(-3);
            user.Password = ("1234").GetMD5().ToString();
            list.Add(user);

            user = new User();
            user.Name = "Scottisafool";
            user.Id = Guid.NewGuid();
            list.Add(user);

            user = new User();
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
                using (FileStream fileStream = new FileStream(exe, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
    }
}
