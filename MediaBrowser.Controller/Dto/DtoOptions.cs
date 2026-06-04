using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Dto
{
    /// <summary>
    /// Options that control which fields and images are populated when building a <see cref="MediaBrowser.Model.Dto.BaseItemDto"/>.
    /// </summary>
    public class DtoOptions
    {
        private static readonly ItemFields[] DefaultExcludedFields =
        [
            ItemFields.SeasonUserData,
            ItemFields.RefreshState
        ];

        private static readonly ImageType[] AllImageTypes = Enum.GetValues<ImageType>();

        private static readonly ItemFields[] AllItemFields = Enum.GetValues<ItemFields>()
            .Except(DefaultExcludedFields)
            .ToArray();

        /// <summary>
        /// Initializes a new instance of the <see cref="DtoOptions"/> class with all fields enabled.
        /// </summary>
        public DtoOptions()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DtoOptions"/> class.
        /// </summary>
        /// <param name="allFields">Whether to populate all available fields.</param>
        public DtoOptions(bool allFields)
        {
            ImageTypeLimit = int.MaxValue;
            EnableImages = true;
            EnableUserData = true;
            AddCurrentProgram = true;

            Fields = allFields ? AllItemFields : [];
            ImageTypes = AllImageTypes;
        }

        /// <summary>
        /// Gets or sets the fields to populate on the DTO.
        /// </summary>
        public IReadOnlyList<ItemFields> Fields { get; set; }

        /// <summary>
        /// Gets or sets the image types to populate on the DTO.
        /// </summary>
        public IReadOnlyList<ImageType> ImageTypes { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of images to return per image type.
        /// </summary>
        public int ImageTypeLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether image information is populated.
        /// </summary>
        public bool EnableImages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether program recording information is populated.
        /// </summary>
        public bool AddProgramRecordingInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user data is populated.
        /// </summary>
        public bool EnableUserData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the currently airing program is populated.
        /// </summary>
        public bool AddCurrentProgram { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an episode's portrait poster (its season's primary
        /// image, falling back to the series') should replace the episode's own (16:9) primary image.
        /// Used by views that render episodes as poster cards, e.g. "Latest".
        /// </summary>
        public bool PreferEpisodeParentPoster { get; set; }

        /// <summary>
        /// Gets a value indicating whether the specified field is populated.
        /// </summary>
        /// <param name="field">The field to check.</param>
        /// <returns><c>true</c> if the field is populated; otherwise, <c>false</c>.</returns>
        public bool ContainsField(ItemFields field)
            => Fields.Contains(field);

        /// <summary>
        /// Gets the number of images to return for the specified image type.
        /// </summary>
        /// <param name="type">The image type.</param>
        /// <returns>The image limit for the type, or 0 if the type is not enabled.</returns>
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
