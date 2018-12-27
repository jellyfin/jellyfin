using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Linq;

namespace MediaBrowser.Controller.Dto
{
    public class DtoOptions
    {
        private static readonly ItemFields[] DefaultExcludedFields = new []
        {
            ItemFields.SeasonUserData,
            ItemFields.RefreshState
        };

        public ItemFields[] Fields { get; set; }
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

        private static readonly ItemFields[] AllItemFields = Enum.GetNames(typeof(ItemFields))
            .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
            .Except(DefaultExcludedFields)
            .ToArray();

        public bool ContainsField(ItemFields field)
        {
            return AllItemFields.Contains(field);
        }

        public DtoOptions(bool allFields)
        {
            ImageTypeLimit = int.MaxValue;
            EnableImages = true;
            EnableUserData = true;
            AddCurrentProgram = true;

            if (allFields)
            {
                Fields = AllItemFields;
            }
            else
            {
                Fields = new ItemFields[] { };
            }

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
