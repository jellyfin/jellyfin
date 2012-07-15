using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;
using System.IO;
using MediaBrowser.Common.Json;

namespace MediaBrowser.Common.Model
{
    public class DictionaryBaseItem : BaseItem
    {
        private Dictionary<string, object> Dictionary { get; set; }

        public DictionaryBaseItem(Dictionary<string, object> dictionary)
        {
            Dictionary = dictionary;
        }

        public override string Name
        {
            get
            {
                return GetString("Name");
            }
            set
            {
                SetValue("Name", value);
            }
        }

        public override string ArtImagePath
        {
            get
            {
                return GetString("ArtImagePath");
            }
            set
            {
                SetValue("ArtImagePath", value);
            }
        }

        public override string AspectRatio
        {
            get
            {
                return GetString("AspectRatio");
            }
            set
            {
                SetValue("AspectRatio", value);
            }
        }

        public override string BannerImagePath
        {
            get
            {
                return GetString("BannerImagePath");
            }
            set
            {
                SetValue("BannerImagePath", value);
            }
        }

        public override string CustomPin
        {
            get
            {
                return GetString("CustomPin");
            }
            set
            {
                SetValue("CustomPin", value);
            }
        }

        public override string CustomRating
        {
            get
            {
                return GetString("CustomRating");
            }
            set
            {
                SetValue("CustomRating", value);
            }
        }

        public override string DisplayMediaType
        {
            get
            {
                return GetString("DisplayMediaType");
            }
            set
            {
                SetValue("DisplayMediaType", value);
            }
        }

        public override string LogoImagePath
        {
            get
            {
                return GetString("LogoImagePath");
            }
            set
            {
                SetValue("LogoImagePath", value);
            }
        }

        public override string OfficialRating
        {
            get
            {
                return GetString("OfficialRating");
            }
            set
            {
                SetValue("OfficialRating", value);
            }
        }

        public override string Overview
        {
            get
            {
                return GetString("Overview");
            }
            set
            {
                SetValue("Overview", value);
            }
        }

        public override string Path
        {
            get
            {
                return GetString("Path");
            }
            set
            {
                SetValue("Path", value);
            }
        }

        public override string PrimaryImagePath
        {
            get
            {
                return GetString("PrimaryImagePath");
            }
            set
            {
                SetValue("PrimaryImagePath", value);
            }
        }

        public override string SortName
        {
            get
            {
                return GetString("SortName");
            }
            set
            {
                SetValue("SortName", value);
            }
        }

        public override string Tagline
        {
            get
            {
                return GetString("Tagline");
            }
            set
            {
                SetValue("Tagline", value);
            }
        }

        public override string TrailerUrl
        {
            get
            {
                return GetString("TrailerUrl");
            }
            set
            {
                SetValue("TrailerUrl", value);
            }
        }

        private string GetString(string name)
        {
            return Dictionary[name] as string;
        }

        private void SetValue<T>(string name, T value)
        {
            Dictionary[name] = value;
        }

        public static DictionaryBaseItem FromApiOutput(Stream stream)
        {
            Dictionary<string,object> data = JsonSerializer.DeserializeFromStream<Dictionary<string, object>>(stream);

            if (data.ContainsKey("BaseItem"))
            {
                string baseItem = data["BaseItem"] as string;

                data = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(baseItem);

                return new DictionaryBaseItem(data);
            }

            return new DictionaryBaseItem(data);
        }
    }
}
