using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Library;
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
        private readonly IUserViewManager _userViewManager;

        private User _currentUser;

        public LibraryViewer(IJsonSerializer jsonSerializer, IUserManager userManager, ILibraryManager libraryManager, IUserViewManager userViewManager)
        {
            InitializeComponent();

            _jsonSerializer = jsonSerializer;
            _libraryManager = libraryManager;
            _userViewManager = userViewManager;

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
                lblType.Text = item.GetType().Name;

                var json = FormatJson(_jsonSerializer.SerializeToString(item));

                var hasMediaSources = item as IHasMediaSources;
                if (hasMediaSources != null)
                {
                    var sources = hasMediaSources.GetMediaSources(false).ToList();

                    json += "\n\nMedia Sources:\n\n" + FormatJson(_jsonSerializer.SerializeToString(sources));
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

        private IEnumerable<BaseItem> GetItems(Folder parent, User user)
        {
            if (parent == null)
            {
                var task = _userViewManager.GetUserViews(new UserViewQuery
                {
                    UserId = user.Id.ToString("N")

                }, CancellationToken.None);

                task.RunSynchronously();

                return task.Result;
            }
            else
            {
                var task = parent.GetUserItems(new UserItemsQuery
                {
                    User = user,
                    SortBy = new[] { ItemSortBy.SortName }

                });

                task.RunSynchronously();

                return task.Result.Items;
            }
        }

        private void LoadTree()
        {
            treeView1.Nodes.Clear();

            var isPhysical = _currentUser.Name == "Physical";
            IEnumerable<BaseItem> children = isPhysical ? new[] { _libraryManager.RootFolder } : GetItems(null, _currentUser);

            foreach (var folder in children.OfType<Folder>())
            {
                var currentFolder = folder;

                var node = new TreeNode { Tag = currentFolder };

                var subChildren = isPhysical ? currentFolder.Children.OrderBy(i => i.SortName) : GetItems(currentFolder, _currentUser);

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
                    var subChildren = isPhysical ? subFolder.Children.OrderBy(i => i.SortName) : GetItems(subFolder, _currentUser);

                    AddChildren(node, subChildren, user, isPhysical);
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
