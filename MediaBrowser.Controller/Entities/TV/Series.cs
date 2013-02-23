using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Series
    /// </summary>
    public class Series : Folder
    {
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
        /// Override to use the provider Ids so it will be portable
        /// </summary>
        /// <value>The user data id.</value>
        [IgnoreDataMember]
        public override Guid UserDataId
        {
            get
            {
                if (_userDataId == Guid.Empty)
                {
                    var baseId = this.GetProviderId(MetadataProviders.Tvdb) ?? this.GetProviderId(MetadataProviders.Tvcom);
                    _userDataId = baseId != null ? baseId.GetMD5() : Id;
                }
                return _userDataId;
            }
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
        protected internal override ItemResolveArgs CreateResolveArgs(WIN32_FIND_DATA? pathInfo = null)
        {
            var args = base.CreateResolveArgs(pathInfo);

            Season.AddMetadataFiles(args);

            return args;
        }
    }
}
