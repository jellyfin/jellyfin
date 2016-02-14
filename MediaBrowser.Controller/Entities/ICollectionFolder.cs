using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// This is just a marker interface to denote top level folders
    /// </summary>
    public interface ICollectionFolder
    {
        string CollectionType { get; }
        string Path { get; }
        string Name { get; }
        Guid Id { get; }
        IEnumerable<string> PhysicalLocations { get; }
    }

    public interface ISupportsUserSpecificView
    {
        bool EnableUserSpecificView { get; }
    }

    public static class CollectionFolderExtensions
    {
        public static string GetViewType(this ICollectionFolder folder, User user)
        {
            if (user.Configuration.PlainFolderViews.Contains(folder.Id.ToString("N"), StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            return folder.CollectionType;
        }
    }
}
