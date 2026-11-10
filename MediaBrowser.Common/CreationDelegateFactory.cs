namespace MediaBrowser.Common
{
    /// <summary>
    /// Delegate used with GetExports{T}.
    /// </summary>
    /// <param name="type">Type to create.</param>
    /// <returns>New instance of type <param>type</param>.</returns>
    public delegate object? CreationDelegateFactory(System.Type type);
}
