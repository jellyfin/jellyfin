using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/Videos/{Id}/AdditionalParts", "GET")]
    [Api(Description = "Gets additional parts for a video.")]
    public class GetAdditionalParts : IReturn<ItemsResult>
    {
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Videos/{Id}/Versions", "GET")]
    [Api(Description = "Gets all versions of a video.")]
    public class GetMediaVersions : IReturn<List<MediaVersionInfo>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Videos/{Id}/AlternateVersions", "DELETE")]
    [Api(Description = "Assigns videos as alternates of antoher.")]
    public class DeleteAlternateVersions : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Videos/MergeVersions", "POST")]
    [Api(Description = "Merges videos into a single record")]
    public class MergeVersions : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id list. This allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }
    }

    public class VideosService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly IFileSystem _fileSystem;
        private readonly IItemRepository _itemRepo;
        private readonly IServerConfigurationManager _config;

        public VideosService(ILibraryManager libraryManager, IUserManager userManager, IDtoService dtoService, IItemRepository itemRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _itemRepo = itemRepo;
            _fileSystem = fileSystem;
            _config = config;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAdditionalParts request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (request.UserId.HasValue
                                  ? user.RootFolder
                                  : _libraryManager.RootFolder)
                           : _dtoService.GetItemByDtoId(request.Id, request.UserId);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var video = (Video)item;

            var items = video.GetAdditionalParts()
                         .Select(i => _dtoService.GetBaseItemDto(i, fields, user, video))
                         .ToArray();

            var result = new ItemsResult
            {
                Items = items,
                TotalRecordCount = items.Length
            };

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public object Get(GetMediaVersions request)
        {
            var item = _libraryManager.GetItemById(new Guid(request.Id));

            var video = (Video)item;

            var result = video.GetAlternateVersions().Select(GetVersionInfo).ToList();

            result.Add(GetVersionInfo(video));

            result = result.OrderBy(i =>
            {
                if (video.VideoType == VideoType.VideoFile)
                {
                    return 0;
                }

                return 1;

            }).ThenBy(i => i.Video3DFormat.HasValue ? 1 : 0)
            .ThenByDescending(i =>
            {
                var stream = i.MediaStreams.FirstOrDefault(m => m.Type == MediaStreamType.Video);

                return stream == null || stream.Width == null ? 0 : stream.Width.Value;
            })
            .ToList();

            return ToOptimizedSerializedResultUsingCache(result);
        }

        private MediaVersionInfo GetVersionInfo(Video i)
        {
            return new MediaVersionInfo
            {
                Chapters = _itemRepo.GetChapters(i.Id).Select(c => _dtoService.GetChapterInfoDto(c, i)).ToList(),

                Id = i.Id.ToString("N"),
                IsoType = i.IsoType,
                LocationType = i.LocationType,
                MediaStreams = _itemRepo.GetMediaStreams(new MediaStreamQuery {ItemId = i.Id}).ToList(),
                Name = GetAlternateVersionName(i),
                Path = GetMappedPath(i),
                RunTimeTicks = i.RunTimeTicks,
                Video3DFormat = i.Video3DFormat,
                VideoType = i.VideoType
            };
        }

        private string GetMappedPath(Video video)
        {
            var path = video.Path;

            var locationType = video.LocationType;

            if (locationType != LocationType.FileSystem && locationType != LocationType.Offline)
            {
                return path;
            }

            foreach (var map in _config.Configuration.PathSubstitutions)
            {
                path = _fileSystem.SubstitutePath(path, map.From, map.To);
            }

            return path;
        }

        private string GetAlternateVersionName(Video video)
        {
            var name = "";

            var stream = video.GetDefaultVideoStream();

            if (video.Video3DFormat.HasValue)
            {
                name = "3D " + name;
                name = name.Trim();
            }

            if (video.VideoType == VideoType.BluRay)
            {
                name = name + " " + "Bluray";
                name = name.Trim();
            }
            else if (video.VideoType == VideoType.Dvd)
            {
                name = name + " " + "DVD";
                name = name.Trim();
            }
            else if (video.VideoType == VideoType.HdDvd)
            {
                name = name + " " + "HD-DVD";
                name = name.Trim();
            }
            else if (video.VideoType == VideoType.Iso)
            {
                if (video.IsoType.HasValue)
                {
                    if (video.IsoType.Value == IsoType.BluRay)
                    {
                        name = name + " " + "Bluray";
                    }
                    else if (video.IsoType.Value == IsoType.Dvd)
                    {
                        name = name + " " + "DVD";
                    }
                }
                else
                {
                    name = name + " " + "ISO";
                }
                name = name.Trim();
            }
            else if (video.VideoType == VideoType.VideoFile)
            {
                if (stream != null)
                {
                    if (stream.Width.HasValue)
                    {
                        if (stream.Width.Value >= 3800)
                        {
                            name = name + " " + "4K";
                            name = name.Trim();
                        }
                        else if (stream.Width.Value >= 1900)
                        {
                            name = name + " " + "1080P";
                            name = name.Trim();
                        }
                        else if (stream.Width.Value >= 1270)
                        {
                            name = name + " " + "720P";
                            name = name.Trim();
                        }
                        else if (stream.Width.Value >= 700)
                        {
                            name = name + " " + "480p";
                            name = name.Trim();
                        }
                        else
                        {
                            name = name + " " + "SD";
                            name = name.Trim();
                        }
                    }
                }
            }

            if (stream != null && !string.IsNullOrWhiteSpace(stream.Codec))
            {
                name = name + " " + stream.Codec.ToUpper();
                name = name.Trim();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return video.Name;
            }

            return name;
        }

        public void Delete(DeleteAlternateVersions request)
        {
            var task = RemoveAlternateVersions(request);

            Task.WaitAll(task);
        }

        private async Task RemoveAlternateVersions(DeleteAlternateVersions request)
        {
            var video = (Video)_dtoService.GetItemByDtoId(request.Id);

            foreach (var link in video.GetLinkedAlternateVersions())
            {
                link.PrimaryVersionId = null;

                await link.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            }

            video.LinkedAlternateVersions.Clear();
            await video.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        public void Post(MergeVersions request)
        {
            var task = MergeVersions(request);

            Task.WaitAll(task);
        }

        private async Task MergeVersions(MergeVersions request)
        {
            var items = request.Ids.Split(',')
                .Select(i => new Guid(i))
                .Select(i => _libraryManager.GetItemById(i))
                .ToList();

            if (items.Count < 2)
            {
                throw new ArgumentException("Please supply at least two videos to merge.");
            }

            if (items.Any(i => !(i is Video)))
            {
                throw new ArgumentException("Only videos can be grouped together.");
            }

            var videos = items.Cast<Video>().ToList();

            var videosWithVersions = videos.Where(i => i.AlternateVersionCount > 0)
                .ToList();

            if (videosWithVersions.Count > 1)
            {
                throw new ArgumentException("Videos with sub-versions cannot be merged.");
            }

            var primaryVersion = videosWithVersions.FirstOrDefault();

            if (primaryVersion == null)
            {
                primaryVersion = videos.OrderByDescending(i =>
                {
                    var stream = i.GetDefaultVideoStream();

                    return stream == null || stream.Width == null ? 0 : stream.Width.Value;

                }).ThenBy(i => i.Name.Length)
                    .First();
            }

            foreach (var item in videos.Where(i => i.Id != primaryVersion.Id))
            {
                item.PrimaryVersionId = primaryVersion.Id;

                await item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

                primaryVersion.LinkedAlternateVersions.Add(new LinkedChild
                {
                    Path = item.Path,
                    ItemId = item.Id
                });
            }

            await primaryVersion.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
