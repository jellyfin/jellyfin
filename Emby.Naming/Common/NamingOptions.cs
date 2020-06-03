#pragma warning disable CS1591

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Video;
using MediaBrowser.Model.Entities;

namespace Emby.Naming.Common
{
    public class NamingOptions
    {
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
                new StubTypeRule
                {
                    StubType = "dvd",
                    Token = "dvd"
                },
                new StubTypeRule
                {
                    StubType = "hddvd",
                    Token = "hddvd"
                },
                new StubTypeRule
                {
                    StubType = "bluray",
                    Token = "bluray"
                },
                new StubTypeRule
                {
                    StubType = "bluray",
                    Token = "brrip"
                },
                new StubTypeRule
                {
                    StubType = "bluray",
                    Token = "bd25"
                },
                new StubTypeRule
                {
                    StubType = "bluray",
                    Token = "bd50"
                },
                new StubTypeRule
                {
                    StubType = "vhs",
                    Token = "vhs"
                },
                new StubTypeRule
                {
                    StubType = "tv",
                    Token = "HDTV"
                },
                new StubTypeRule
                {
                    StubType = "tv",
                    Token = "PDTV"
                },
                new StubTypeRule
                {
                    StubType = "tv",
                    Token = "DSR"
                }
            };

            VideoFileStackingExpressions = new[]
            {
                "(.*?)([ _.-]*(?:cd|dvd|p(?:ar)?t|dis[ck])[ _.-]*[0-9]+)(.*?)(\\.[^.]+)$",
                "(.*?)([ _.-]*(?:cd|dvd|p(?:ar)?t|dis[ck])[ _.-]*[a-d])(.*?)(\\.[^.]+)$",
                "(.*?)([ ._-]*[a-d])(.*?)(\\.[^.]+)$"
            };

            CleanDateTimes = new[]
            {
                @"(.+[^_\,\.\(\)\[\]\-])[_\.\(\)\[\]\-](19\d{2}|20\d{2})([ _\,\.\(\)\[\]\-][^0-9]|).*(19\d{2}|20\d{2})*",
                @"(.+[^_\,\.\(\)\[\]\-])[ _\.\(\)\[\]\-]+(19\d{2}|20\d{2})([ _\,\.\(\)\[\]\-][^0-9]|).*(19\d{2}|20\d{2})*"
            };

