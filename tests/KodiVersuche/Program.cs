using System;
using System.IO;
using System.Xml.Serialization;
using Jellyfin.KodiMetadata.Models;

namespace KodiVersuche
{
    class Program
    {
        private const string Filepath = @"C:\Users\david\Desktop\Avengers Endgame (2019).nfo";
        static void Main(string[] args)
        {
            //Deserialize();
            Serialize();
        }

        private static void Deserialize()
        {
            var stream = File.OpenRead(Filepath);

            XmlSerializer serializer = new XmlSerializer(typeof(VideoNfo));

            var obj = serializer.Deserialize(stream);
        }

        private static void Serialize()
        {
            var test = new VideoNfo()
            {
                Title = "Avengers",
                Art = new ArtNfo()
                {
                    Fanart = new []{"uzrl1", "url2"}
                }
            };

            var ser = new XmlSerializer(typeof(VideoNfo));
            ser.Serialize(Console.Out, test);
            Console.WriteLine();
        }
    }
}
