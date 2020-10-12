using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Represents a type option object.
    /// </summary>
    public class TypeOptions
    {
        private static readonly Dictionary<string, ImageOption[]> DefaultImageOptions = new Dictionary<string, ImageOption[]>
        {
            {
                "Movie", new[]
                {
                    new ImageOption { Limit = 1, MinWidth = 1280, Type = ImageType.Backdrop },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Art },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Disc }, new ImageOption { Limit = 1, Type = ImageType.Primary }, new ImageOption { Limit = 0, Type = ImageType.Banner }, new ImageOption { Limit = 1, Type = ImageType.Thumb }, new ImageOption { Limit = 1, Type = ImageType.Logo }
                }
            },
            {
                "MusicVideo", new[]
                {
                    new ImageOption { Limit = 1, MinWidth = 1280, Type = ImageType.Backdrop },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Art },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Disc }, new ImageOption { Limit = 1, Type = ImageType.Primary }, new ImageOption { Limit = 0, Type = ImageType.Banner }, new ImageOption { Limit = 1, Type = ImageType.Thumb }, new ImageOption { Limit = 1, Type = ImageType.Logo }
                }
            },
            {
                "Series", new[]
                {
                    new ImageOption { Limit = 1, MinWidth = 1280, Type = ImageType.Backdrop },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Art }, new ImageOption { Limit = 1, Type = ImageType.Primary }, new ImageOption { Limit = 1, Type = ImageType.Banner }, new ImageOption { Limit = 1, Type = ImageType.Thumb }, new ImageOption { Limit = 1, Type = ImageType.Logo }
                }
            },
            {
                "MusicAlbum", new[]
                {
                    new ImageOption { Limit = 0, MinWidth = 1280, Type = ImageType.Backdrop },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Disc }
                }
            },
            {
                "MusicArtist", new[]
                {
                    new ImageOption { Limit = 1, MinWidth = 1280, Type = ImageType.Backdrop },

                    // Don't download this by default
                    // They do look great, but most artists won't have them, which means a banner view isn't really possible
                    new ImageOption { Limit = 0, Type = ImageType.Banner },

                    // Don't download this by default
                    // Generally not used
                    new ImageOption { Limit = 0, Type = ImageType.Art }, new ImageOption { Limit = 1, Type = ImageType.Logo }
                }
            },
            {
                "BoxSet", new[]
                {
                    new ImageOption { Limit = 1, MinWidth = 1280, Type = ImageType.Backdrop }, new ImageOption { Limit = 1, Type = ImageType.Primary }, new ImageOption { Limit = 1, Type = ImageType.Thumb }, new ImageOption { Limit = 1, Type = ImageType.Logo },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Art },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Disc },

                    // Don't download this by default as it's rarely used.
                    new ImageOption { Limit = 0, Type = ImageType.Banner }
                }
            },
            { "Season", new[] { new ImageOption { Limit = 0, MinWidth = 1280, Type = ImageType.Backdrop }, new ImageOption { Limit = 1, Type = ImageType.Primary }, new ImageOption { Limit = 0, Type = ImageType.Banner }, new ImageOption { Limit = 0, Type = ImageType.Thumb } } },
            { "Episode", new[] { new ImageOption { Limit = 0, MinWidth = 1280, Type = ImageType.Backdrop }, new ImageOption { Limit = 1, Type = ImageType.Primary } } }
        };

        private static readonly ImageOption DefaultInstance = new ImageOption();

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeOptions"/> class.
        /// </summary>
        public TypeOptions()
        {
            MetadataFetchers = Array.Empty<string>();
            MetadataFetcherOrder = Array.Empty<string>();
            ImageFetchers = Array.Empty<string>();
            ImageFetcherOrder = Array.Empty<string>();
            ImageOptions = Array.Empty<ImageOption>();
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the metadata fetchers.
        /// </summary>
        /// <value>
        /// The metadata fetchers.
        /// </value>
        public IReadOnlyCollection<string> MetadataFetchers { get; set; }

        /// <summary>
        /// Gets or sets the metadata fetcher order.
        /// </summary>
        /// <value>
        /// The metadata fetcher order.
        /// </value>
        public IReadOnlyCollection<string> MetadataFetcherOrder { get; set; }

        /// <summary>
        /// Gets or sets the image fetchers.
        /// </summary>
        /// <value>
        /// The image fetchers.
        /// </value>
        public IReadOnlyCollection<string> ImageFetchers { get; set; }

        /// <summary>
        /// Gets or sets the image fetcher order.
        /// </summary>
        /// <value>
        /// The image fetcher order.
        /// </value>
        public IReadOnlyCollection<string> ImageFetcherOrder { get; set; }

        /// <summary>
        /// Gets or sets the image options.
        /// </summary>
        /// <value>
        /// The image options.
        /// </value>
        public IReadOnlyCollection<ImageOption> ImageOptions { get; set; }

        /// <summary>
        /// Gets the image opiton that applies for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The first ImageOption that matches the given type.</returns>
        public ImageOption GetImageOptionByType(ImageType type)
        {
            foreach (var i in ImageOptions)
            {
                if (i.Type == type)
                {
                    return i;
                }
            }

            if (DefaultImageOptions.TryGetValue(Type, out ImageOption[] options))
            {
                foreach (var i in options)
                {
                    if (i.Type == type)
                    {
                        return i;
                    }
                }
            }

            return DefaultInstance;
        }

        /// <summary>
        /// Gets the limit.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Fisrt matching image option.</returns>
        public int GetLimit(ImageType type)
        {
            return GetImageOptionByType(type).Limit;
        }

        /// <summary>
        /// Gets the minimum width.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The MinWidth property of the first matching image option.</returns>
        public int GetMinWidth(ImageType type)
        {
            return GetImageOptionByType(type).MinWidth;
        }

        /// <summary>
        /// Determines whether the specified type is enabled.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is enabled; otherwise, <c>false</c>.
        /// </returns>
        public bool IsEnabled(ImageType type)
        {
            return GetLimit(type) > 0;
        }
    }
}
