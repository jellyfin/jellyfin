using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Trailer
    /// </summary>
    public class Trailer : Video
    {
        /// <summary>
        /// Gets a value indicating whether this instance is local trailer.
        /// </summary>
        /// <value><c>true</c> if this instance is local trailer; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsLocalTrailer
        {
            get
            {
                // Local trailers are not part of children
                return Parent == null;
            }
        }

        /// <summary>
        /// Should be overridden to return the proper folder where metadata lives
        /// </summary>
        /// <value>The meta location.</value>
        [IgnoreDataMember]
        public override string MetaLocation
        {
            get
            {
                if (!IsLocalTrailer)
                {
                    return System.IO.Path.GetDirectoryName(Path);
                }

                return base.MetaLocation;
            }
        }

        /// <summary>
        /// Needed because the resolver stops at the trailer folder and we find the video inside.
        /// </summary>
        /// <value><c>true</c> if [use parent path to create resolve args]; otherwise, <c>false</c>.</value>
        protected override bool UseParentPathToCreateResolveArgs
        {
            get { return !IsLocalTrailer; }
        }
    }
}
