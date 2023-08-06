#nullable disable

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
        private static readonly ItemFields[] _defaultExcludedFields = new[]
        {
            ItemFields.SeasonUserData,
            ItemFields.RefreshState
        };

        private static readonly ImageType[] _allImageTypes = Enum.GetValues<ImageType>();

        private static readonly ItemFields[] _allItemFields = Enum.GetValues<ItemFields>()
            .Except(_defaultExcludedFields)
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

            Fields = allFields ? _allItemFields : Array.Empty<ItemFields>();
            ImageTypes = _allImageTypes;
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
