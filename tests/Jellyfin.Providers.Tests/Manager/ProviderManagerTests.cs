using System;
using System.Collections.Generic;
using System.Linq;
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
        private static TheoryData<int, bool[]?, int[]?, int[]?, int?[]?, int[]> GetImageProvidersOrderData()
            => new ()
            {
                { 3, null, null, null, null, new[] { 0, 1, 2 } }, // no order options set

                // library options ordering
                { 3, null, Array.Empty<int>(), null, null, new[] { 0, 1, 2 } }, // no order provided
                { 3, null, new[] { 1 }, null, null, new[] { 1, 0, 2 } }, // one item in order
                { 3, null, new[] { 2, 1, 0 }, null, null, new[] { 2, 1, 0 } }, // full reverse order

                // server options ordering
                { 3, null, null, Array.Empty<int>(), null, new[] { 0, 1, 2 } }, // no order provided
                { 3, null, null, new[] { 1 }, null, new[] { 1, 0, 2 } }, // one item in order
                { 3, null, null, new[] { 2, 1, 0 }, null, new[] { 2, 1, 0 } }, // full reverse order

                // IHasOrder ordering
                // TODO unintuitive - default if not IHasOrder is 0, not max
                { 3, null, null, null, new int?[] { null, 0, null }, new[] { 0, 1, 2 } }, // one item with order 0, no change because default order value is 0
                { 3, null, null, null, new int?[] { null, 1, null }, new[] { 0, 2, 1 } }, // one item in order (goes to end, not beginning)
                { 3, null, null, null, new int?[] { 2, 1, 0 }, new[] { 2, 1, 0 } }, // full reverse order

                // multiple orders set
                // TODO should library fall through to server if both are set on different elements?
                { 3, null, new[] { 1 }, new[] { 2, 0, 1 }, null, new[] { 1, 0, 2 } }, // library order first, server order ignored
                { 3, null, new[] { 1 }, null, new int?[] { 2, 0, 1 }, new[] { 1, 2, 0 } }, // library order first, then orderby
                { 3, null, new[] { 2, 1, 0 }, new[] { 1, 2, 0 }, new int?[] { 2, 0, 1 }, new[] { 2, 1, 0 } }, // library order wins

                // ordering with ILocalImageProvider
                // TODO what is the value of testing for ILocalImageProvider on the sort, should this be removed? Behavior is unintuitive
                { 3, new[] { false, true, false }, new[] { 1, 0, 2 }, null, null, new[] { 0, 2, 1 } }, // ILocalImageProvider - sorts to end even when set first
                { 3, new[] { false, true, false }, new[] { 1 }, null, null, new[] { 0, 1, 2 } }, // ILocalImageProvider - set order ignored when only value set
                { 2, new[] { true, true }, new[] { 1, 0 }, null, null, new[] { 0, 1 } }, // ILocalImageProvider - set order ignored
                { 2, new[] { true, true }, null, null, new int?[] { 1, 0 }, new[] { 1, 0 } }, // ILocalImageProvider - IHasOrder applies
            };

        [Theory]
        [MemberData(nameof(GetImageProvidersOrderData))]
        public void GetImageProviders_ProviderOrder_MatchesExpected(int providerCount, bool[]? localImageProvider, int[]? libraryOrder, int[]? serverOrder, int?[]? hasOrderOrder, int[] expectedOrder)
        {
            var item = new Movie();

            var nameProvider = new Func<int, string>(i => "Provider" + i);

            var providerList = new List<IImageProvider>();
            for (var i = 0; i < providerCount; i++)
            {
                var order = hasOrderOrder?[i];
                if (localImageProvider != null && localImageProvider[i])
                {
                    providerList.Add(MockIImageProvider<ILocalImageProvider>(nameProvider(i), item, order));
                }
                else
                {
                    providerList.Add(MockIImageProvider<IImageProvider>(nameProvider(i), item, order));
                }
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

        private static IImageProvider MockIImageProvider<T>(string name, BaseItem supportedType, int? order = null)
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
            provider.Setup(p => p.Supports(supportedType))
                .Returns(true);
            return provider.Object;
        }

        private static ProviderManager GetProviderManager(ServerConfiguration? serverConfiguration = null, LibraryOptions? libraryOptions = null)
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
                null);

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
