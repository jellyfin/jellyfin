using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Emby.XmlTv.Classes;
using Emby.XmlTv.Console.Classes;
using Emby.XmlTv.Entities;

namespace Emby.XmlTv.Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            var filename = @"C:\Temp\QLD.GoldCoast.xml";

            if (args.Length == 1 && File.Exists(args[0]))
            {
                filename = args[0];
            }

            var timer = Stopwatch.StartNew();
            System.Console.WriteLine("Running XMLTv Parsing");

            var resultsFile = String.Format("C:\\Temp\\{0}_Results_{1:HHmmss}.txt", 
                Path.GetFileNameWithoutExtension(filename),
                DateTimeOffset.UtcNow);

            System.Console.Write("Enter the language required: ");
            var lang = System.Console.ReadLine();

            ReadSourceXmlTvFile(filename, resultsFile, lang).Wait();

            System.Console.WriteLine("Completed in {0:g} - press any key to open the file...", timer.Elapsed);
            System.Console.ReadKey();

            Process.Start(resultsFile);
        }

        public static async Task ReadSourceXmlTvFile(string filename, string resultsFile, string lang)
        {
            System.Console.WriteLine("Writing to file: {0}", resultsFile);

            using (var resultsFileStream = new StreamWriter(resultsFile) { AutoFlush = true })
            {
                var reader = new XmlTvReader(filename, lang);
                await ReadOutChannels(reader, resultsFileStream);

                resultsFileStream.Close();
            }
        }

        public static async Task ReadOutChannels(XmlTvReader reader, StreamWriter resultsFileStream)
        {
            var channels = reader.GetChannels().Distinct().ToList();

            resultsFileStream.Write(EntityExtensions.GetHeader("Channels"));

            foreach (var channel in channels)
            {
                System.Console.WriteLine("Retrieved Channel: {0} - {1}", channel.Id, channel.DisplayName);
                resultsFileStream.Write(channel.GetChannelDetail());
            }

            var totalProgrammeCount = 0;

            resultsFileStream.Write("\r\n");
            foreach (var channel in channels)
            {
                System.Console.WriteLine("Processing Channel: {0}", channel.DisplayName);

                resultsFileStream.Write(EntityExtensions.GetHeader("Programs for " + channel.DisplayName));
                var channelProgrammeCount = await ReadOutChannelProgrammes(reader, channel, resultsFileStream);

                totalProgrammeCount += channelProgrammeCount;
                await resultsFileStream.WriteLineAsync(String.Format("Total Programmes for {1}: {0}", channelProgrammeCount, channel.DisplayName));
            }

            await resultsFileStream.WriteLineAsync(String.Format("Total Programmes: {0}", totalProgrammeCount));
        }

        private static async Task<int> ReadOutChannelProgrammes(XmlTvReader reader, XmlTvChannel channel, StreamWriter resultsFileStream)
        {
            //var startDate = new DateTime(2015, 11, 28);
            //var endDate = new DateTime(2015, 11, 29);
            var startDate = DateTimeOffset.MinValue;
            var endDate = DateTimeOffset.MaxValue;

            var count = 0;

            foreach (var programme in reader.GetProgrammes(channel.Id, startDate, endDate, new CancellationToken()).Distinct())
            {
                count++;
                await resultsFileStream.WriteLineAsync(programme.GetProgrammeDetail(channel));
            }

            return count;
        }
    }
}