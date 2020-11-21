using System;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Video;
using MediaBrowser.Model.Entities;

// ReSharper disable StringLiteralTypo

namespace Emby.Naming.Common
{
    /// <summary>
    /// Big ugly class containing lot of different naming options that should be split and injected instead of passes everywhere.
    /// </summary>
    public class NamingOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamingOptions"/> class.
        /// </summary>
        public NamingOptions()
        {
            VideoFileExtensions = new[]
            {
                ".m4v",
                ".3gp",
                ".nsv",
                ".ts",
                ".ty",
                ".strm",
                ".rm",
                ".rmvb",
                ".ifo",
                ".mov",
                ".qt",
                ".divx",
                ".xvid",
                ".bivx",
                ".vob",
                ".nrg",
                ".img",
                ".iso",
                ".pva",
                ".wmv",
                ".asf",
                ".asx",
                ".ogm",
                ".m2v",
                ".avi",
                ".bin",
                ".dvr-ms",
                ".mpg",
                ".mpeg",
                ".mp4",
                ".mkv",
                ".avc",
                ".vp3",
                ".svq3",
                ".nuv",
                ".viv",
                ".dv",
                ".fli",
                ".flv",
                ".001",
                ".tp"
            };

            VideoFlagDelimiters = new[]
            {
                '(',
                ')',
                '-',
                '.',
                '_',
                '[',
                ']'
            };

            StubFileExtensions = new[]
            {
                ".disc"
            };

            StubTypes = new[]
            {
                new StubTypeRule(
                    stubType: "dvd",
                    token: "dvd"),

                new StubTypeRule(
                    stubType: "hddvd",
                    token: "hddvd"),

                new StubTypeRule(
                    stubType: "bluray",
                    token: "bluray"),

                new StubTypeRule(
                    stubType: "bluray",
                    token: "brrip"),

                new StubTypeRule(
                    stubType: "bluray",
                    token: "bd25"),

                new StubTypeRule(
                    stubType: "bluray",
                    token: "bd50"),

                new StubTypeRule(
                    stubType: "vhs",
                    token: "vhs"),

                new StubTypeRule(
                    stubType: "tv",
                    token: "HDTV"),

                new StubTypeRule(
                    stubType: "tv",
                    token: "PDTV"),

                new StubTypeRule(
                    stubType: "tv",
                    token: "DSR")
            };

            VideoFileStackingExpressions = new[]
            {
                "(?<title>.*?)(?<volume>[ _.-]*(?:cd|dvd|p(?:ar)?t|dis[ck])[ _.-]*[0-9]+)(?<ignore>.*?)(?<extension>\\.[^.]+)$",
                "(?<title>.*?)(?<volume>[ _.-]*(?:cd|dvd|p(?:ar)?t|dis[ck])[ _.-]*[a-d])(?<ignore>.*?)(?<extension>\\.[^.]+)$",
                "(?<title>.*?)(?<volume>[ ._-]*[a-d])(?<ignore>.*?)(?<extension>\\.[^.]+)$"
            };

            CleanDateTimes = new[]
            {
                @"(.+[^_\,\.\(\)\[\]\-])[_\.\(\)\[\]\-](19[0-9]{2}|20[0-9]{2})(?![0-9]+|\W[0-9]{2}\W[0-9]{2})([ _\,\.\(\)\[\]\-][^0-9]|).*(19[0-9]{2}|20[0-9]{2})*",
                @"(.+[^_\,\.\(\)\[\]\-])[ _\.\(\)\[\]\-]+(19[0-9]{2}|20[0-9]{2})(?![0-9]+|\W[0-9]{2}\W[0-9]{2})([ _\,\.\(\)\[\]\-][^0-9]|).*(19[0-9]{2}|20[0-9]{2})*"
            };

            CleanStrings = new[]
            {
                @"[ _\,\.\(\)\[\]\-](3d|sbs|tab|hsbs|htab|mvc|HDR|HDC|UHD|UltraHD|4k|ac3|dts|custom|dc|divx|divx5|dsr|dsrip|dutch|dvd|dvdrip|dvdscr|dvdscreener|screener|dvdivx|cam|fragment|fs|hdtv|hdrip|hdtvrip|internal|limited|multisubs|ntsc|ogg|ogm|pal|pdtv|proper|repack|rerip|retail|cd[1-9]|r3|r5|bd5|bd|se|svcd|swedish|german|read.nfo|nfofix|unrated|ws|telesync|ts|telecine|tc|brrip|bdrip|480p|480i|576p|576i|720p|720i|1080p|1080i|2160p|hrhd|hrhdtv|hddvd|bluray|blu-ray|x264|x265|h264|xvid|xvidvd|xxx|www.www|AAC|DTS|\[.*\])([ _\,\.\(\)\[\]\-]|$)",
                @"(\[.*\])"
            };

            SubtitleFileExtensions = new[]
            {
                ".srt",
                ".ssa",
                ".ass",
                ".sub"
            };

            SubtitleFlagDelimiters = new[]
            {
                '.'
            };

            SubtitleForcedFlags = new[]
            {
                "foreign",
                "forced"
            };

            SubtitleDefaultFlags = new[]
            {
                "default"
            };

            AlbumStackingPrefixes = new[]
            {
                "disc",
                "cd",
                "disk",
                "vol",
                "volume"
            };

            AudioFileExtensions = new[]
            {
                ".nsv",
                ".m4a",
                ".flac",
                ".aac",
                ".strm",
                ".pls",
                ".rm",
                ".mpa",
                ".wav",
                ".wma",
                ".ogg",
                ".opus",
                ".mp3",
                ".mp2",
                ".mod",
                ".amf",
                ".669",
                ".dmf",
                ".dsm",
                ".far",
                ".gdm",
                ".imf",
                ".it",
                ".m15",
                ".med",
                ".okt",
                ".s3m",
                ".stm",
                ".sfx",
                ".ult",
                ".uni",
                ".xm",
                ".sid",
                ".ac3",
                ".dts",
                ".cue",
                ".aif",
                ".aiff",
                ".ape",
                ".mac",
                ".mpc",
                ".mp+",
                ".mpp",
                ".shn",
                ".wv",
                ".nsf",
                ".spc",
                ".gym",
                ".adplug",
                ".adx",
                ".dsp",
                ".adp",
                ".ymf",
                ".ast",
                ".afc",
                ".hps",
                ".xsp",
                ".acc",
                ".m4b",
                ".oga",
                ".dsf",
                ".mka"
            };

            EpisodeExpressions = new[]
            {
                // *** Begin Kodi Standard Naming
                // <!-- foo.s01.e01, foo.s01_e01, S01E02 foo, S01 - E02 -->
                new EpisodeExpression(@".*(\\|\/)(?<seriesname>((?![Ss]([0-9]+)[][ ._-]*[Ee]([0-9]+))[^\\\/])*)?[Ss](?<seasonnumber>[0-9]+)[][ ._-]*[Ee](?<epnumber>[0-9]+)([^\\/]*)$")
                {
                    IsNamed = true
                },
                // <!-- foo.ep01, foo.EP_01 -->
                new EpisodeExpression(@"[\._ -]()[Ee][Pp]_?([0-9]+)([^\\/]*)$"),
                new EpisodeExpression("(?<year>[0-9]{4})[\\.-](?<month>[0-9]{2})[\\.-](?<day>[0-9]{2})", true)
                {
                    DateTimeFormats = new[]
                    {
                        "yyyy.MM.dd",
                        "yyyy-MM-dd",
                        "yyyy_MM_dd"
                    }
                },
                new EpisodeExpression(@"(?<day>[0-9]{2})[.-](?<month>[0-9]{2})[.-](?<year>[0-9]{4})", true)
                {
                    DateTimeFormats = new[]
                    {
                        "dd.MM.yyyy",
                        "dd-MM-yyyy",
                        "dd_MM_yyyy"
                    }
                },

                // This isn't a Kodi naming rule, but the expression below causes false positives,
                // so we make sure this one gets tested first.
                // "Foo Bar 889"
                new EpisodeExpression(@".*[\\\/](?![Ee]pisode)(?<seriesname>[\w\s]+?)\s(?<epnumber>[0-9]{1,3})(-(?<endingepnumber>[0-9]{2,3}))*[^\\\/x]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression("[\\\\/\\._ \\[\\(-]([0-9]+)x([0-9]+(?:(?:[a-i]|\\.[1-9])(?![0-9]))?)([^\\\\/]*)$")
                {
                    SupportsAbsoluteEpisodeNumbers = true
                },

                // Case Closed (1996-2007)/Case Closed - 317.mkv
                // /server/anything_102.mp4
                // /server/james.corden.2017.04.20.anne.hathaway.720p.hdtv.x264-crooks.mkv
                // /server/anything_1996.11.14.mp4
                new EpisodeExpression(@"[\\/._ -](?<seriesname>(?![0-9]+[0-9][0-9])([^\\\/_])*)[\\\/._ -](?<seasonnumber>[0-9]+)(?<epnumber>[0-9][0-9](?:(?:[a-i]|\.[1-9])(?![0-9]))?)([._ -][^\\\/]*)$")
                {
                    IsOptimistic = true,
                    IsNamed = true,
                    SupportsAbsoluteEpisodeNumbers = false
                },
                new EpisodeExpression("[\\/._ -]p(?:ar)?t[_. -]()([ivx]+|[0-9]+)([._ -][^\\/]*)$")
                {
                    SupportsAbsoluteEpisodeNumbers = true
                },

                // *** End Kodi Standard Naming

                // [bar] Foo - 1 [baz]
                new EpisodeExpression(@".*?(\[.*?\])+.*?(?<seriesname>[\w\s]+?)[-\s_]+(?<epnumber>[0-9]+).*$")
                {
                    IsNamed = true
                },
                new EpisodeExpression(@".*(\\|\/)[sS]?(?<seasonnumber>[0-9]+)[xX](?<epnumber>[0-9]+)[^\\\/]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression(@".*(\\|\/)[sS](?<seasonnumber>[0-9]+)[x,X]?[eE](?<epnumber>[0-9]+)[^\\\/]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression(@".*(\\|\/)(?<seriesname>((?![sS]?[0-9]{1,4}[xX][0-9]{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]+))[^\\\/]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression(@".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>[0-9]{1,4})[xX\.]?[eE](?<epnumber>[0-9]+)[^\\\/]*$")
                {
                    IsNamed = true
                },

                // "01.avi"
                new EpisodeExpression(@".*[\\\/](?<epnumber>[0-9]+)(-(?<endingepnumber>[0-9]+))*\.\w+$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },

                // "1-12 episode title"
                new EpisodeExpression(@"([0-9]+)-([0-9]+)"),

                // "01 - blah.avi", "01-blah.avi"
                new EpisodeExpression(@".*(\\|\/)(?<epnumber>[0-9]{1,3})(-(?<endingepnumber>[0-9]{2,3}))*\s?-\s?[^\\\/]*$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },

                // "01.blah.avi"
                new EpisodeExpression(@".*(\\|\/)(?<epnumber>[0-9]{1,3})(-(?<endingepnumber>[0-9]{2,3}))*\.[^\\\/]+$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },

                // "blah - 01.avi", "blah 2 - 01.avi", "blah - 01 blah.avi", "blah 2 - 01 blah", "blah - 01 - blah.avi", "blah 2 - 01 - blah"
                new EpisodeExpression(@".*[\\\/][^\\\/]* - (?<epnumber>[0-9]{1,3})(-(?<endingepnumber>[0-9]{2,3}))*[^\\\/]*$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },

                // "01 episode title.avi"
                new EpisodeExpression(@"[Ss]eason[\._ ](?<seasonnumber>[0-9]+)[\\\/](?<epnumber>[0-9]{1,3})([^\\\/]*)$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },
                // "Episode 16", "Episode 16 - Title"
                new EpisodeExpression(@".*[\\\/][^\\\/]* (?<epnumber>[0-9]{1,3})(-(?<endingepnumber>[0-9]{2,3}))*[^\\\/]*$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                }
            };

            EpisodeWithoutSeasonExpressions = new[]
            {
                @"[/\._ \-]()([0-9]+)(-[0-9]+)?"
            };

            EpisodeMultiPartExpressions = new[]
            {
                @"^[-_ex]+([0-9]+(?:(?:[a-i]|\\.[1-9])(?![0-9]))?)"
            };

            VideoExtraRules = new[]
            {
                new ExtraRule(
                    ExtraType.Trailer,
                    ExtraRuleType.Filename,
                    "trailer",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Trailer,
                    ExtraRuleType.Suffix,
                    "-trailer",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Trailer,
                    ExtraRuleType.Suffix,
                    ".trailer",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Trailer,
                    ExtraRuleType.Suffix,
                    "_trailer",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Trailer,
                    ExtraRuleType.Suffix,
                    " trailer",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Sample,
                    ExtraRuleType.Filename,
                    "sample",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Sample,
                    ExtraRuleType.Suffix,
                    "-sample",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Sample,
                    ExtraRuleType.Suffix,
                    ".sample",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Sample,
                    ExtraRuleType.Suffix,
                    "_sample",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Sample,
                    ExtraRuleType.Suffix,
                    " sample",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.ThemeSong,
                    ExtraRuleType.Filename,
                    "theme",
                    MediaType.Audio),

                new ExtraRule(
                    ExtraType.Scene,
                    ExtraRuleType.Suffix,
                    "-scene",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Clip,
                    ExtraRuleType.Suffix,
                    "-clip",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Interview,
                    ExtraRuleType.Suffix,
                    "-interview",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.BehindTheScenes,
                    ExtraRuleType.Suffix,
                    "-behindthescenes",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.DeletedScene,
                    ExtraRuleType.Suffix,
                    "-deleted",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Clip,
                    ExtraRuleType.Suffix,
                    "-featurette",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Clip,
                    ExtraRuleType.Suffix,
                    "-short",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.BehindTheScenes,
                    ExtraRuleType.DirectoryName,
                    "behind the scenes",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.DeletedScene,
                    ExtraRuleType.DirectoryName,
                    "deleted scenes",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Interview,
                    ExtraRuleType.DirectoryName,
                    "interviews",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Scene,
                    ExtraRuleType.DirectoryName,
                    "scenes",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Sample,
                    ExtraRuleType.DirectoryName,
                    "samples",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Clip,
                    ExtraRuleType.DirectoryName,
                    "shorts",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Clip,
                    ExtraRuleType.DirectoryName,
                    "featurettes",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Unknown,
                    ExtraRuleType.DirectoryName,
                    "extras",
                    MediaType.Video),
            };

            Format3DRules = new[]
            {
                // Kodi rules:
                new Format3DRule(
                    precedingToken: "3d",
                    token: "hsbs"),

                new Format3DRule(
                    precedingToken: "3d",
                    token: "sbs"),

                new Format3DRule(
                    precedingToken: "3d",
                    token: "htab"),

                new Format3DRule(
                    precedingToken: "3d",
                    token: "tab"),

                 // Media Browser rules:
                new Format3DRule("fsbs"),
                new Format3DRule("hsbs"),
                new Format3DRule("sbs"),
                new Format3DRule("ftab"),
                new Format3DRule("htab"),
                new Format3DRule("tab"),
                new Format3DRule("sbs3d"),
                new Format3DRule("mvc")
            };

            AudioBookPartsExpressions = new[]
            {
                // Detect specified chapters, like CH 01
                @"ch(?:apter)?[\s_-]?(?<chapter>[0-9]+)",
                // Detect specified parts, like Part 02
                @"p(?:ar)?t[\s_-]?(?<part>[0-9]+)",
                // Chapter is often beginning of filename
                "^(?<chapter>[0-9]+)",
                // Part if often ending of filename
                @"(?<!ch(?:apter) )(?<part>[0-9]+)$",
                // Sometimes named as 0001_005 (chapter_part)
                "(?<chapter>[0-9]+)_(?<part>[0-9]+)",
                // Some audiobooks are ripped from cd's, and will be named by disk number.
                @"dis(?:c|k)[\s_-]?(?<chapter>[0-9]+)"
            };

            AudioBookNamesExpressions = new[]
            {
                // Detect year usually in brackets after name Batman (2020)
                @"^(?<name>.+?)\s*\(\s*(?<year>\d{4})\s*\)\s*$",
                @"^\s*(?<name>[^ ].*?)\s*$"
            };

            var extensions = VideoFileExtensions.ToList();

            extensions.AddRange(new[]
            {
                ".mkv",
                ".m2t",
                ".m2ts",
                ".img",
                ".iso",
                ".mk3d",
                ".ts",
                ".rmvb",
                ".mov",
                ".avi",
                ".mpg",
                ".mpeg",
                ".wmv",
                ".mp4",
                ".divx",
                ".dvr-ms",
                ".wtv",
                ".ogm",
                ".ogv",
                ".asf",
                ".m4v",
                ".flv",
                ".f4v",
                ".3gp",
                ".webm",
                ".mts",
                ".m2v",
                ".rec",
                ".mxf"
            });

            MultipleEpisodeExpressions = new[]
            {
                @".*(\\|\/)[sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]{1,3})((-| - )[0-9]{1,4}[eExX](?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)[sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]{1,3})((-| - )[0-9]{1,4}[xX][eE](?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)[sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]{1,3})((-| - )?[xXeE](?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)[sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]{1,3})(-[xE]?[eE]?(?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>((?![sS]?[0-9]{1,4}[xX][0-9]{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]{1,3}))((-| - )[0-9]{1,4}[xXeE](?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>((?![sS]?[0-9]{1,4}[xX][0-9]{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]{1,3}))((-| - )[0-9]{1,4}[xX][eE](?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>((?![sS]?[0-9]{1,4}[xX][0-9]{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]{1,3}))((-| - )?[xXeE](?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>((?![sS]?[0-9]{1,4}[xX][0-9]{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>[0-9]{1,4})[xX](?<epnumber>[0-9]{1,3}))(-[xX]?[eE]?(?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>[0-9]{1,4})[xX\.]?[eE](?<epnumber>[0-9]{1,3})((-| - )?[xXeE](?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>[0-9]{1,4})[xX\.]?[eE](?<epnumber>[0-9]{1,3})(-[xX]?[eE]?(?<endingepnumber>[0-9]{1,3}))+[^\\\/]*$"
            }.Select(i => new EpisodeExpression(i)
            {
                IsNamed = true
            }).ToArray();

            VideoFileExtensions = extensions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Compile();
        }

        /// <summary>
        /// Gets or sets list of audio file extensions.
        /// </summary>
        public string[] AudioFileExtensions { get; set; }

        /// <summary>
        /// Gets or sets list of album stacking prefixes.
        /// </summary>
        public string[] AlbumStackingPrefixes { get; set; }

        /// <summary>
        /// Gets or sets list of subtitle file extensions.
        /// </summary>
        public string[] SubtitleFileExtensions { get; set; }

        /// <summary>
        /// Gets or sets list of subtitles flag delimiters.
        /// </summary>
        public char[] SubtitleFlagDelimiters { get; set; }

        /// <summary>
        /// Gets or sets list of subtitle forced flags.
        /// </summary>
        public string[] SubtitleForcedFlags { get; set; }

        /// <summary>
        /// Gets or sets list of subtitle default flags.
        /// </summary>
        public string[] SubtitleDefaultFlags { get; set; }

        /// <summary>
        /// Gets or sets list of episode regular expressions.
        /// </summary>
        public EpisodeExpression[] EpisodeExpressions { get; set; }

        /// <summary>
        /// Gets or sets list of raw episode without season regular expressions strings.
        /// </summary>
        public string[] EpisodeWithoutSeasonExpressions { get; set; }

        /// <summary>
        /// Gets or sets list of raw multi-part episodes regular expressions strings.
        /// </summary>
        public string[] EpisodeMultiPartExpressions { get; set; }

        /// <summary>
        /// Gets or sets list of video file extensions.
        /// </summary>
        public string[] VideoFileExtensions { get; set; }

        /// <summary>
        /// Gets or sets list of video stub file extensions.
        /// </summary>
        public string[] StubFileExtensions { get; set; }

        /// <summary>
        /// Gets or sets list of raw audiobook parts regular expressions strings.
        /// </summary>
        public string[] AudioBookPartsExpressions { get; set; }

        /// <summary>
        /// Gets or sets list of raw audiobook names regular expressions strings.
        /// </summary>
        public string[] AudioBookNamesExpressions { get; set; }

        /// <summary>
        /// Gets or sets list of stub type rules.
        /// </summary>
        public StubTypeRule[] StubTypes { get; set; }

        /// <summary>
        /// Gets or sets list of video flag delimiters.
        /// </summary>
        public char[] VideoFlagDelimiters { get; set; }

        /// <summary>
        /// Gets or sets list of 3D Format rules.
        /// </summary>
        public Format3DRule[] Format3DRules { get; set; }

        /// <summary>
        /// Gets or sets list of raw video file-stacking expressions strings.
        /// </summary>
        public string[] VideoFileStackingExpressions { get; set; }

        /// <summary>
        /// Gets or sets list of raw clean DateTimes regular expressions strings.
        /// </summary>
        public string[] CleanDateTimes { get; set; }

        /// <summary>
        /// Gets or sets list of raw clean strings regular expressions strings.
        /// </summary>
        public string[] CleanStrings { get; set; }

        /// <summary>
        /// Gets or sets list of multi-episode regular expressions.
        /// </summary>
        public EpisodeExpression[] MultipleEpisodeExpressions { get; set; }

        /// <summary>
        /// Gets or sets list of extra rules for videos.
        /// </summary>
        public ExtraRule[] VideoExtraRules { get; set; }

        /// <summary>
        /// Gets list of video file-stack regular expressions.
        /// </summary>
        public Regex[] VideoFileStackingRegexes { get; private set; } = Array.Empty<Regex>();

        /// <summary>
        /// Gets list of clean datetime regular expressions.
        /// </summary>
        public Regex[] CleanDateTimeRegexes { get; private set; } = Array.Empty<Regex>();

        /// <summary>
        /// Gets list of clean string regular expressions.
        /// </summary>
        public Regex[] CleanStringRegexes { get; private set; } = Array.Empty<Regex>();

        /// <summary>
        /// Gets list of episode without season regular expressions.
        /// </summary>
        public Regex[] EpisodeWithoutSeasonRegexes { get; private set; } = Array.Empty<Regex>();

        /// <summary>
        /// Gets list of multi-part episode regular expressions.
        /// </summary>
        public Regex[] EpisodeMultiPartRegexes { get; private set; } = Array.Empty<Regex>();

        /// <summary>
        /// Compiles raw regex strings into regexes.
        /// </summary>
        public void Compile()
        {
            VideoFileStackingRegexes = VideoFileStackingExpressions.Select(Compile).ToArray();
            CleanDateTimeRegexes = CleanDateTimes.Select(Compile).ToArray();
            CleanStringRegexes = CleanStrings.Select(Compile).ToArray();
            EpisodeWithoutSeasonRegexes = EpisodeWithoutSeasonExpressions.Select(Compile).ToArray();
            EpisodeMultiPartRegexes = EpisodeMultiPartExpressions.Select(Compile).ToArray();
        }

        private Regex Compile(string exp)
        {
            return new Regex(exp, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
