using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System;

namespace MediaBrowser.Controller.Providers.Movies
{
    [Export(typeof(BaseMetadataProvider))]
    public class MovieProviderFromXml : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Movie;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        protected override DateTime CompareDate(BaseEntity item)
        {
            var entry = item.ResolveArgs.GetFileSystemEntry(Path.Combine(item.Path, "movie.xml"));
            return entry != null ? entry.Value.LastWriteTimeUtc : DateTime.MinValue;
        }

        public override async Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            await Task.Run(() => Fetch(item, args)).ConfigureAwait(false);
        }

        private void Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            if (args.ContainsFile("movie.xml"))
            {
                new BaseItemXmlParser<Movie>().Fetch(item as Movie, Path.Combine(args.Path, "movie.xml"));
            }
        }
    }
}
