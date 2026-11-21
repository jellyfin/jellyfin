namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// The 3D video layout format.
    /// </summary>
    public enum Video3DFormat
    {
        /// <summary>
        /// Half side-by-side format.
        /// </summary>
        HalfSideBySide,

        /// <summary>
        /// Full side-by-side format.
        /// </summary>
        FullSideBySide,

        /// <summary>
        /// Full top-and-bottom format.
        /// </summary>
        FullTopAndBottom,

        /// <summary>
        /// Half top-and-bottom format.
        /// </summary>
        HalfTopAndBottom,

        /// <summary>
        /// Multiview Video Coding format.
        /// </summary>
        MVC
    }
}
