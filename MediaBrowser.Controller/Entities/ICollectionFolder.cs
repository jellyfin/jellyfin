using System;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// This is just a marker interface to denote top level folders
    /// </summary>
    public interface ICollectionFolder : IHasCollectionType
    {
        string Path { get; }

        string Name { get; }

        Guid Id { get; }

        string[] PhysicalLocations { get; }
    }

    public interface ISupportsUserSpecificView
    {
        bool EnableUserSpecificView { get; }
    }

    public interface IHasCollectionType
    {
        string CollectionType { get; }
    }
}
