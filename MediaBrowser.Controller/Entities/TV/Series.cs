using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Series
    /// </summary>
    public class Series : Folder
    {
        public List<Guid> SpecialFeatureIds { get; set; }

        public int SeasonCount { get; set; }

        public Series()
        {
            AirDays = new List<DayOfWeek>();

            SpecialFeatureIds = new List<Guid>();
        }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public SeriesStatus? Status { get; set; }
        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        public List<DayOfWeek> AirDays { get; set; }
        /// <summary>
        /// Gets or sets the air time.
        /// </summary>
        /// <value>The air time.</value>
        public string AirTime { get; set; }

        /// <summary>
        /// Series aren't included directly in indices - Their Episodes will roll up to them
        /// </summary>
        /// <value><c>true</c> if [include in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IncludeInIndex
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return this.GetProviderId(MetadataProviders.Tvdb) ?? this.GetProviderId(MetadataProviders.Tvcom) ?? base.GetUserDataKey();
        }

        // Studio, Genre and Rating will all be the same so makes no sense to index by these
        protected override Dictionary<string, Func<User, IEnumerable<BaseItem>>> GetIndexByOptions()
        {
            return new Dictionary<string, Func<User, IEnumerable<BaseItem>>> {            
                {LocalizedStrings.Instance.GetString("NoneDispPref"), null}, 
                {LocalizedStrings.Instance.GetString("PerformerDispPref"), GetIndexByPerformer},
                {LocalizedStrings.Instance.GetString("DirectorDispPref"), GetIndexByDirector},
                {LocalizedStrings.Instance.GetString("YearDispPref"), GetIndexByYear},
            };
        }

        /// <summary>
        /// Creates ResolveArgs on demand
        /// </summary>
        /// <param name="pathInfo">The path info.</param>
        /// <returns>ItemResolveArgs.</returns>
        protected internal override ItemResolveArgs CreateResolveArgs(FileSystemInfo pathInfo = null)
        {
            var args = base.CreateResolveArgs(pathInfo);

            Season.AddMetadataFiles(args);

            return args;
        }
    }
}
