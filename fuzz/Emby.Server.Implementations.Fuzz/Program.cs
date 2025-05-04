using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using SharpFuzz;

namespace Emby.Server.Implementations.Fuzz
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            switch (args[0])
            {
                case "PathExtensions.TryReplaceSubPath": Run(PathExtensions_TryReplaceSubPath); return;
                case "SqliteItemRepository.ItemImageInfoFromValueString": Run(SqliteItemRepository_ItemImageInfoFromValueString); return;
                default: throw new ArgumentException($"Unknown fuzzing function: {args[0]}");
            }
        }

        private static void Run(Action<string> action) => Fuzzer.OutOfProcess.Run(action);

        private static void PathExtensions_TryReplaceSubPath(string data)
        {
            // Stupid, but it worked
            var parts = data.Split(':');
            if (parts.Length != 3)
            {
                return;
            }

            _ = PathExtensions.TryReplaceSubPath(parts[0], parts[1], parts[2], out _);
        }

        private static void SqliteItemRepository_ItemImageInfoFromValueString(string data)
        {
            var sqliteItemRepository = MockSqliteItemRepository();
            sqliteItemRepository.ItemImageInfoFromValueString(data);
        }

        private static SqliteItemRepository MockSqliteItemRepository()
        {
            const string VirtualMetaDataPath = "%MetadataPath%";
            const string MetaDataPath = "/meta/data/path";

            var appHost = new Mock<IServerApplicationHost>();
            appHost.Setup(x => x.ExpandVirtualPath(It.IsAny<string>()))
                .Returns((string x) => x.Replace(VirtualMetaDataPath, MetaDataPath, StringComparison.Ordinal));
            appHost.Setup(x => x.ReverseVirtualPath(It.IsAny<string>()))
                .Returns((string x) => x.Replace(MetaDataPath, VirtualMetaDataPath, StringComparison.Ordinal));

            var configSection = new Mock<IConfigurationSection>();
            configSection.SetupGet(x => x[It.Is<string>(s => s == MediaBrowser.Controller.Extensions.ConfigurationExtensions.SqliteCacheSizeKey)])
                .Returns("0");
            var config = new Mock<IConfiguration>();
            config.Setup(x => x.GetSection(It.Is<string>(s => s == MediaBrowser.Controller.Extensions.ConfigurationExtensions.SqliteCacheSizeKey)))
                .Returns(configSection.Object);

            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            fixture.Inject(appHost);
            fixture.Inject(config);
            return fixture.Create<SqliteItemRepository>();
        }
    }
}
