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

        private static readonly ImageType[] AllImageTypes = Enum.GetValues<ImageType>();

        private static readonly ItemFields[] AllItemFields = Enum.GetValues<ItemFields>()
            .Except(DefaultExcludedFields)
            .ToArray();

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

            Fields = allFields ? AllItemFields : Array.Empty<ItemFields>();
            ImageTypes = AllImageTypes;
        }

        public IReadOnlyList<ItemFields> Fields { get; set; }

        public IReadOnlyList<ImageType> ImageTypes { get; set; }

        public int ImageTypeLimit { get; set; }

        public bool EnableImages { get; set; }

        public bool AddProgramRecordingInfo { get; set; }

        public bool EnableUserData { get; set; }

        public bool AddCurrentProgram { get; set; }

        public bool ContainsField(ItemFields field)
            => Fields.Contains(field);

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
