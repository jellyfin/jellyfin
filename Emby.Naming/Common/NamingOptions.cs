#pragma warning disable CA1819

using System;
using System.Collections.Generic;
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
                ".001",
                ".3g2",
                ".3gp",
                ".amv",
                ".asf",
                ".asx",
                ".avi",
                ".bin",
                ".bivx",
                ".divx",
                ".dv",
                ".dvr-ms",
                ".f4v",
                ".fli",
                ".flv",
                ".ifo",
                ".img",
                ".iso",
                ".m2t",
                ".m2ts",
                ".m2v",
                ".m4v",
                ".mkv",
                ".mk3d",
                ".mov",
                ".mp4",
                ".mpe",
                ".mpeg",
                ".mpg",
                ".mts",
                ".mxf",
                ".nrg",
                ".nsv",
                ".nuv",
                ".ogg",
                ".ogm",
                ".ogv",
                ".pva",
                ".qt",
                ".rec",
                ".rm",
                ".rmvb",
                ".strm",
                ".svq3",
                ".tp",
                ".ts",
                ".ty",
                ".viv",
                ".vob",
                ".vp3",
                ".webm",
                ".wmv",
                ".wtv",
                ".xvid"
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

            VideoFileStackingRules = new[]
            {
                new FileStackRule(@"^(?<filename>.*?)(?:(?<=[\]\)\}])|[ _.-]+)[\(\[]?(?<parttype>cd|dvd|part|pt|dis[ck])[ _.-]*(?<number>[0-9]+)[\)\]]?(?:\.[^.]+)?$", true),
                new FileStackRule(@"^(?<filename>.*?)(?:(?<=[\]\)\}])|[ _.-]+)[\(\[]?(?<parttype>cd|dvd|part|pt|dis[ck])[ _.-]*(?<number>[a-d])[\)\]]?(?:\.[^.]+)?$", false)
            };

            CleanDateTimes = new[]
            {
                @"(.+[^_\,\.\(\)\[\]\-])[_\.\(\)\[\]\-](19[0-9]{2}|20[0-9]{2})(?![0-9]+|\W[0-9]{2}\W[0-9]{2})([ _\,\.\(\)\[\]\-][^0-9]|).*(19[0-9]{2}|20[0-9]{2})*",
                @"(.+[^_\,\.\(\)\[\]\-])[ _\.\(\)\[\]\-]+(19[0-9]{2}|20[0-9]{2})(?![0-9]+|\W[0-9]{2}\W[0-9]{2})([ _\,\.\(\)\[\]\-][^0-9]|).*(19[0-9]{2}|20[0-9]{2})*"
            };

            CleanStrings = new[]
            {
                @"^\s*(?<cleaned>.+?)[ _\,\.\(\)\[\]\-](3d|sbs|tab|hsbs|htab|mvc|HDR|HDC|UHD|UltraHD|4k|ac3|dts|custom|dc|divx|divx5|dsr|dsrip|dutch|dvd|dvdrip|dvdscr|dvdscreener|screener|dvdivx|cam|fragment|fs|hdtv|hdrip|hdtvrip|internal|limited|multi|subs|ntsc|ogg|ogm|pal|pdtv|proper|repack|rerip|retail|cd[1-9]|r5|bd5|bd|se|svcd|swedish|german|read.nfo|nfofix|unrated|ws|telesync|ts|telecine|tc|brrip|bdrip|480p|480i|576p|576i|720p|720i|1080p|1080i|2160p|hrhd|hrhdtv|hddvd|bluray|blu-ray|x264|x265|h264|h265|xvid|xvidvd|xxx|www.www|AAC|DTS|\[.*\])([ _\,\.\(\)\[\]\-]|$)",
                @"^(?<cleaned>.+?)(\[.*\])",
                @"^\s*(?<cleaned>.+?)\WE[0-9]+(-|~)E?[0-9]+(\W|$)",
                @"^\s*\[[^\]]+\](?!\.\w+$)\s*(?<cleaned>.+)",
                @"^\s*(?<cleaned>.+?)\s+-\s+[0-9]+\s*$",
                @"^\s*(?<cleaned>.+?)(([-._ ](trailer|sample))|-(scene|clip|behindthescenes|deleted|deletedscene|featurette|short|interview|other|extra))$"
            };

            SubtitleFileExtensions = new[]
            {
                ".ass",
                ".mks",
                ".sami",
                ".smi",
                ".srt",
                ".ssa",
                ".sub",
                ".sup",
                ".vtt",
            };

            LyricFileExtensions = new[]
            {
                ".lrc",
                ".elrc",
                ".txt"
            };

            AlbumStackingPrefixes = new[]
            {
                "cd",
                "digital media",
                "disc",
                "disk",
                "vol",
                "volume"
            };

            ArtistSubfolders = new[]
            {
                "albums",
                "broadcasts",
                "bootlegs",
                "compilations",
                "dj-mixes",
                "eps",
                "live",
                "mixtapes",
                "others",
                "remixes",
                "singles",
                "soundtracks",
                "spokenwords",
                "streets"
            };

            AudioFileExtensions = new[]
            {
                ".669",
                ".3gp",
                ".aa",
                ".aac",
                ".aax",
                ".ac3",
                ".act",
                ".adp",
                ".adplug",
                ".adx",
                ".afc",
                ".amf",
                ".aif",
                ".aiff",
                ".alac",
                ".amr",
                ".ape",
                ".ast",
                ".au",
                ".awb",
                ".cda",
                ".cue",
                ".dmf",
                ".dsf",
                ".dsm",
                ".dsp",
                ".dts",
                ".dvf",
                ".far",
                ".flac",
                ".gdm",
                ".gsm",
                ".gym",
                ".hps",
                ".imf",
                ".it",
                ".m15",
                ".m4a",
                ".m4b",
                ".mac",
                ".med",
                ".mka",
                ".mmf",
                ".mod",
                ".mogg",
                ".mp2",
                ".mp3",
                ".mpa",
                ".mpc",
                ".mpp",
                ".mp+",
                ".msv",
                ".nmf",
                ".nsf",
                ".nsv",
                ".oga",
                ".ogg",
                ".okt",
                ".opus",
                ".pls",
                ".ra",
                ".rf64",
                ".rm",
                ".s3m",
                ".sfx",
                ".shn",
                ".sid",
                ".stm",
                ".strm",
                ".ult",
                ".uni",
                ".vox",
                ".wav",
                ".wma",
                ".wv",
                ".xm",
                ".xsp",
                ".ymf"
            };

            MediaFlagDelimiters = new[]
            {
                '.'
            };

            MediaForcedFlags = new[]
            {
                "foreign",
                "forced"
            };

            MediaDefaultFlags = new[]
            {
                "default"
            };

            MediaHearingImpairedFlags = new[]
            {
                "cc",
                "hi",
                "sdh"
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
                // <!-- foo.E01., foo.e01. -->
                new EpisodeExpression(@"[^\\/]*?()\.?[Ee]([0-9]+)\.([^\\/]*)$"),
                new EpisodeExpression("(?<year>[0-9]{4})[._ -](?<month>[0-9]{2})[._ -](?<day>[0-9]{2})", true)
                {
                    DateTimeFormats = new[]
                    {
                        "yyyy.MM.dd",
                        "yyyy-MM-dd",
                        "yyyy_MM_dd",
                        "yyyy MM dd"
                    }
                },
                new EpisodeExpression("(?<day>[0-9]{2})[._ -](?<month>[0-9]{2})[._ -](?<year>[0-9]{4})", true)
                {
                    DateTimeFormats = new[]
                    {
                        "dd.MM.yyyy",
                        "dd-MM-yyyy",
                        "dd_MM_yyyy",
                        "dd MM yyyy"
                    }
                },

                // This isn't a Kodi naming rule, but the expression below causes false episode numbers for
                // Title Season X Episode X naming schemes.
                // "Series Season X Episode X - Title.avi", "Series S03 E09.avi", "s3 e9 - Title.avi"
                new EpisodeExpression(@".*[\\\/]((?<seriesname>[^\\/]+?)\s)?[Ss](?:eason)?\s*(?<seasonnumber>[0-9]+)\s+[Ee](?:pisode)?\s*(?<epnumber>[0-9]+).*$")
                {
                    IsNamed = true
                },

                // Not a Kodi rule as well, but the expression below also causes false positives,
                // so we make sure this one gets tested first.
                // "Foo Bar 889"
                new EpisodeExpression(@".*[\\\/](?![Ee]pisode)(?<seriesname>[\w\s]+?)\s(?<epnumber>[0-9]{1,4})(-(?<endingepnumber>[0-9]{2,4}))*[^\\\/x]*$")
                {
                    IsNamed = true
                },

                new EpisodeExpression(@"[\\\/\._ \[\(-]([0-9]+)x([0-9]+(?:(?:[a-i]|\.[1-9])(?![0-9]))?)([^\\\/]*)$")
                {
                    SupportsAbsoluteEpisodeNumbers = true
                },

                // Not a Kodi rule as well, but below rule also causes false positives for triple-digit episode names
                // [bar] Foo - 1 [baz] special case of below expression to prevent false positives with digits in the series name
                new EpisodeExpression(@".*[\\\/]?.*?(\[.*?\])+.*?(?<seriesname>[-\w\s]+?)[\s_]*-[\s_]*(?<epnumber>[0-9]+).*$")
                {
                    IsNamed = true
                },

                // /server/anything_102.mp4
                // /server/james.corden.2017.04.20.anne.hathaway.720p.hdtv.x264-crooks.mkv
                // /server/anything_1996.11.14.mp4
                new EpisodeExpression(@"[\\/._ -](?<seriesname>(?![0-9]+[0-9][0-9])([^\\\/_])*)[\\\/._ -](?<seasonnumber>[0-9]+)(?<epnumber>[0-9][0-9](?:(?:[a-i]|\.[1-9])(?![0-9]))?)([._ -][^\\\/]*)$")
                {
                    IsOptimistic = true,
                    IsNamed = true,
                    SupportsAbsoluteEpisodeNumbers = false
                },
                new EpisodeExpression(@"[\/._ -]p(?:ar)?t[_. -]()([ivx]+|[0-9]+)([._ -][^\/]*)$")
                {
                    SupportsAbsoluteEpisodeNumbers = true
                },

                // *** End Kodi Standard Naming

                // "Episode 16", "Episode 16 - Title"
                new EpisodeExpression(@"[Ee]pisode (?<epnumber>[0-9]+)(-(?<endingepnumber>[0-9]+))?[^\\\/]*$")
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
                new EpisodeExpression("([0-9]+)-([0-9]+)"),

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

                // Series and season only expression
                // "the show/season 1", "the show/s01"
                new EpisodeExpression(@"(.*(\\|\/))*(?<seriesname>.+)\/[Ss](eason)?[\. _\-]*(?<seasonnumber>[0-9]+)")
                {
                    IsNamed = true
                },

                // Series and season only expression
                // "the show S01", "the show season 1"
                new EpisodeExpression(@"(.*(\\|\/))*(?<seriesname>.+)[\. _\-]+[sS](eason)?[\. _\-]*(?<seasonnumber>[0-9]+)")
                {
                    IsNamed = true
                },

                // Anime style expression
                // "[Group][Series Name][21][1080p][FLAC][HASH]"
                // "[Group] Series Name [04][BDRIP]"
                new EpisodeExpression(@"(?:\[(?:[^\]]+)\]\s*)?(?<seriesname>\[[^\]]+\]|[^[\]]+)\s*\[(?<epnumber>[0-9]+)\]")
                {
                    IsNamed = true
                },
            };

            VideoExtraRules = new[]
            {
                new ExtraRule(
                    ExtraType.Trailer,
                    ExtraRuleType.DirectoryName,
                    "trailers",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.ThemeVideo,
                    ExtraRuleType.DirectoryName,
                    "backdrops",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.ThemeSong,
                    ExtraRuleType.DirectoryName,
                    "theme-music",
                    MediaType.Audio),

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
                    ExtraType.Short,
                    ExtraRuleType.DirectoryName,
                    "shorts",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Featurette,
                    ExtraRuleType.DirectoryName,
                    "featurettes",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Unknown,
                    ExtraRuleType.DirectoryName,
                    "extras",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Unknown,
                    ExtraRuleType.DirectoryName,
                    "extra",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Unknown,
                    ExtraRuleType.DirectoryName,
                    "other",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Clip,
                    ExtraRuleType.DirectoryName,
                    "clips",
                    MediaType.Video),

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
                    ExtraType.DeletedScene,
                    ExtraRuleType.Suffix,
                    "-deletedscene",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Featurette,
                    ExtraRuleType.Suffix,
                    "-featurette",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Short,
                    ExtraRuleType.Suffix,
                    "-short",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Unknown,
                    ExtraRuleType.Suffix,
                    "-extra",
                    MediaType.Video),

                new ExtraRule(
                    ExtraType.Unknown,
                    ExtraRuleType.Suffix,
                    "-other",
                    MediaType.Video)
            };

            AllExtrasTypesFolderNames = VideoExtraRules
                .Where(i => i.RuleType == ExtraRuleType.DirectoryName)
                .ToDictionary(i => i.Token, i => i.ExtraType, StringComparer.OrdinalIgnoreCase);

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
                "(?<!ch(?:apter) )(?<part>[0-9]+)$",
                // Sometimes named as 0001_005 (chapter_part)
                "(?<chapter>[0-9]+)_(?<part>[0-9]+)",
                // Some audiobooks are ripped from cd's, and will be named by disk number.
                @"dis(?:c|k)[\s_-]?(?<chapter>[0-9]+)"
            };

            AudioBookNamesExpressions = new[]
            {
                // Detect year usually in brackets after name Batman (2020)
                @"^(?<name>.+?)\s*\(\s*(?<year>[0-9]{4})\s*\)\s*$",
                @"^\s*(?<name>[^ ].*?)\s*$"
            };

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

            Compile();
        }

        /// <summary>
        /// Gets or sets the folder name to extra types mapping.
        /// </summary>
        public Dictionary<string, ExtraType> AllExtrasTypesFolderNames { get; set; }

        /// <summary>
        /// Gets or sets list of audio file extensions.
        /// </summary>
        public string[] AudioFileExtensions { get; set; }

        /// <summary>
        /// Gets or sets list of external media flag delimiters.
        /// </summary>
        public char[] MediaFlagDelimiters { get; set; }

        /// <summary>
        /// Gets or sets list of external media forced flags.
        /// </summary>
        public string[] MediaForcedFlags { get; set; }

        /// <summary>
        /// Gets or sets list of external media default flags.
        /// </summary>
        public string[] MediaDefaultFlags { get; set; }

        /// <summary>
        /// Gets or sets list of external media hearing impaired flags.
        /// </summary>
        public string[] MediaHearingImpairedFlags { get; set; }

        /// <summary>
        /// Gets or sets list of album stacking prefixes.
        /// </summary>
        public string[] AlbumStackingPrefixes { get; set; }

        /// <summary>
        /// Gets or sets list of artist subfolders.
        /// </summary>
        public string[] ArtistSubfolders { get; set; }

        /// <summary>
        /// Gets or sets list of subtitle file extensions.
        /// </summary>
        public string[] SubtitleFileExtensions { get; set; }

        /// <summary>
        /// Gets the list of lyric file extensions.
        /// </summary>
        public string[] LyricFileExtensions { get; }

        /// <summary>
        /// Gets or sets list of episode regular expressions.
        /// </summary>
        public EpisodeExpression[] EpisodeExpressions { get; set; }

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
        /// Gets the file stacking rules.
        /// </summary>
        public FileStackRule[] VideoFileStackingRules { get; }

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
        /// Gets list of clean datetime regular expressions.
        /// </summary>
        public Regex[] CleanDateTimeRegexes { get; private set; } = Array.Empty<Regex>();

        /// <summary>
        /// Gets list of clean string regular expressions.
        /// </summary>
        public Regex[] CleanStringRegexes { get; private set; } = Array.Empty<Regex>();

        /// <summary>
        /// Compiles raw regex strings into regexes.
        /// </summary>
        public void Compile()
        {
            CleanDateTimeRegexes = CleanDateTimes.Select(Compile).ToArray();
            CleanStringRegexes = CleanStrings.Select(Compile).ToArray();
        }

        private Regex Compile(string exp)
        {
            return new Regex(exp, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
