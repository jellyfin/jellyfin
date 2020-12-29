#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Dto
{
    public class DtoOptions
    {
        private static readonly ItemFields[] DefaultExcludedFields = new[]
        {
            ItemFields.SeasonUserData,
            ItemFields.RefreshState
        };

        public IReadOnlyList<ItemFields> Fields { get; set; }

        public IReadOnlyList<ImageType> ImageTypes { get; set; }

        public int ImageTypeLimit { get; set; }

        public bool EnableImages { get; set; }

        public bool AddProgramRecordingInfo { get; set; }

        public bool EnableUserData { get; set; }

        public bool AddCurrentProgram { get; set; }

        public DtoOptions()
            : this(true)
        {
        }

        private static readonly ImageType[] AllImageTypes = Enum.GetNames(typeof(ImageType))
            .Select(i => (ImageType)Enum.Parse(typeof(ImageType), i, true))
            .ToArray();

        private static readonly ItemFields[] AllItemFields = Enum.GetNames(typeof(ItemFields))
            .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
            .Except(DefaultExcludedFields)
            .ToArray();

        public bool ContainsField(ItemFields field)
            => Fields.Contains(field);

        public DtoOptions(bool allFields)
        {
            ImageTypeLimit = int.MaxValue;
            EnableImages = true;
            EnableUserData = true;
            AddCurrentProgram = true;

            Fields = allFields ? AllItemFields : Array.Empty<ItemFields>();
            ImageTypes = AllImageTypes;
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
