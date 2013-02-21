using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class UploadImageHandler
    /// </summary>
    [Export(typeof(IHttpServerHandler))]
    class UploadImageHandler : BaseActionHandler<Kernel>
    {
        /// <summary>
        /// The _source entity
        /// </summary>
        private BaseItem _sourceEntity;

        /// <summary>
        /// Gets the source entity.
        /// </summary>
        /// <returns>Task{BaseItem}.</returns>
        private async Task<BaseItem> GetSourceEntity()
        {
            if (_sourceEntity == null)
            {
                if (!string.IsNullOrEmpty(QueryString["personname"]))
                {
                    _sourceEntity =
                        await Kernel.LibraryManager.GetPerson(QueryString["personname"]).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["genre"]))
                {
                    _sourceEntity =
                        await Kernel.LibraryManager.GetGenre(QueryString["genre"]).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["year"]))
                {
                    _sourceEntity =
                        await
                        Kernel.LibraryManager.GetYear(int.Parse(QueryString["year"])).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["studio"]))
                {
                    _sourceEntity =
                        await Kernel.LibraryManager.GetStudio(QueryString["studio"]).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["userid"]))
                {
                    _sourceEntity = ApiService.GetUserById(QueryString["userid"]);
                }

                else
                {
                    _sourceEntity = DtoBuilder.GetItemByClientId(QueryString["id"]);
                }
            }

            return _sourceEntity;
        }

        /// <summary>
        /// Gets the type of the image.
        /// </summary>
        /// <value>The type of the image.</value>
        private ImageType ImageType
        {
            get
            {
                var imageType = QueryString["type"];

                return (ImageType)Enum.Parse(typeof(ImageType), imageType, true);
            }
        }

        /// <summary>
        /// Performs the action.
        /// </summary>
        /// <returns>Task.</returns>
        protected override async Task ExecuteAction()
        {
            var entity = await GetSourceEntity().ConfigureAwait(false);

            using (var reader = new StreamReader(HttpListenerContext.Request.InputStream))
            {
                var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                var bytes = Convert.FromBase64String(text);

                string filename;

                switch (ImageType)
                {
                    case ImageType.Art:
                        filename = "clearart";
                        break;
                    case ImageType.Primary:
                        filename = "folder";
                        break;
                    default:
                        filename = ImageType.ToString().ToLower();
                        break;
                }

                // Use the client filename to determine the original extension
                var clientFileName = QueryString["filename"];

                var oldImagePath = entity.GetImage(ImageType);

                var imagePath = Path.Combine(entity.MetaLocation, filename + Path.GetExtension(clientFileName));

                // Save to file system
                using (var fs = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }

                // Set the image
                entity.SetImage(ImageType, imagePath);

                // If the new and old paths are different, delete the old one
                if (!string.IsNullOrEmpty(oldImagePath) && !oldImagePath.Equals(imagePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(oldImagePath);
                }

                // Directory watchers should repeat this, but do a quick refresh first
                await entity.RefreshMetadata(CancellationToken.None, forceSave: true, allowSlowProviders: false).ConfigureAwait(false);
            }
        }
    }
}
