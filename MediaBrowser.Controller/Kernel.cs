using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Progress;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller
{
    public class Kernel : BaseKernel<ServerConfiguration>
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
        /// Gets the list of currently registered entity resolvers
        /// </summary>
        [ImportMany(typeof(IBaseItemResolver))]
        public IEnumerable<IBaseItemResolver> EntityResolvers { get; private set; }

        /// <summary>
        /// Creates a kernal based on a Data path, which is akin to our current programdata path
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

        public override void Init(IProgress<TaskProgress> progress)
        {
            base.Init(progress);

            progress.Report(new TaskProgress() { Description = "Loading Users", PercentComplete = 15 });
            ReloadUsers();

            progress.Report(new TaskProgress() { Description = "Loading Media Library", PercentComplete = 20 });
            ReloadRoot();

            progress.Report(new TaskProgress() { Description = "Loading Complete", PercentComplete = 100 });
        }

        protected override void OnComposablePartsLoaded()
        {
            List<IBaseItemResolver> resolvers = EntityResolvers.ToList();

            // Add the internal resolvers
            resolvers.Add(new VideoResolver());
            resolvers.Add(new AudioResolver());
            resolvers.Add(new FolderResolver());

            EntityResolvers = resolvers;

            // The base class will start up all the plugins
            base.OnComposablePartsLoaded();
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

        /// <summary>
        /// Finds a library item by Id
        /// </summary>
        public BaseItem GetItemById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return RootFolder;
            }

            return RootFolder.FindById(id);
        }

        /// <summary>
        /// Gets all years from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each year appears
        /// </summary>
        public IEnumerable<IBNItem<Year>> GetAllYears(Folder parent, User user)
        {
            Dictionary<int, int> data = new Dictionary<int, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                // Add the year from the item to the data dictionary
                // If the year already exists, increment the count
                if (item.ProductionYear == null)
                {
                    continue;
                }

                if (!data.ContainsKey(item.ProductionYear.Value))
                {
                    data.Add(item.ProductionYear.Value, 1);
                }
                else
                {
                    data[item.ProductionYear.Value]++;
                }
            }

            // Now go through the dictionary and create a Category for each studio
            List<IBNItem<Year>> list = new List<IBNItem<Year>>();

            foreach (int key in data.Keys)
            {
                // Get the original entity so that we can also supply the PrimaryImagePath
                Year entity = Kernel.Instance.ItemController.GetYear(key);

                if (entity != null)
                {
                    list.Add(new IBNItem<Year>()
                    {
                        Item = entity,
                        BaseItemCount = data[key]
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// Gets all studios from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each studio appears
        /// </summary>
        public IEnumerable<IBNItem<Studio>> GetAllStudios(Folder parent, User user)
        {
            Dictionary<string, int> data = new Dictionary<string, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                // Add each studio from the item to the data dictionary
                // If the studio already exists, increment the count
                if (item.Studios == null)
                {
                    continue;
                }

                foreach (string val in item.Studios)
                {
                    if (!data.ContainsKey(val))
                    {
                        data.Add(val, 1);
                    }
                    else
                    {
                        data[val]++;
                    }
                }
            }

            // Now go through the dictionary and create a Category for each studio
            List<IBNItem<Studio>> list = new List<IBNItem<Studio>>();

            foreach (string key in data.Keys)
            {
                // Get the original entity so that we can also supply the PrimaryImagePath
                Studio entity = Kernel.Instance.ItemController.GetStudio(key);

                if (entity != null)
                {
                    list.Add(new IBNItem<Studio>()
                    {
                        Item = entity,
                        BaseItemCount = data[key]
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// Gets all genres from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each genres appears
        /// </summary>
        public IEnumerable<IBNItem<Genre>> GetAllGenres(Folder parent, User user)
        {
            Dictionary<string, int> data = new Dictionary<string, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                // Add each genre from the item to the data dictionary
                // If the genre already exists, increment the count
                if (item.Genres == null)
                {
                    continue;
                }

                foreach (string val in item.Genres)
                {
                    if (!data.ContainsKey(val))
                    {
                        data.Add(val, 1);
                    }
                    else
                    {
                        data[val]++;
                    }
                }
            }

            // Now go through the dictionary and create a Category for each genre
            List<IBNItem<Genre>> list = new List<IBNItem<Genre>>();

            foreach (string key in data.Keys)
            {
                // Get the original entity so that we can also supply the PrimaryImagePath
                Genre entity = Kernel.Instance.ItemController.GetGenre(key);

                if (entity != null)
                {
                    list.Add(new IBNItem<Genre>()
                    {
                        Item = entity,
                        BaseItemCount = data[key]
                    });
                }
            }

            return list;
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
    }
}
