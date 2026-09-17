using Xunit;

// BaseItem reaches its collaborators through static properties (LibraryManager, ItemRepository,
// FileSystem). Every test class here that builds an item either sets or reads them, so running two
// classes at once lets one class' mocks answer another class' calls: the symptoms are a strict-mock
// MockException, a NullReferenceException out of a static getter, or an assertion that sees an item
// belonging to some other test. Serialising the assembly is cheap - it is under a second - and it
// keeps a newly added class from silently reintroducing the race.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
