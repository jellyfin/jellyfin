using System;
using System.IO;
using System.Linq;
using System.Text;
using Emby.Server.Implementations.Serialization;
using Jellyfin.DlnaProfiles.Profiles;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.DlnaProfiles.Tests
{
    public class ProfileTests
    {
        public virtual string GetValidFilename(string filename)
        {
            var builder = new StringBuilder(filename);

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                builder = builder.Replace(c, ' ');
            }

            return builder.ToString();
        }

        [Theory]
        [InlineData("C:\\ProgramData\\Jellyfin\\Server\\config\\dlna\\test\\")]
        void Export(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var xmlSerializer = new MyXmlSerializer();

            var profiles = typeof(ProfileTests).Assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(DeviceProfile)));

            foreach (var profile in profiles)
            {
                var item = (DefaultProfile?)Activator.CreateInstance(profile);
                if (item != null)
                {
                    var path = Path.Combine(folder, GetValidFilename(item.Name ?? string.Empty) + ".xml");
                    xmlSerializer.SerializeToFile(item, path);
                }
            }
        }
    }
}
