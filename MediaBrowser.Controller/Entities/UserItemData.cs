using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class UserItemData
    {
        private float? _rating;
        /// <summary>
        /// Gets or sets the users 0-10 rating
        /// </summary>
        public float? Rating
        {
            get
            {
                return _rating;
            }
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0 || value.Value > 10)
                    {
                        throw new InvalidOperationException("A 0-10 rating is required for UserItemData.");
                    }
                }

                _rating = value;
            }
        }

        public long PlaybackPositionTicks { get; set; }

        public int PlayCount { get; set; }

        public bool IsFavorite { get; set; }

        /// <summary>
        /// This is an interpreted property to indicate likes or dislikes
        /// This should never be serialized.
        /// </summary>
        [IgnoreDataMember]
        public bool? Likes
        {
            get
            {
                if (Rating != null)
                {
                    return Rating >= 6.5;
                }

                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    Rating = value.Value ? 10 : 1;
                }
                else
                {
                    Rating = null;
                }
            }
        }
    }
}
