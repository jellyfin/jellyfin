using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using System;
using System.IO;
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
            item.ResetResolveArgs(args);

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
            EnsureName(item);

            item.DontFetchMeta = item.Path.IndexOf("[dontfetchmeta]", StringComparison.OrdinalIgnoreCase) != -1;

            // Make sure DateCreated and DateModified have values
            EntityResolutionHelper.EnsureDates(fileSystem, item, args, true);
        }

        /// <summary>
        /// Ensures the name.
        /// </summary>
        /// <param name="item">The item.</param>
        private static void EnsureName(BaseItem item)
        {
            // If the subclass didn't supply a name, add it here
            if (string.IsNullOrEmpty(item.Name) && !string.IsNullOrEmpty(item.Path))
            {
                //we use our resolve args name here to get the name of the containg folder, not actual video file
                item.Name = GetMBName(item.ResolveArgs.FileInfo.Name, (item.ResolveArgs.FileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
            }
        }

        /// <summary>
        /// The MB name regex
        /// </summary>
        private static readonly Regex MBNameRegex = new Regex(@"(\[boxset\]|\[tmdbid=\d+\]|\[tvdbid=\d+\])", RegexOptions.Compiled);

        /// <summary>
        /// Strip out attribute items and return just the name we will use for items
        /// </summary>
        /// <param name="path">Assumed to be a file or directory path</param>
        /// <param name="isDirectory">if set to <c>true</c> [is directory].</param>
        /// <returns>The cleaned name</returns>
        private static string GetMBName(string path, bool isDirectory)
        {
            //first just get the file or directory name
            var fn = isDirectory ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);

            //now - strip out anything inside brackets
            fn = StripBrackets(fn);

            return fn;
        }

        public static string StripBrackets(string inputString) {
            var output = MBNameRegex.Replace(inputString, string.Empty).Trim();
            return Regex.Replace(output, @"\s+", " ");
        }

    }
}
