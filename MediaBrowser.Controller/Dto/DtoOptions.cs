using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Dto
{
    public class DtoOptions
    {
        private static readonly List<ItemFields> DefaultExcludedFields = new List<ItemFields>
        {
            ItemFields.SeasonUserData,
            ItemFields.RefreshState
        };

        public List<ItemFields> Fields { get; set; }
        public List<ImageType> ImageTypes { get; set; }
        public int ImageTypeLimit { get; set; }
        public bool EnableImages { get; set; }
        public bool AddProgramRecordingInfo { get; set; }
        public string DeviceId { get; set; }
        public bool EnableUserData { get; set; }
        public bool AddCurrentProgram { get; set; }

        public DtoOptions()
            : this(true)
        {
        }

        public DtoOptions(bool allFields)
        {
            ImageTypeLimit = int.MaxValue;
            EnableImages = true;
            EnableUserData = true;
            AddCurrentProgram = true;

            if (allFields)
            {
                Fields = Enum.GetNames(typeof(ItemFields))
                        .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                        .Except(DefaultExcludedFields)
                        .ToList();
            }
            else
            {
                Fields = new List<ItemFields>();
            }

            ImageTypes = Enum.GetNames(typeof(ImageType))
                .Select(i => (ImageType)Enum.Parse(typeof(ImageType), i, true))
                .ToList();
        }

        public int GetImageLimit(ImageType type)
        {
            if (EnableImages && ImageTypes.Contains(type))
            {
                return ImageTypeLimit;
            }

            return 0;
        }
    }
}
