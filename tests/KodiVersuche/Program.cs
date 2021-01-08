using System.IO;
using System.Xml.Serialization;
using Jellyfin.KodiMetadata.Models;

namespace KodiVersuche
{
    class Program
    {
        private const string Filepath = @"C:\Users\david\Desktop\movie.nfo";
        static void Main(string[] args)
        {
            var stream = File.OpenRead(Filepath);

            XmlSerializer serializer = new XmlSerializer(typeof(MovieNfo));

            var obj = serializer.Deserialize(stream);
        }
    }
}
