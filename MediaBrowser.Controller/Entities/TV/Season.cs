using System.Collections.Generic;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Entities;
using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Season
    /// </summary>
    public class Season : Folder
    {

        /// <summary>
        /// Seasons are just containers
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
        /// We want to group into our Series
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get
            {
                return Series;
            }
        }

        // Genre, Rating and Stuido will all be the same
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
        /// Override to use the provider Ids + season number so it will be portable
        /// </summary>
        /// <value>The user data id.</value>
        [IgnoreDataMember]
        public override Guid UserDataId
        {
            get
            {
                if (_userDataId == Guid.Empty)
                {
                    var baseId = Series != null ? Series.GetProviderId(MetadataProviders.Tvdb) ?? Series.GetProviderId(MetadataProviders.Tvcom) : null;
                    if (baseId != null)
                    {
                        var seasonNo = IndexNumber ?? 0;
                        baseId =  baseId + seasonNo.ToString("000");
                    }
        
                    _userDataId = baseId != null ? baseId.GetMD5() : Id;
                }
                return _userDataId;
            }
        }

        /// <summary>
        /// We persist the MB Id of our series object so we can always find it no matter
        /// what context we happen to be loaded from.
        /// </summary>
        /// <value>The series item id.</value>
        public Guid SeriesItemId { get; set; }

        /// <summary>
        /// The _series
        /// </summary>
        private Series _series;
        /// <summary>
        /// This Episode's Series Instance
        /// </summary>
        /// <value>The series.</value>
        [IgnoreDataMember]
        public Series Series
        {
            get { return _series ?? (_series = FindParent<Series>()); }
        }

        /// <summary>
        /// Add files from the metadata folder to ResolveArgs
        /// </summary>
        /// <param name="args">The args.</param>
        public static void AddMetadataFiles(ItemResolveArgs args)
        {
            var folder = args.GetFileSystemEntryByName("metadata");

            if (folder.HasValue)
            {
                args.AddMetadataFiles(FileSystem.GetFiles(folder.Value.Path));
            }
        }

        /// <summary>
        /// Creates ResolveArgs on demand
        /// </summary>
        /// <param name="pathInfo">The path info.</param>
        /// <returns>ItemResolveArgs.</returns>
        protected internal override ItemResolveArgs CreateResolveArgs(WIN32_FIND_DATA? pathInfo = null)
        {
            var args = base.CreateResolveArgs(pathInfo);

            AddMetadataFiles(args);

            return args;
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return IndexNumber != null ? IndexNumber.Value.ToString("0000") : Name;
        }
    }
}