            CleanStrings = new[]
            {
                @"[ _\,\.\(\)\[\]\-](3d|sbs|tab|hsbs|htab|mvc|HDR|HDC|UHD|UltraHD|4k|ac3|dts|custom|dc|divx|divx5|dsr|dsrip|dutch|dvd|dvdrip|dvdscr|dvdscreener|screener|dvdivx|cam|fragment|fs|hdtv|hdrip|hdtvrip|internal|limited|multisubs|ntsc|ogg|ogm|pal|pdtv|proper|repack|rerip|retail|cd[1-9]|r3|r5|bd5|bd|se|svcd|swedish|german|read.nfo|nfofix|unrated|ws|telesync|ts|telecine|tc|brrip|bdrip|480p|480i|576p|576i|720p|720i|1080p|1080i|2160p|hrhd|hrhdtv|hddvd|bluray|x264|h264|xvid|xvidvd|xxx|www.www|\[.*\])([ _\,\.\(\)\[\]\-]|$)",
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
                new EpisodeExpression("([0-9]{4})[\\.-]([0-9]{2})[\\.-]([0-9]{2})", true)
                {
                    DateTimeFormats = new[]
                    {
                        "yyyy.MM.dd",
                        "yyyy-MM-dd",
                        "yyyy_MM_dd"
                    }
                },
                new EpisodeExpression("([0-9]{2})[\\.-]([0-9]{2})[\\.-]([0-9]{4})", true)
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
                new EpisodeExpression(@".*[\\\/](?![Ee]pisode)(?<seriesname>[\w\s]+?)\s(?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*[^\\\/x]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression("[\\\\/\\._ \\[\\(-]([0-9]+)x([0-9]+(?:(?:[a-i]|\\.[1-9])(?![0-9]))?)([^\\\\/]*)$")
                {
                    SupportsAbsoluteEpisodeNumbers = true
                },
                new EpisodeExpression(@"[\\\\/\\._ -](?<seriesname>(?![0-9]+[0-9][0-9])([^\\\/])*)[\\\\/\\._ -](?<seasonnumber>[0-9]+)(?<epnumber>[0-9][0-9](?:(?:[a-i]|\\.[1-9])(?![0-9]))?)([\\._ -][^\\\\/]*)$")
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
                new EpisodeExpression(@".*?(\[.*?\])+.*?(?<seriesname>[\w\s]+?)[-\s_]+(?<epnumber>\d+).*$")
                {
                    IsNamed = true
                },
                new EpisodeExpression(@".*(\\|\/)[sS]?(?<seasonnumber>\d+)[xX](?<epnumber>\d+)[^\\\/]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression(@".*(\\|\/)[sS](?<seasonnumber>\d+)[x,X]?[eE](?<epnumber>\d+)[^\\\/]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression(@".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d+))[^\\\/]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression(@".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d+)[^\\\/]*$")
                {
                    IsNamed = true
                },

                // "01.avi"
                new EpisodeExpression(@".*[\\\/](?<epnumber>\d+)(-(?<endingepnumber>\d+))*\.\w+$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },

                // "1-12 episode title"
                new EpisodeExpression(@"([0-9]+)-([0-9]+)"),

                // "01 - blah.avi", "01-blah.avi"
                new EpisodeExpression(@".*(\\|\/)(?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*\s?-\s?[^\\\/]*$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },

                // "01.blah.avi"
                new EpisodeExpression(@".*(\\|\/)(?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*\.[^\\\/]+$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },

                // "blah - 01.avi", "blah 2 - 01.avi", "blah - 01 blah.avi", "blah 2 - 01 blah", "blah - 01 - blah.avi", "blah 2 - 01 - blah"
                new EpisodeExpression(@".*[\\\/][^\\\/]* - (?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*[^\\\/]*$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },

                // "01 episode title.avi"
                new EpisodeExpression(@"[Ss]eason[\._ ](?<seasonnumber>[0-9]+)[\\\/](?<epnumber>\d{1,3})([^\\\/]*)$")
                {
                    IsOptimistic = true,
                    IsNamed = true
                },
                // "Episode 16", "Episode 16 - Title"
                new EpisodeExpression(@".*[\\\/][^\\\/]* (?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*[^\\\/]*$")
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
                new ExtraRule
                {
                    ExtraType = ExtraType.Trailer,
                    RuleType = ExtraRuleType.Filename,
                    Token = "trailer",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Trailer,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-trailer",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Trailer,
                    RuleType = ExtraRuleType.Suffix,
                    Token = ".trailer",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Trailer,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "_trailer",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Trailer,
                    RuleType = ExtraRuleType.Suffix,
                    Token = " trailer",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Sample,
                    RuleType = ExtraRuleType.Filename,
                    Token = "sample",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Sample,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-sample",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Sample,
                    RuleType = ExtraRuleType.Suffix,
                    Token = ".sample",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Sample,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "_sample",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Sample,
                    RuleType = ExtraRuleType.Suffix,
                    Token = " sample",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.ThemeSong,
                    RuleType = ExtraRuleType.Filename,
                    Token = "theme",
                    MediaType = MediaType.Audio
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Scene,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-scene",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Clip,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-clip",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Interview,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-interview",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.BehindTheScenes,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-behindthescenes",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.DeletedScene,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-deleted",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Clip,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-featurette",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Clip,
                    RuleType = ExtraRuleType.Suffix,
                    Token = "-short",
                    MediaType = MediaType.Video
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.BehindTheScenes,
                    RuleType = ExtraRuleType.DirectoryName,
                    Token = "behind the scenes",
                    MediaType = MediaType.Video,
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.DeletedScene,
                    RuleType = ExtraRuleType.DirectoryName,
                    Token = "deleted scenes",
                    MediaType = MediaType.Video,
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Interview,
                    RuleType = ExtraRuleType.DirectoryName,
                    Token = "interviews",
                    MediaType = MediaType.Video,
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Scene,
                    RuleType = ExtraRuleType.DirectoryName,
                    Token = "scenes",
                    MediaType = MediaType.Video,
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Sample,
                    RuleType = ExtraRuleType.DirectoryName,
                    Token = "samples",
                    MediaType = MediaType.Video,
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Clip,
                    RuleType = ExtraRuleType.DirectoryName,
                    Token = "shorts",
                    MediaType = MediaType.Video,
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Clip,
                    RuleType = ExtraRuleType.DirectoryName,
                    Token = "featurettes",
                    MediaType = MediaType.Video,
                },
                new ExtraRule
                {
                    ExtraType = ExtraType.Unknown,
                    RuleType = ExtraRuleType.DirectoryName,
                    Token = "extras",
                    MediaType = MediaType.Video,
                },
            };

            Format3DRules = new[]
            {
                // Kodi rules:
                new Format3DRule
                {
                    PreceedingToken = "3d",
                    Token = "hsbs"
                },
                new Format3DRule
                {
                    PreceedingToken = "3d",
                    Token = "sbs"
                },
                new Format3DRule
                {
                    PreceedingToken = "3d",
                    Token = "htab"
                },
                new Format3DRule
                {
                    PreceedingToken = "3d",
                    Token = "tab"
                },
                                 // Media Browser rules:
                new Format3DRule
                {
                    Token = "fsbs"
                },
                new Format3DRule
                {
                    Token = "hsbs"
                },
                new Format3DRule
                {
                    Token = "sbs"
                },
                new Format3DRule
                {
                    Token = "ftab"
                },
                new Format3DRule
                {
                    Token = "htab"
                },
                new Format3DRule
                {
                    Token = "tab"
                },
                new Format3DRule
                {
                    Token = "sbs3d"
                },
                new Format3DRule
                {
                    Token = "mvc"
                }
            };
            AudioBookPartsExpressions = new[]
            {
                // Detect specified chapters, like CH 01
                @"ch(?:apter)?[\s_-]?(?<chapter>\d+)",
                // Detect specified parts, like Part 02
                @"p(?:ar)?t[\s_-]?(?<part>\d+)",
                // Chapter is often beginning of filename
                @"^(?<chapter>\d+)",
                // Part if often ending of filename
                @"(?<part>\d+)$",
                // Sometimes named as 0001_005 (chapter_part)
                @"(?<chapter>\d+)_(?<part>\d+)",
                // Some audiobooks are ripped from cd's, and will be named by disk number.
                @"dis(?:c|k)[\s_-]?(?<chapter>\d+)"
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

            MultipleEpisodeExpressions = new string[]
            {
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )\d{1,4}[eExX](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )\d{1,4}[xX][eE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})(-[xE]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )\d{1,4}[xXeE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )\d{1,4}[xX][eE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))(-[xX]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                @".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})(-[xX]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\\/]*$"
            }.Select(i => new EpisodeExpression(i)
            {
                IsNamed = true
            }).ToArray();

            VideoFileExtensions = extensions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Compile();
        }

        public string[] AudioFileExtensions { get; set; }

        public string[] AlbumStackingPrefixes { get; set; }

        public string[] SubtitleFileExtensions { get; set; }

        public char[] SubtitleFlagDelimiters { get; set; }

        public string[] SubtitleForcedFlags { get; set; }

        public string[] SubtitleDefaultFlags { get; set; }

        public EpisodeExpression[] EpisodeExpressions { get; set; }

        public string[] EpisodeWithoutSeasonExpressions { get; set; }

        public string[] EpisodeMultiPartExpressions { get; set; }

        public string[] VideoFileExtensions { get; set; }

        public string[] StubFileExtensions { get; set; }

        public string[] AudioBookPartsExpressions { get; set; }

        public StubTypeRule[] StubTypes { get; set; }

        public char[] VideoFlagDelimiters { get; set; }

        public Format3DRule[] Format3DRules { get; set; }

        public string[] VideoFileStackingExpressions { get; set; }

        public string[] CleanDateTimes { get; set; }

        public string[] CleanStrings { get; set; }

        public EpisodeExpression[] MultipleEpisodeExpressions { get; set; }

        public ExtraRule[] VideoExtraRules { get; set; }

        public Regex[] VideoFileStackingRegexes { get; private set; }

        public Regex[] CleanDateTimeRegexes { get; private set; }

        public Regex[] CleanStringRegexes { get; private set; }

        public Regex[] EpisodeWithoutSeasonRegexes { get; private set; }

        public Regex[] EpisodeMultiPartRegexes { get; private set; }

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
