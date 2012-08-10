using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Api.Transcoding;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Contains some helpers for the api
    /// </summary>
    public static class ApiService
    {
        /// <summary>
        /// Holds the list of active transcoding jobs
        /// </summary>
        private static List<TranscodingJob> CurrentTranscodingJobs = new List<TranscodingJob>();

        /// <summary>
        /// Finds an active transcoding job
        /// </summary>
        public static TranscodingJob GetTranscodingJob(string outputPath)
        {
            lock (CurrentTranscodingJobs)
            {
                return CurrentTranscodingJobs.FirstOrDefault(j => j.OutputFile.Equals(outputPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Removes a transcoding job from the active list
        /// </summary>
        public static void RemoveTranscodingJob(TranscodingJob job)
        {
            lock (CurrentTranscodingJobs)
            {
                CurrentTranscodingJobs.Remove(job);
            }
        }

        /// <summary>
        /// Adds a transcoding job to the active list
        /// </summary>
        public static void AddTranscodingJob(TranscodingJob job)
        {
            lock (CurrentTranscodingJobs)
            {
                CurrentTranscodingJobs.Add(job);
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
