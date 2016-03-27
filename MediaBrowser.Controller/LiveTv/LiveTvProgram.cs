using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveTvProgram : BaseItem, IHasLookupInfo<LiveTvProgramLookupInfo>, IHasStartDate, IHasProgramAttributes
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateUserDataKey()
        {
            if (IsMovie)
            {
                var key = Movie.GetMovieUserDataKey(this);

                if (!string.IsNullOrWhiteSpace(key))
                {
                    return key;
                }
            }

            if (IsSeries && !string.IsNullOrWhiteSpace(EpisodeTitle))
            {
                var name = GetClientTypeName();

                return name + "-" + Name + (EpisodeTitle ?? string.Empty);
            }

            return base.CreateUserDataKey();
        }

        [IgnoreDataMember]
        public override SourceType SourceType
        {
            get { return SourceType.LiveTV; }
            set { }
        }

        /// <summary>
        /// The start date of the program, in UTC.
        /// </summary>
        [IgnoreDataMember]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is repeat.
        /// </summary>
        /// <value><c>true</c> if this instance is repeat; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsRepeat { get; set; }

        /// <summary>
        /// Gets or sets the episode title.
        /// </summary>
        /// <value>The episode title.</value>
        [IgnoreDataMember]
        public string EpisodeTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is movie.
        /// </summary>
        /// <value><c>true</c> if this instance is movie; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsMovie { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>true</c> if this instance is sports; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsSports { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is series.
        /// </summary>
        /// <value><c>true</c> if this instance is series; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is live.
        /// </summary>
        /// <value><c>true</c> if this instance is live; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsLive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is news.
        /// </summary>
        /// <value><c>true</c> if this instance is news; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsNews { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>true</c> if this instance is kids; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsKids { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is premiere.
        /// </summary>
        /// <value><c>true</c> if this instance is premiere; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsPremiere { get; set; }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
        [IgnoreDataMember]
        public override string ContainingFolderPath
        {
            get
            {
                return Path;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsOwnedItem
        {
            get
            {
                return false;
            }
        }

        //[IgnoreDataMember]
        //public override string MediaType
        //{
        //    get
        //    {
        //        return ChannelType == ChannelType.TV ? Model.Entities.MediaType.Video : Model.Entities.MediaType.Audio;
        //    }
        //}

        [IgnoreDataMember]
        public bool IsAiring
        {
            get
            {
                var now = DateTime.UtcNow;

                return now >= StartDate && now < EndDate;
            }
        }

        [IgnoreDataMember]
        public bool HasAired
        {
            get
            {
                var now = DateTime.UtcNow;

                return now >= EndDate;
            }
        }

        public override string GetClientTypeName()
        {
            return "Program";
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.LiveTvProgram;
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "livetv", Id.ToString("N"));
        }

        public override bool CanDelete()
        {
            return false;
        }

        public override bool IsInternetMetadataEnabled()
        {
            if (IsMovie)
            {
                var options = (LiveTvOptions)ConfigurationManager.GetConfiguration("livetv");
                return options.EnableMovieProviders;
            }

            return false;
        }

        public LiveTvProgramLookupInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<LiveTvProgramLookupInfo>();
            info.IsMovie = IsMovie; 
            return info;
        }

        [IgnoreDataMember]
        public override bool SupportsPeople
        {
            get
            {
                // Optimization
                if (IsNews || IsSports)
                {
                    return false;
                }

                return base.SupportsPeople;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsAncestors
        {
            get
            {
                return false;
            }
        }
    }
}
