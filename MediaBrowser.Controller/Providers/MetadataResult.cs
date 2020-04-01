using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class MetadataResult<T>
    {
        public List<LocalImageInfo> Images { get; set; }

        public List<UserItemData> UserDataList { get; set; }

        public MetadataResult()
        {
            Images = new List<LocalImageInfo>();
            ResultLanguage = "en";
        }

        public List<PersonInfo> People { get; set; }

        public bool HasMetadata { get; set; }

        public T Item { get; set; }

        public string ResultLanguage { get; set; }

        public string Provider { get; set; }

        public bool QueriedById { get; set; }

        public void AddPerson(PersonInfo p)
        {
            if (People == null)
            {
                People = new List<PersonInfo>();
            }

            PeopleHelper.AddPerson(People, p);
        }

        /// <summary>
        /// Not only does this clear, but initializes the list so that services can differentiate between a null list and zero people
        /// </summary>
        public void ResetPeople()
        {
            if (People == null)
            {
                People = new List<PersonInfo>();
            }
            People.Clear();
        }

        public UserItemData GetOrAddUserData(string userId)
        {
            if (UserDataList == null)
            {
                UserDataList = new List<UserItemData>();
            }

            UserItemData userData = null;

            foreach (var i in UserDataList)
            {
                if (string.Equals(userId, i.UserId.ToString("N", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                {
                    userData = i;
                }
            }

            if (userData == null)
            {
                userData = new UserItemData()
                {
                    UserId = new Guid(userId)
                };

                UserDataList.Add(userData);
            }

            return userData;
        }
    }
}
