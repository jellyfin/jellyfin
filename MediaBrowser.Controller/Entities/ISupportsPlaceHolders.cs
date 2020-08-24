#pragma warning disable CS1591

namespace MediaBrowser.Controller.Entities
{
    public interface ISupportsPlaceHolders
    {
        /// <summary>
        /// Gets a value indicating whether this instance is place holder.
        /// </summary>
        /// <value><c>true</c> if this instance is place holder; otherwise, <c>false</c>.</value>
        bool IsPlaceHolder { get; }
    }
}
