using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class PlaylistImages : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;

        public PlaylistImages(IServerConfigurationManager config)
        {
            _config = config;
        }

        public void Run()
        {
            if (!_config.Configuration.PlaylistImagesDeleted)
            {
                DeletePlaylistImages();
                _config.Configuration.PlaylistImagesDeleted = true;
                _config.SaveConfiguration();
            }
        }

        private void DeletePlaylistImages()
        {
            try
            {
                var path = Path.Combine(_config.ApplicationPaths.DataPath, "playlists");

                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .Where(i => BaseItem.SupportedImageExtensions.Contains(Path.GetExtension(i) ?? string.Empty))
                    .ToList();

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {

                    }
                }
            }
            catch (IOException)
            {

            }
        }
    }
}
