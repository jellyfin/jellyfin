using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SortOrder = MediaBrowser.Model.Entities.SortOrder;

namespace MediaBrowser.ServerApplication
{
    public partial class LibraryViewer : Form
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILibraryManager _libraryManager;
        private readonly IDisplayPreferencesRepository _displayPreferencesManager;
        private readonly IItemRepository _itemRepository;

        private User _currentUser;

        public LibraryViewer(IJsonSerializer jsonSerializer, IUserManager userManager, ILibraryManager libraryManager, IDisplayPreferencesRepository displayPreferencesManager, IItemRepository itemRepo)
        {
            InitializeComponent();

            _jsonSerializer = jsonSerializer;
            _libraryManager = libraryManager;
            _displayPreferencesManager = displayPreferencesManager;
            _itemRepository = itemRepo;

            foreach (var user in userManager.Users)
                selectUser.Items.Add(user);
            selectUser.Items.Insert(0, new User { Name = "Physical" });
            selectUser.SelectedIndex = 0;

            selectUser.SelectedIndexChanged += selectUser_SelectedIndexChanged;
            treeView1.AfterSelect += treeView1_AfterSelect;
        }

        void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                var item = (BaseItem)e.Node.Tag;
                lblType.Text = "Type: " + item.GetType().Name;

                var json = FormatJson(_jsonSerializer.SerializeToString(item));

                if (item is IHasMediaStreams)
                {
                    var mediaStreams = _itemRepository.GetMediaStreams(new MediaStreamQuery
                    {
                        ItemId = item.Id

                    }).ToList();

                    if (mediaStreams.Count > 0)
                    {
                        json += "\n\nMedia Streams:\n\n" + FormatJson(_jsonSerializer.SerializeToString(mediaStreams));
                    }
                }

                txtJson.Text = json;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            selectUser_SelectedIndexChanged(null, null);
        }

        void selectUser_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentUser = selectUser.SelectedItem as User;
            if (_currentUser != null)
                LoadTree();
        }

        private void LoadTree()
        {
            treeView1.Nodes.Clear();

            var isPhysical = _currentUser.Name == "Physical";
            IEnumerable<BaseItem> children = isPhysical ? new[] { _libraryManager.RootFolder } : _libraryManager.RootFolder.GetChildren(_currentUser, true);
            children = OrderByName(children, _currentUser);

            foreach (Folder folder in children)
            {

                var currentFolder = folder;

                var node = new TreeNode { Tag = currentFolder };

                var subChildren = isPhysical ? currentFolder.Children : currentFolder.GetChildren(_currentUser, true);
                subChildren = OrderByName(subChildren, _currentUser);
                AddChildren(node, subChildren, _currentUser, isPhysical);
                node.Text = currentFolder.Name + " (" +
                            node.Nodes.Count + ")";

                treeView1.Nodes.Add(node);
            }
        }

        /// <summary>
        /// Adds the children.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="children">The children.</param>
        /// <param name="user">The user.</param>
        private void AddChildren(TreeNode parent, IEnumerable<BaseItem> children, User user, bool isPhysical)
        {
            foreach (var item in children)
            {
                var node = new TreeNode { Tag = item };
                var subFolder = item as Folder;
                if (subFolder != null)
                {
                    var subChildren = isPhysical ? subFolder.Children : subFolder.GetChildren(_currentUser, true);

                    AddChildren(node, OrderBy(subChildren, user, ItemSortBy.SortName), user, isPhysical);
                    node.Text = item.Name + " (" + node.Nodes.Count + ")";
                }
                else
                {
                    node.Text = item.Name;
                }
                parent.Nodes.Add(node);
            }
        }

        /// <summary>
        /// Orders the name of the by.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> OrderByName(IEnumerable<BaseItem> items, User user)
        {
            return OrderBy(items, user, ItemSortBy.SortName);
        }

        /// <summary>
        /// Orders the name of the by.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> OrderBy(IEnumerable<BaseItem> items, User user, string order)
        {
            return _libraryManager.Sort(items, user, new[] { order }, SortOrder.Ascending);
        }

        /// <summary>
        /// The INDEN t_ STRING
        /// </summary>
        private const string INDENT_STRING = "    ";
        /// <summary>
        /// Formats the json.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>System.String.</returns>
        private static string FormatJson(string str)
        {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Class Extensions
    /// </summary>
    static class Extensions
    {
        /// <summary>
        /// Fors the each.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ie">The ie.</param>
        /// <param name="action">The action.</param>
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }
    }
}
