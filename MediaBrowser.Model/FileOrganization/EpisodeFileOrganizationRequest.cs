using System.Collections.Generic;

namespace MediaBrowser.Model.FileOrganization
{
    public class EpisodeFileOrganizationRequest
    {
        public string ResultId { get; set; }
        
        public string SeriesId { get; set; }

        public int SeasonNumber { get; set; }

        public int EpisodeNumber { get; set; }

        public int? EndingEpisodeNumber { get; set; }

        public bool RememberCorrection { get; set; }
        public string NewSeriesName { get; set; }

        public string NewSeriesYear { get; set; }

        public string NewSeriesProviderIds { get; set; }

        public string TargetFolder { get; set; }

        public Dictionary<string, string> NewSeriesProviderIdsDictionary
        {
            get
            {
                var dic = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(NewSeriesProviderIds))
                {
                    var str = NewSeriesProviderIds.Replace("{", "").Replace("}", "").Replace("\"", "");

                    foreach (var item in str.Split(','))
                    {
                        var itemArr = item.Split(':');
                        if (itemArr.Length > 1)
                        {
                            var key = itemArr[0].Trim();
                            var val = itemArr[1].Trim();
                            dic.Add(key, val);
                        }
                    }
                }

                return dic;
            }
        }
    }
}