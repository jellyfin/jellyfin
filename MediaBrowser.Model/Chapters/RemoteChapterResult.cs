
namespace MediaBrowser.Model.Chapters
{
    public class RemoteChapterResult
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the provider.
        /// </summary>
        /// <value>The name of the provider.</value>
        public string ProviderName { get; set; }
        
        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        public float? CommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the chapter count.
        /// </summary>
        /// <value>The chapter count.</value>
        public int? ChapterCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the three letter iso language.
        /// </summary>
        /// <value>The name of the three letter iso language.</value>
        public string ThreeLetterISOLanguageName { get; set; }
    }
}
