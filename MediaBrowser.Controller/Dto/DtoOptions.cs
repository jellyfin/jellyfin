using System;
using System.Linq;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Dto
{
    public class DtoOptions
    {
        private static readonly ItemField[] DefaultExcludedFields = new[]
        {
            ItemField.SeasonUserData,
            ItemField.RefreshState
        };

        public ItemField[] Fields { get; set; }
        public ImageType[] ImageTypes { get; set; }
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

        private static readonly ItemField[] AllItemField = Enum.GetNames(typeof(ItemField))
            .Select(i => (ItemField)Enum.Parse(typeof(ItemField), i, true))
            .Except(DefaultExcludedFields)
            .ToArray();

        public bool ContainsField(ItemField field)
            => Fields.Contains(field);

        public DtoOptions(bool allFields)
        {
            ImageTypeLimit = int.MaxValue;
            EnableImages = true;
            EnableUserData = true;
            AddCurrentProgram = true;

            Fields = allFields ? AllItemField : Array.Empty<ItemField>();
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
