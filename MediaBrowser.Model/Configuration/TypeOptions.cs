#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Configuration
{
    public class TypeOptions
    {
        public static readonly ImageOption DefaultInstance = new ImageOption();

        public static readonly Dictionary<string, ImageOption[]> DefaultImageOptions = new Dictionary<string, ImageOption[]>
        {
            {
                "Movie", new[]
                {
                    new ImageOption
                    {
                        Limit = 1,
                        MinWidth = 1280,
                        Type = ImageType.Backdrop
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Art
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Disc
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Primary
                    },

                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Banner
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Thumb
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Logo
                    }
                }
            },
            {
                "MusicVideo", new[]
                {
                    new ImageOption
                    {
                        Limit = 1,
                        MinWidth = 1280,
                        Type = ImageType.Backdrop
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Art
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Disc
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Primary
                    },

                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Banner
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Thumb
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Logo
                    }
                }
            },
            {
                "Series", new[]
                {
                    new ImageOption
                    {
                        Limit = 1,
                        MinWidth = 1280,
                        Type = ImageType.Backdrop
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Art
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Primary
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Banner
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Thumb
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Logo
                    }
                }
            },
            {
                "MusicAlbum", new[]
                {
                    new ImageOption
                    {
                        Limit = 0,
                        MinWidth = 1280,
                        Type = ImageType.Backdrop
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Disc
                    }
                }
            },
            {
                "MusicArtist", new[]
                {
                    new ImageOption
                    {
                        Limit = 1,
                        MinWidth = 1280,
                        Type = ImageType.Backdrop
                    },

                    // Don't download this by default
                    // They do look great, but most artists won't have them, which means a banner view isn't really possible
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Banner
                    },

                    // Don't download this by default
                    // Generally not used
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Art
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Logo
                    }
                }
            },
            {
                "BoxSet", new[]
                {
                    new ImageOption
                    {
                        Limit = 1,
                        MinWidth = 1280,
                        Type = ImageType.Backdrop
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Primary
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Thumb
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Logo
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Art
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Disc
                    },

                    // Don't download this by default as it's rarely used.
                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Banner
                    }
                }
            },
            {
                "Season", new[]
                {
                    new ImageOption
                    {
                        Limit = 0,
                        MinWidth = 1280,
                        Type = ImageType.Backdrop
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Primary
                    },

                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Banner
                    },

                    new ImageOption
                    {
                        Limit = 0,
                        Type = ImageType.Thumb
                    }
                }
            },
            {
                "Episode", new[]
                {
                    new ImageOption
                    {
                        Limit = 0,
                        MinWidth = 1280,
                        Type = ImageType.Backdrop
                    },

                    new ImageOption
                    {
                        Limit = 1,
                        Type = ImageType.Primary
                    }
                }
            }
        };

        public TypeOptions()
        {
            MetadataFetchers = Array.Empty<string>();
            MetadataFetcherOrder = Array.Empty<string>();
            ImageFetchers = Array.Empty<string>();
            ImageFetcherOrder = Array.Empty<string>();
            ImageOptions = Array.Empty<ImageOption>();
        }

        public string Type { get; set; }

        public string[] MetadataFetchers { get; set; }

        public string[] MetadataFetcherOrder { get; set; }

        public string[] ImageFetchers { get; set; }

        public string[] ImageFetcherOrder { get; set; }

        public ImageOption[] ImageOptions { get; set; }

        public ImageOption GetImageOptions(ImageType type)
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

        public int GetLimit(ImageType type)
        {
            return GetImageOptions(type).Limit;
        }

        public int GetMinWidth(ImageType type)
        {
            return GetImageOptions(type).MinWidth;
        }

        public bool IsEnabled(ImageType type)
        {
            return GetLimit(type) > 0;
        }
    }
}
