using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Contains some helpers for the api
    /// </summary>
    public static class ApiService
    {
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
                    _FFMpegDirectory = System.IO.Path.Combine(ApplicationPaths.ProgramDataPath, "ffmpeg");

                    if (!Directory.Exists(_FFMpegDirectory))
                    {
                        Directory.CreateDirectory(_FFMpegDirectory);

                        // Extract ffmpeg
                        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.Api.ffmpeg.ffmpeg.exe"))
                        {
                            using (FileStream fileStream = new FileStream(FFMpegPath, FileMode.Create))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }
                }

                return _FFMpegDirectory;
            }
        }

        public static string FFMpegPath
        {
            get
            {
                return System.IO.Path.Combine(FFMpegDirectory, "ffmpeg.exe");
            }
        }

        public static BaseItem GetItemById(string id)
        {
            Guid guid = string.IsNullOrEmpty(id) ? Guid.Empty : new Guid(id);

            return Kernel.Instance.GetItemById(guid);
        }

        /// <summary>
        /// Takes a BaseItem and returns the actual object that will be serialized by the api
        /// </summary>
        public static ApiBaseItemWrapper<BaseItem> GetSerializationObject(BaseItem item, bool includeChildren, Guid userId)
        {
            ApiBaseItemWrapper<BaseItem> wrapper = new ApiBaseItemWrapper<BaseItem>()
            {
                Item = item,
                UserItemData = Kernel.Instance.GetUserItemData(userId, item.Id),
                Type = item.GetType().Name,
                IsFolder = (item is Folder)
            };

            if (string.IsNullOrEmpty(item.LogoImagePath))
            {
                wrapper.ParentLogoItemId = GetParentLogoItemId(item);
            }

            if (item.BackdropImagePaths == null || !item.BackdropImagePaths.Any())
            {
                int backdropCount;
                wrapper.ParentBackdropItemId = GetParentBackdropItemId(item, out backdropCount);
                wrapper.ParentBackdropCount = backdropCount;
            }

            if (item.Parent != null)
            {
                wrapper.ParentId = item.Parent.Id;
            }

            if (includeChildren)
            {
                var folder = item as Folder;

                if (folder != null)
                {
                    wrapper.Children = Kernel.Instance.GetParentalAllowedChildren(folder, userId).Select(c => GetSerializationObject(c, false, userId));
                }
            }

            return wrapper;
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
    }
}
