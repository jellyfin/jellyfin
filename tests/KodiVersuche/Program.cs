using System;
using System.IO;
using System.Xml.Serialization;
using Jellyfin.KodiMetadata.Models;

namespace KodiVersuche
{
    class Program
    {
        private const string Filepath = @"C:\Users\david\Desktop\sample-movie.nfo";
        static void Main(string[] args)
        {
            Deserialize();
        }

        private static void Deserialize()
        {
            var stream = File.OpenRead(Filepath);

            XmlSerializer serializer = new XmlSerializer(typeof(MovieNfo));

            var obj = serializer.Deserialize(stream);
        }

        private static void Serialize()
        {
            var test = new MovieNfo()
            {
                Title = "Avengers",
                Ratings = new RatingNfo[] {
                    new RatingNfo{
                        Name = "IMDB",
                        Value = 5,
                        Max = 10,
                        Default = true,
                        Votes = 500
                    },
                    new RatingNfo{
                        Name = "rottentomatoes",
                        Value = 100,
                        Max = 100,
                        Default = false,
                        Votes = 1000
                    }
                }
            };

            var ser = new XmlSerializer(typeof(MovieNfo));
            ser.Serialize(Console.Out, test);
            Console.WriteLine();
        }
    }
}
