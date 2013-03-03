using System.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Music
{
    class LastfmProviderException : ApplicationException
    {
        public LastfmProviderException(string msg)
            : base(msg)
        {
        }
     
    }
    /// <summary>
    /// Class MovieDbProvider
    /// </summary>
    public abstract class LastfmBaseProvider : BaseMetadataProvider
    {
        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// The name of the local json meta file for this item type
        /// </summary>
        protected string LocalMetaFileName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LastfmBaseProvider" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The Log manager</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public LastfmBaseProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager)
            : base(logManager)
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is MusicArtist;
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// If we save locally, refresh if they delete something
        /// </summary>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return Kernel.Instance.Configuration.SaveLocalMeta;
            }
        }

        protected const string RootUrl = @"http://ws.audioscrobbler.com/2.0/";
        protected static string ApiKey = "7b76553c3eb1d341d642755aecc40a33";

        static readonly Regex[] NameMatches = new[] {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"), // matches "My Movie (2001)" and gives us the name and the year
            new Regex(@"(?<name>.*)") // last resort matches the whole string as the name
        };

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.DontFetchMeta) return false;

            if (Kernel.Instance.Configuration.SaveLocalMeta && HasFileSystemStampChanged(item, providerInfo))
            {
                //If they deleted something from file system, chances are, this item was mis-identified the first time
                item.SetProviderId(MetadataProviders.Musicbrainz, null);
                Logger.Debug("LastfmProvider reports file system stamp change...");
                return true;

            }

            if (providerInfo.LastRefreshStatus == ProviderRefreshStatus.CompletedWithErrors)
            {
                Logger.Debug("LastfmProvider for {0} - last attempt had errors.  Will try again.", item.Path);
                return true;
            }

            var downloadDate = providerInfo.LastRefreshed;

            if (Kernel.Instance.Configuration.MetadataRefreshDays == -1 && downloadDate != DateTime.MinValue)
            {
                return false;
            }

            if (DateTime.Today.Subtract(item.DateCreated).TotalDays > 180 && downloadDate != DateTime.MinValue)
                return false; // don't trigger a refresh data for item that are more than 6 months old and have been refreshed before

            if (DateTime.Today.Subtract(downloadDate).TotalDays < Kernel.Instance.Configuration.MetadataRefreshDays) // only refresh every n days
                return false;


            Logger.Debug("LastfmProvider - " + item.Name + " needs refresh.  Download date: " + downloadDate + " item created date: " + item.DateCreated + " Check for Update age: " + Kernel.Instance.Configuration.MetadataRefreshDays);
            return true;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            if (item.DontFetchMeta)
            {
                Logger.Info("LastfmProvider - Not fetching because requested to ignore " + item.Name);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!Kernel.Instance.Configuration.SaveLocalMeta || !HasLocalMeta(item) || (force && !HasLocalMeta(item)))
            {
                try
                {
                    await FetchData(item, cancellationToken).ConfigureAwait(false);
                    SetLastRefreshed(item, DateTime.UtcNow);
                }
                catch (LastfmProviderException)
                {
                    SetLastRefreshed(item, DateTime.UtcNow, ProviderRefreshStatus.CompletedWithErrors);
                }

                return true;
            }
            Logger.Debug("LastfmProvider not fetching because local meta exists for " + item.Name);
            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Determines whether [has local meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local meta] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(BaseItem item)
        {
            return item.ResolveArgs.ContainsMetaFileByName(LocalMetaFileName);
        }

        /// <summary>
        /// Fetches the items data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task.</returns>
        protected abstract Task FetchData(BaseItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Parses the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="justName">Name of the just.</param>
        /// <param name="year">The year.</param>
        protected void ParseName(string name, out string justName, out int? year)
        {
            justName = null;
            year = null;
            foreach (var re in NameMatches)
            {
                Match m = re.Match(name);
                if (m.Success)
                {
                    justName = m.Groups["name"].Value.Trim();
                    string y = m.Groups["year"] != null ? m.Groups["year"].Value : null;
                    int temp;
                    year = Int32.TryParse(y, out temp) ? temp : (int?)null;
                    break;
                }
            }
        }

        /// <summary>
        /// Encodes an URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        protected static string UrlEncode(string name)
        {
            return WebUtility.UrlEncode(name);
        }

        /// <summary>
        /// The remove
        /// </summary>
        const string remove = "\"'!`?";
        // "Face/Off" support.
        /// <summary>
        /// The spacers
        /// </summary>
        const string spacers = "/,.:;\\(){}[]+-_=–*";  // (there are not actually two - in the they are different char codes)
        /// <summary>
        /// The replace start numbers
        /// </summary>
        static readonly Dictionary<string, string> ReplaceStartNumbers = new Dictionary<string, string> {
            {"1 ","one "},
            {"2 ","two "},
            {"3 ","three "},
            {"4 ","four "},
            {"5 ","five "},
            {"6 ","six "},
            {"7 ","seven "},
            {"8 ","eight "},
            {"9 ","nine "},
            {"10 ","ten "},
            {"11 ","eleven "},
            {"12 ","twelve "},
            {"13 ","thirteen "},
            {"100 ","one hundred "},
            {"101 ","one hundred one "}
        };

        /// <summary>
        /// The replace end numbers
        /// </summary>
        static readonly Dictionary<string, string> ReplaceEndNumbers = new Dictionary<string, string> {
            {" 1"," i"},
            {" 2"," ii"},
            {" 3"," iii"},
            {" 4"," iv"},
            {" 5"," v"},
            {" 6"," vi"},
            {" 7"," vii"},
            {" 8"," viii"},
            {" 9"," ix"},
            {" 10"," x"}
        };

        /// <summary>
        /// Gets the name of the comparable.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>System.String.</returns>
        internal static string GetComparableName(string name, ILogger logger)
        {
            name = name.ToLower();
            name = name.Replace("á", "a");
            name = name.Replace("é", "e");
            name = name.Replace("í", "i");
            name = name.Replace("ó", "o");
            name = name.Replace("ú", "u");
            name = name.Replace("ü", "u");
            name = name.Replace("ñ", "n");
            foreach (var pair in ReplaceStartNumbers)
            {
                if (name.StartsWith(pair.Key))
                {
                    name = name.Remove(0, pair.Key.Length);
                    name = pair.Value + name;
                    logger.Info("MovieDbProvider - Replaced Start Numbers: " + name);
                }
            }
            foreach (var pair in ReplaceEndNumbers)
            {
                if (name.EndsWith(pair.Key))
                {
                    name = name.Remove(name.IndexOf(pair.Key), pair.Key.Length);
                    name = name + pair.Value;
                    logger.Info("MovieDbProvider - Replaced End Numbers: " + name);
                }
            }
            name = name.Normalize(NormalizationForm.FormKD);
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (c >= 0x2B0 && c <= 0x0333)
                {
                    // skip char modifier and diacritics 
                }
                else if (remove.IndexOf(c) > -1)
                {
                    // skip chars we are removing
                }
                else if (spacers.IndexOf(c) > -1)
                {
                    sb.Append(" ");
                }
                else if (c == '&')
                {
                    sb.Append(" and ");
                }
                else
                {
                    sb.Append(c);
                }
            }
            name = sb.ToString();
            name = name.Replace(", the", "");
            name = name.Replace(" the ", " ");
            name = name.Replace("the ", "");

            string prevName;
            do
            {
                prevName = name;
                name = name.Replace("  ", " ");
            } while (name.Length != prevName.Length);

            return name.Trim();
        }

    }
}
