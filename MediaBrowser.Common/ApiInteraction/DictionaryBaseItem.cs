using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Json;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;
using System.Linq;

namespace MediaBrowser.Common.ApiInteraction
{
    public class DictionaryBaseItem : BaseItem
    {
        private Dictionary<string, object> Dictionary { get; set; }

        public UserItemData UserItemData { get; set; }
        public IEnumerable<DictionaryBaseItem> Children { get; set; }

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

        public override DateTime DateCreated
        {
            get
            {
                return GetDateTime("DateCreated");
            }
            set
            {
                SetValue("DateCreated", value);
            }
        }

        public override DateTime DateModified
        {
            get
            {
                return GetDateTime("DateModified");
            }
            set
            {
                SetValue("DateModified", value);
            }
        }

        public override float? UserRating
        {
            get
            {
                return GetNullableFloat("UserRating");
            }
            set
            {
                SetValue("UserRating", value);
            }
        }

        public override string ThumbnailImagePath
        {
            get
            {
                return GetString("ThumbnailImagePath");
            }
            set
            {
                SetValue("ThumbnailImagePath", value);
            }
        }

        public override int? ProductionYear
        {
            get
            {
                return GetNullableInt("ProductionYear");
            }
            set
            {
                SetValue("ProductionYear", value);
            }
        }

        public override TimeSpan? RunTime
        {
            get
            {
                return GetNullableTimeSpan("RunTime");
            }
            set
            {
                SetValue("RunTime", value);
            }
        }

        public bool IsFolder
        {
            get
            {
                return GetBool("IsFolder");
            }
        }

        public override Guid Id
        {
            get
            {
                return GetGuid("Id");
            }
            set
            {
                SetValue("Id", value);
            }
        }

        public TimeSpan? GetNullableTimeSpan(string name)
        {
            string val = Dictionary[name] as string;

            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return TimeSpan.Parse(val);
        }

        public int? GetNullableInt(string name)
        {
            string val = Dictionary[name] as string;

            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return int.Parse(val);
        }

        public float? GetNullableFloat(string name)
        {
            string val = Dictionary[name] as string;

            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return float.Parse(val);
        }

        public DateTime? GetNullableDateTime(string name)
        {
            string val = Dictionary[name] as string;

            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return DateTime.Parse(val);
        }

        public DateTime GetDateTime(string name)
        {
            DateTime? val = GetNullableDateTime(name);

            return val ?? DateTime.MinValue;
        }

        public bool? GetNullableBool(string name)
        {
            string val = Dictionary[name] as string;

            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val != "false";
        }

        public Guid GetGuid(string name)
        {
            string val = GetString(name);

            if (string.IsNullOrEmpty(val))
            {
                return Guid.Empty;
            }

            return Guid.Parse(val);
        }

        public bool GetBool(string name)
        {
            bool? val = GetNullableBool(name);

            return val ?? false;
        }

        public string GetString(string name)
        {
            return Dictionary[name] as string;
        }

        private void SetValue<T>(string name, T value)
        {
            Dictionary[name] = value;
        }

        public static DictionaryBaseItem FromApiOutput(Stream stream)
        {
            Dictionary<string, object> data = JsonSerializer.DeserializeFromStream<Dictionary<string, object>>(stream);

            string baseItem = data["Item"] as string;

            DictionaryBaseItem item = new DictionaryBaseItem(JsonSerializer.DeserializeFromString<Dictionary<string, object>>(baseItem));

            if (data.ContainsKey("UserItemData"))
            {
                item.UserItemData = JsonSerializer.DeserializeFromString<UserItemData>(data["UserItemData"].ToString());
            }

            if (data.ContainsKey("Children"))
            {
                item.Children = JsonSerializer.DeserializeFromString<IEnumerable<Dictionary<string, object>>>(data["Children"].ToString()).Select(c => new DictionaryBaseItem(c));
            }

            return item;
        }
    }
}
