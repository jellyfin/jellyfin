using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.BaseItemManager;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Manager
{
    public class ProviderManagerTests
    {
        private static TheoryData<int, int[]?, int[]?, int?[]?, int[]> GetImageProvidersOrderData()
            => new ()
            {
                { 3, null, null, null, new[] { 0, 1, 2 } }, // no order options set

                // library options ordering
                { 3, Array.Empty<int>(), null, null, new[] { 0, 1, 2 } }, // no order provided
                { 3, new[] { 1 }, null, null, new[] { 1, 0, 2 } }, // one item in order
                { 3, new[] { 2, 1, 0 }, null, null, new[] { 2, 1, 0 } }, // full reverse order

                // server options ordering
                { 3, null, Array.Empty<int>(), null, new[] { 0, 1, 2 } }, // no order provided
                { 3, null, new[] { 1 }, null, new[] { 1, 0, 2 } }, // one item in order
                { 3, null, new[] { 2, 1, 0 }, null, new[] { 2, 1, 0 } }, // full reverse order

                // IHasOrder ordering
                { 3, null, null, new int?[] { null, 1, null }, new[] { 1, 0, 2 } }, // one item with defined order
                { 3, null, null, new int?[] { 2, 1, 0 }, new[] { 2, 1, 0 } }, // full reverse order

                // multiple orders set
                { 3, new[] { 1 }, new[] { 2, 0, 1 }, null, new[] { 1, 0, 2 } }, // library order first, server order ignored
                { 3, new[] { 1 }, null, new int?[] { 2, 0, 1 }, new[] { 1, 2, 0 } }, // library order first, then orderby
                { 3, new[] { 2, 1, 0 }, new[] { 1, 2, 0 }, new int?[] { 2, 0, 1 }, new[] { 2, 1, 0 } }, // library order wins
            };

        [Theory]
        [MemberData(nameof(GetImageProvidersOrderData))]
        public void GetImageProviders_ProviderOrder_MatchesExpected(int providerCount, int[]? libraryOrder, int[]? serverOrder, int?[]? hasOrderOrder, int[] expectedOrder)
        {
            var item = new Movie();

            var nameProvider = new Func<int, string>(i => "Provider" + i);

            var providerList = new List<IImageProvider>();
            for (var i = 0; i < providerCount; i++)
            {
                var order = hasOrderOrder?[i];
                providerList.Add(MockIImageProvider<IImageProvider>(nameProvider(i), item, order: order));
            }

            var libraryOptions = new LibraryOptions();
            if (libraryOrder != null)
            {
                libraryOptions.TypeOptions = new[]
                {
                    new TypeOptions
                    {
                        Type = item.GetType().Name,
                        ImageFetcherOrder = libraryOrder.Select(nameProvider).ToArray()
                    }
                };
            }

            var serverConfiguration = new ServerConfiguration();
            if (serverOrder != null)
            {
                serverConfiguration.MetadataOptions = new[]
                {
                    new MetadataOptions
                    {
                        ItemType = item.GetType().Name,
                        ImageFetcherOrder = serverOrder.Select(nameProvider).ToArray()
                    }
                };
            }

            var providerManager = GetProviderManager(serverConfiguration: serverConfiguration, libraryOptions: libraryOptions);
            AddParts(providerManager, imageProviders: providerList);

            var refreshOptions = new ImageRefreshOptions(Mock.Of<IDirectoryService>(MockBehavior.Strict));
            var actualProviders = providerManager.GetImageProviders(item, refreshOptions).ToList();

            Assert.Equal(providerList.Count, actualProviders.Count);
            for (var i = 0; i < providerList.Count; i++)
            {
                Assert.Equal(i, actualProviders.IndexOf(providerList[expectedOrder[i]]));
            }
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        public void GetImageProviders_CanRefreshImagesBasic_WhenSupportsWithoutError(bool supports, bool errorOnSupported, bool expected)
        {
            GetImageProviders_CanRefreshImages_Tester(typeof(IImageProvider), supports, expected, errorOnSupported: errorOnSupported);
        }

        [Theory]
        [InlineData(typeof(ILocalImageProvider), false, true)]
        [InlineData(typeof(ILocalImageProvider), true, true)]
        [InlineData(typeof(IImageProvider), false, false)]
        [InlineData(typeof(IImageProvider), true, true)]
        public void GetImageProviders_CanRefreshImagesLocked_WhenLocalOrFullRefresh(Type providerType, bool fullRefresh, bool expected)
        {
            GetImageProviders_CanRefreshImages_Tester(providerType, true, expected, itemLocked: true, fullRefresh: fullRefresh);
        }

        [Theory]
        [InlineData(typeof(ILocalImageProvider), false, true)]
        [InlineData(typeof(IRemoteImageProvider), true, true)]
        [InlineData(typeof(IDynamicImageProvider), true, true)]
        [InlineData(typeof(IRemoteImageProvider), false, false)]
        [InlineData(typeof(IDynamicImageProvider), false, false)]
        public void GetImageProviders_CanRefreshImagesEnabled_WhenLocalOrEnabled(Type providerType, bool enabled, bool expected)
        {
            GetImageProviders_CanRefreshImages_Tester(providerType, true, expected, baseItemEnabled: enabled);
        }

        private static void GetImageProviders_CanRefreshImages_Tester(Type providerType, bool supports, bool expected, bool errorOnSupported = false, bool itemLocked = false, bool fullRefresh = false, bool baseItemEnabled = true)
        {
            var item = new Movie
            {
                IsLocked = itemLocked
            };

            var providerName = "provider";
            IImageProvider provider = providerType.Name switch
            {
                "IImageProvider" => MockIImageProvider<IImageProvider>(providerName, item, supports: supports, errorOnSupported: errorOnSupported),
                "ILocalImageProvider" => MockIImageProvider<ILocalImageProvider>(providerName, item, supports: supports, errorOnSupported: errorOnSupported),
                "IRemoteImageProvider" => MockIImageProvider<IRemoteImageProvider>(providerName, item, supports: supports, errorOnSupported: errorOnSupported),
                "IDynamicImageProvider" => MockIImageProvider<IDynamicImageProvider>(providerName, item, supports: supports, errorOnSupported: errorOnSupported),
                _ => throw new ArgumentException("Unexpected provider type")
            };

            var refreshOptions = new ImageRefreshOptions(Mock.Of<IDirectoryService>(MockBehavior.Strict))
            {
                ImageRefreshMode = fullRefresh ? MetadataRefreshMode.FullRefresh : MetadataRefreshMode.Default
            };

            var baseItemManager = new Mock<IBaseItemManager>(MockBehavior.Strict);
            baseItemManager.Setup(i => i.IsImageFetcherEnabled(item, It.IsAny<LibraryOptions>(), providerName))
                .Returns(baseItemEnabled);

            var providerManager = GetProviderManager(baseItemManager: baseItemManager.Object);
            AddParts(providerManager, imageProviders: new[] { provider });

            var actualProviders = providerManager.GetImageProviders(item, refreshOptions);

            if (expected)
            {
                Assert.Single(actualProviders);
            }
            else
            {
                Assert.Empty(actualProviders);
            }
        }

        private static IImageProvider MockIImageProvider<T>(string name, BaseItem expectedType, bool supports = true, int? order = null, bool errorOnSupported = false)
            where T : class, IImageProvider
        {
            Mock<IHasOrder>? hasOrder = null;
            if (order != null)
            {
                hasOrder = new Mock<IHasOrder>(MockBehavior.Strict);
                hasOrder.Setup(i => i.Order)
                    .Returns((int)order);
            }

            var provider = hasOrder == null
                ? new Mock<T>(MockBehavior.Strict)
                : hasOrder.As<T>();
            provider.Setup(p => p.Name)
                .Returns(name);
            if (errorOnSupported)
            {
                provider.Setup(p => p.Supports(It.IsAny<BaseItem>()))
                    .Throws(new ArgumentException());
            }
            else
            {
                provider.Setup(p => p.Supports(expectedType))
                    .Returns(supports);
            }

            return provider.Object;
        }

        private static ProviderManager GetProviderManager(ServerConfiguration? serverConfiguration = null, LibraryOptions? libraryOptions = null, IBaseItemManager? baseItemManager = null)
        {
            var serverConfigurationManager = new Mock<IServerConfigurationManager>(MockBehavior.Strict);
            serverConfigurationManager.Setup(i => i.Configuration)
                .Returns(serverConfiguration ?? new ServerConfiguration());

            var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
            libraryManager.Setup(i => i.GetLibraryOptions(It.IsAny<BaseItem>()))
                .Returns(libraryOptions ?? new LibraryOptions());

            var providerManager = new ProviderManager(
                null,
                null,
                serverConfigurationManager.Object,
                null,
                new NullLogger<ProviderManager>(),
                null,
                null,
                libraryManager.Object,
                baseItemManager);

            return providerManager;
        }

        private static void AddParts(
            ProviderManager providerManager,
            IEnumerable<IImageProvider>? imageProviders = null,
            IEnumerable<IMetadataService>? metadataServices = null,
            IEnumerable<IMetadataProvider>? metadataProviders = null,
            IEnumerable<IMetadataSaver>? metadataSavers = null,
            IEnumerable<IExternalId>? externalIds = null)
        {
            imageProviders ??= Array.Empty<IImageProvider>();
            metadataServices ??= Array.Empty<IMetadataService>();
            metadataProviders ??= Array.Empty<IMetadataProvider>();
            metadataSavers ??= Array.Empty<IMetadataSaver>();
            externalIds ??= Array.Empty<IExternalId>();

            providerManager.AddParts(imageProviders, metadataServices, metadataProviders, metadataSavers, externalIds);
        }
    }
}
