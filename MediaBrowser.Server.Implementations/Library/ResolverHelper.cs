using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class ResolverHelper
    /// </summary>
    public static class ResolverHelper
    {
        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        /// <param name="fileSystem">The file system.</param>
        public static void SetInitialItemValues(BaseItem item, ItemResolveArgs args, IFileSystem fileSystem)
        {
            // If the resolver didn't specify this
            if (string.IsNullOrEmpty(item.Path))
            {
                item.Path = args.Path;
            }

            // If the resolver didn't specify this
            if (args.Parent != null)
            {
                item.Parent = args.Parent;
            }

            item.Id = item.Path.GetMBId(item.GetType());

            // If the resolver didn't specify this
            if (string.IsNullOrEmpty(item.DisplayMediaType))
            {
                item.DisplayMediaType = item.GetType().Name;
            }

            // Make sure the item has a name
            EnsureName(item, args);

            item.IsLocked = item.Path.IndexOf("[dontfetchmeta]", StringComparison.OrdinalIgnoreCase) != -1 ||
                item.Parents.Any(i => i.IsLocked);

            // Make sure DateCreated and DateModified have values
            EntityResolutionHelper.EnsureDates(fileSystem, item, args, true);
        }

        /// <summary>
        /// Ensures the name.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The arguments.</param>
        private static void EnsureName(BaseItem item, ItemResolveArgs args)
        {
            // If the subclass didn't supply a name, add it here
            if (string.IsNullOrEmpty(item.Name) && !string.IsNullOrEmpty(item.Path))
            {
                //we use our resolve args name here to get the name of the containg folder, not actual video file
                item.Name = GetDisplayName(args.FileInfo.Name, (args.FileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
            }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">if set to <c>true</c> [is directory].</param>
        /// <returns>System.String.</returns>
        private static string GetDisplayName(string path, bool isDirectory)
        {
            //first just get the file or directory name
            var fn = isDirectory ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);

            return fn;
        }

        /// <summary>
        /// The MB name regex
        /// </summary>
        private static readonly Regex MbNameRegex = new Regex(@"(\[.*?\])", RegexOptions.Compiled);

        internal static string StripBrackets(string inputString)
        {
            var output = MbNameRegex.Replace(inputString, string.Empty).Trim();
            return Regex.Replace(output, @"\s+", " ");
        }
    }
}
