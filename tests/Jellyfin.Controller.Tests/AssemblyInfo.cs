using Xunit;

// BaseItem reaches its collaborators through static properties (LibraryManager, ItemRepository,
// FileSystem). Every test class here that builds an item either sets or reads them, so running two
// classes at once lets one class' mocks answer another class' calls.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
