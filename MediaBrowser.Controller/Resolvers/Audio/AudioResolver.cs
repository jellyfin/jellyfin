using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Resolvers.Audio
{
    /// <summary>
    /// Class AudioResolver
    /// </summary>
    public class AudioResolver : BaseItemResolver<Entities.Audio.Audio>
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Last; }
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Entities.Audio.Audio.</returns>
        protected override Entities.Audio.Audio Resolve(ItemResolveArgs args)
        {
            // Return audio if the path is a file and has a matching extension

            if (!args.IsDirectory)
            {
                if (EntityResolutionHelper.IsAudioFile(args))
                {
                    return new Entities.Audio.Audio();
                }
            }

            return null;
        }
    }
}
