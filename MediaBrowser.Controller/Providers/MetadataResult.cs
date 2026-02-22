#nullable disable

#pragma warning disable CA1002, CA2227, CS1591

using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class MetadataResult<T>
    {
        // Images aren't always used so the allocation is a waste a lot of the time
        private List<LocalImageInfo> _images;
        private List<(string Url, ImageType Type)> _remoteImages;
        private List<PersonInfo> _people;

        public MetadataResult()
        {
            ResultLanguage = "en";
        }

        public List<LocalImageInfo> Images
        {
            get => _images ??= [];
            set => _images = value;
        }

        public List<(string Url, ImageType Type)> RemoteImages
        {
            get => _remoteImages ??= [];
            set => _remoteImages = value;
        }

        public IReadOnlyList<PersonInfo> People
        {
            get => _people;
            set => _people = value?.ToList();
        }

        public bool HasMetadata { get; set; }

        public T Item { get; set; }

        public string ResultLanguage { get; set; }

        public string Provider { get; set; }

        public bool QueriedById { get; set; }

        public void AddPerson(PersonInfo p)
        {
            People ??= new List<PersonInfo>();

            PeopleHelper.AddPerson(_people, p);
        }

        /// <summary>
        /// Not only does this clear, but initializes the list so that services can differentiate between a null list and zero people.
        /// </summary>
        public void ResetPeople()
        {
            if (People is null)
            {
                People = new List<PersonInfo>();
            }
            else
            {
                _people.Clear();
            }
        }
    }
}
