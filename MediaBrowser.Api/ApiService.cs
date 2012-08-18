using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Contains some helpers for the api
    /// </summary>
    public static class ApiService
    {
        public static BaseItem GetItemById(string id)
        {
            Guid guid = string.IsNullOrEmpty(id) ? Guid.Empty : new Guid(id);

            return Kernel.Instance.GetItemById(guid);
        }

        public static DTOBaseItem GetDTOBaseItem(BaseItem item, User user, 
            bool includeChildren = true, 
            bool includePeople = true)
        {
            DTOBaseItem dto = new DTOBaseItem();

            dto.AspectRatio = item.AspectRatio;
            dto.BackdropCount = item.BackdropImagePaths == null ? 0 : item.BackdropImagePaths.Count();
            dto.DateCreated = item.DateCreated;
            dto.DisplayMediaType = item.DisplayMediaType;
            dto.Genres = item.Genres;
            dto.HasArt = !string.IsNullOrEmpty(item.ArtImagePath);
            dto.HasBanner = !string.IsNullOrEmpty(item.BannerImagePath);
            dto.HasLogo = !string.IsNullOrEmpty(item.LogoImagePath);
            dto.HasPrimaryImage = !string.IsNullOrEmpty(item.LogoImagePath);
            dto.HasThumb = !string.IsNullOrEmpty(item.ThumbnailImagePath);
            dto.Id = item.Id;
            dto.IndexNumber = item.IndexNumber;
            dto.IsFolder = item is Folder;
            dto.LocalTrailerCount = item.LocalTrailers == null ? 0 : item.LocalTrailers.Count();
            dto.Name = item.Name;
            dto.OfficialRating = item.OfficialRating;
            dto.Overview = item.Overview;

            // If there are no backdrops, indicate what parent has them in case the UI wants to allow inheritance
            if (dto.BackdropCount == 0)
            {
                int backdropCount;
                dto.ParentBackdropItemId = GetParentBackdropItemId(item, out backdropCount);
                dto.ParentBackdropCount = backdropCount;
            }

            if (item.Parent != null)
            {
                dto.ParentId = item.Parent.Id;
            }

            // If there is no logo, indicate what parent has one in case the UI wants to allow inheritance
            if (!dto.HasLogo)
            {
                dto.ParentLogoItemId = GetParentLogoItemId(item);
            }

            dto.Path = item.Path;

            dto.PremiereDate = item.PremiereDate;
            dto.ProductionYear = item.ProductionYear;
            dto.ProviderIds = item.ProviderIds;
            dto.RunTimeTicks = item.RunTimeTicks;
            dto.SortName = item.SortName;
            dto.Taglines = item.Taglines;
            dto.TrailerUrl = item.TrailerUrl;
            dto.Type = item.GetType().Name;
            dto.UserRating = item.UserRating;

            dto.UserData = item.GetUserData(user);

            AttachStudios(dto, item);

            if (includeChildren)
            {
                AttachChildren(dto, item, user);
            }

            if (includePeople)
            {
                AttachPeople(dto, item);
            }

            Folder folder = item as Folder;

            if (folder != null)
            {
                dto.SpecialCounts = folder.GetSpecialCounts(user);

                dto.IsRoot = folder.IsRoot;
                dto.IsVirtualFolder = folder is VirtualFolder;
            }
            
            return dto;
        }

        private static void AttachStudios(DTOBaseItem dto, BaseItem item)
        {
            // Attach Studios by transforming them into BaseItemStudio (DTO)
            if (item.Studios != null)
            {
                dto.Studios = item.Studios.Select(s =>
                {
                    BaseItemStudio baseItemStudio = new BaseItemStudio();

                    baseItemStudio.Name = s;

                    Studio ibnObject = Kernel.Instance.ItemController.GetStudio(s);

                    if (ibnObject != null)
                    {
                        baseItemStudio.HasImage = !string.IsNullOrEmpty(ibnObject.PrimaryImagePath);
                    }

                    return baseItemStudio;
                });
            }
        }

        private static void AttachChildren(DTOBaseItem dto, BaseItem item, User user)
        {
            var folder = item as Folder;

            if (folder != null)
            {
                dto.Children = folder.GetParentalAllowedChildren(user).Select(c => GetDTOBaseItem(c, user, false, false));
            }

            dto.LocalTrailers = item.LocalTrailers;
        }

        private static void AttachPeople(DTOBaseItem dto, BaseItem item)
        {
            // Attach People by transforming them into BaseItemPerson (DTO)
            if (item.People != null)
            {
                dto.People = item.People.Select(p =>
                {
                    BaseItemPerson baseItemPerson = new BaseItemPerson();

                    baseItemPerson.PersonInfo = p;

                    Person ibnObject = Kernel.Instance.ItemController.GetPerson(p.Name);

                    if (ibnObject != null)
                    {
                        baseItemPerson.HasImage = !string.IsNullOrEmpty(ibnObject.PrimaryImagePath);
                    }

                    return baseItemPerson;
                });
            }
        }

        private static Guid? GetParentBackdropItemId(BaseItem item, out int backdropCount)
        {
            backdropCount = 0;

            var parent = item.Parent;

            while (parent != null)
            {
                if (parent.BackdropImagePaths != null && parent.BackdropImagePaths.Any())
                {
                    backdropCount = parent.BackdropImagePaths.Count();
                    return parent.Id;
                }

                parent = parent.Parent;
            }

            return null;
        }

        private static Guid? GetParentLogoItemId(BaseItem item)
        {
            var parent = item.Parent;

            while (parent != null)
            {
                if (!string.IsNullOrEmpty(parent.LogoImagePath))
                {
                    return parent.Id;
                }

                parent = parent.Parent;
            }

            return null;
        }

        private static string _FFMpegDirectory = null;
        /// <summary>
        /// Gets the folder path to ffmpeg
        /// </summary>
        public static string FFMpegDirectory
        {
            get
            {
                if (_FFMpegDirectory == null)
                {
                    _FFMpegDirectory = System.IO.Path.Combine(Kernel.Instance.ApplicationPaths.ProgramDataPath, "ffmpeg");

                    if (!Directory.Exists(_FFMpegDirectory))
                    {
                        Directory.CreateDirectory(_FFMpegDirectory);
                    }
                }

                return _FFMpegDirectory;
            }
        }

        private static string _FFMpegPath = null;
        /// <summary>
        /// Gets the path to ffmpeg.exe
        /// </summary>
        public static string FFMpegPath
        {
            get
            {
                if (_FFMpegPath == null)
                {
                    string filename = "ffmpeg.exe";

                    _FFMpegPath = Path.Combine(FFMpegDirectory, filename);

                    // Always re-extract the first time to handle new versions
                    if (File.Exists(_FFMpegPath))
                    {
                        File.Delete(_FFMpegPath);
                    }

                    // Extract ffprobe
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.Api.FFMpeg." + filename))
                    {
                        using (FileStream fileStream = new FileStream(_FFMpegPath, FileMode.Create))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }

                return _FFMpegPath;
            }
        }
    }
}
