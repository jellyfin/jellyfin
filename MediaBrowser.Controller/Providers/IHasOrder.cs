namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IHasOrder.
    /// </summary>
    public interface IHasOrder
    {
        /// <summary>
        /// Gets the order.
        /// </summary>
        /// <value>The order.</value>
        int Order { get; }
    }
}
