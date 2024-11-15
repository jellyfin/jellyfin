using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.BaseItemManager;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

// Allow Moq to see internal class
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Jellyfin.Providers.Tests.Manager
{
    public class ProviderManagerTests
    {
        private static readonly ILogger<ProviderManager> _logger = new NullLogger<ProviderManager>();

        public static TheoryData<Mock<IMetadataService>[], int> RefreshSingleItemOrderData()
            => new()
            {
                // no order set, uses provided order
                {
                    new[]
                    {
                        MockIMetadataService(true, true),
                        MockIMetadataService(true, true)
                    },
                    0
                },
                // sort order sets priority when all match
                {
                    new[]
                    {
                        MockIMetadataService(true, true, 1),
                        MockIMetadataService(true, true, 0),
                        MockIMetadataService(true, true, 2)
                    },
                    1
                },
                // CanRefreshPrimary prioritized
                {
                    new[]
                    {
                        MockIMetadataService(false, true),
                        MockIMetadataService(true, true),
                    },
                    1
                },
                // falls back to CanRefresh
                {
                    new[]
                    {
                        MockIMetadataService(false, false),
                        MockIMetadataService(false, true)
                    },
                    1
                },
            };

        [Theory]
        [MemberData(nameof(RefreshSingleItemOrderData))]
        public async Task RefreshSingleItem_ServiceOrdering_FollowsPriority(Mock<IMetadataService>[] servicesList, int expectedIndex)
        {
            var item = new Movie();

            using var providerManager = GetProviderManager();
            AddParts(providerManager, metadataServices: servicesList.Select(s => s.Object).ToArray());

            var refreshOptions = new MetadataRefreshOptions(Mock.Of<IDirectoryService>(MockBehavior.Strict));
            var actual = await providerManager.RefreshSingleItem(item, refreshOptions, CancellationToken.None);

            Assert.Equal(ItemUpdateType.MetadataDownload, actual);
            for (var i = 0; i < servicesList.Length; i++)
            {
                var times = i == expectedIndex ? Times.Once() : Times.Never();
                servicesList[i].Verify(mock => mock.RefreshMetadata(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()), times);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RefreshSingleItem_RefreshMetadata_WhenServiceFound(bool serviceFound)
        {
            var item = new Movie();

            var servicesList = new[] { MockIMetadataService(false, serviceFound) };

            using var providerManager = GetProviderManager();
            AddParts(providerManager, metadataServices: servicesList.Select(s => s.Object).ToArray());

            var refreshOptions = new MetadataRefreshOptions(Mock.Of<IDirectoryService>(MockBehavior.Strict));
            var actual = await providerManager.RefreshSingleItem(item, refreshOptions, CancellationToken.None);

            var expectedResult = serviceFound ? ItemUpdateType.MetadataDownload : ItemUpdateType.None;
            Assert.Equal(expectedResult, actual);
        }

        public static TheoryData<int, int[]?, int[]?, int?[]?, int[]> GetImageProvidersOrderData()
            => new()
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
                { 3, new[] { 1 }, new[] { 2, 0, 1 }, null, new[] { 1, 0, 2 } }, // partial library order first, server order ignored
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
                providerList.Add(MockIImageProvider<ILocalImageProvider>(nameProvider(i), item, order: order));
            }

            var libraryOptions = CreateLibraryOptions(item.GetType().Name, imageFetcherOrder: libraryOrder?.Select(nameProvider).ToArray());
            var serverConfiguration = CreateServerConfiguration(item.GetType().Name, imageFetcherOrder: serverOrder?.Select(nameProvider).ToArray());

            using var providerManager = GetProviderManager(serverConfiguration: serverConfiguration, libraryOptions: libraryOptions);
            AddParts(providerManager, imageProviders: providerList);

            var refreshOptions = new ImageRefreshOptions(Mock.Of<IDirectoryService>(MockBehavior.Strict));
            var actualProviders = providerManager.GetImageProviders(item, refreshOptions).ToList();

            Assert.Equal(providerList.Count, actualProviders.Count);
            var actualOrder = actualProviders.Select(i => providerList.IndexOf(i)).ToArray();
            Assert.Equal(expectedOrder, actualOrder);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        public void GetImageProviders_CanRefreshImagesBasic_WhenSupportsWithoutError(bool supports, bool errorOnSupported, bool expected)
        {
            GetImageProviders_CanRefreshImages_Tester(nameof(IImageProvider), supports, expected, errorOnSupported: errorOnSupported);
        }

        [Theory]
        [InlineData(nameof(ILocalImageProvider), false, true)]
        [InlineData(nameof(ILocalImageProvider), true, true)]
        [InlineData(nameof(IImageProvider), false, false)]
        [InlineData(nameof(IImageProvider), true, true)]
        public void GetImageProviders_CanRefreshImagesLocked_WhenLocalOrFullRefresh(string providerType, bool fullRefresh, bool expected)
        {
            GetImageProviders_CanRefreshImages_Tester(providerType, true, expected, itemLocked: true, fullRefresh: fullRefresh);
        }

        [Theory]
        [InlineData(nameof(ILocalImageProvider), false, true)]
        [InlineData(nameof(IRemoteImageProvider), true, true)]
        [InlineData(nameof(IDynamicImageProvider), true, true)]
        [InlineData(nameof(IRemoteImageProvider), false, false)]
        [InlineData(nameof(IDynamicImageProvider), false, false)]
        public void GetImageProviders_CanRefreshImagesBaseItemEnabled_WhenLocalOrEnabled(string providerType, bool enabled, bool expected)
        {
            GetImageProviders_CanRefreshImages_Tester(providerType, true, expected, baseItemEnabled: enabled);
        }

        private static void GetImageProviders_CanRefreshImages_Tester(
            string providerType,
            bool supports,
            bool expected,
            bool errorOnSupported = false,
            bool itemLocked = false,
            bool fullRefresh = false,
            bool baseItemEnabled = true)
        {
            var item = new Movie
            {
                IsLocked = itemLocked
            };

            var providerName = "provider";
            IImageProvider provider = providerType switch
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
            baseItemManager.Setup(i => i.IsImageFetcherEnabled(item, It.IsAny<TypeOptions>(), providerName))
                .Returns(baseItemEnabled);

            using var providerManager = GetProviderManager(baseItemManager: baseItemManager.Object);
            AddParts(providerManager, imageProviders: new[] { provider });

            var actualProviders = providerManager.GetImageProviders(item, refreshOptions).ToArray();

            Assert.Equal(expected ? 1 : 0, actualProviders.Length);
        }

        public static TheoryData<string[], int[]?, int[]?, int[]?, int[]?, int?[]?, int[]> GetMetadataProvidersOrderData()
        {
            var l = nameof(ILocalMetadataProvider);
            var r = nameof(IRemoteMetadataProvider);
            return new()
            {
                { new[] { l, l, r, r }, null, null, null, null, null, new[] { 0, 1, 2, 3 } }, // no order options set

                // library options ordering
                { new[] { l, l, r, r }, Array.Empty<int>(), Array.Empty<int>(), null, null, null, new[] { 0, 1, 2, 3 } }, // no order provided
                // local only
                { new[] { r, l, l, l }, new[] { 2 }, null, null, null, null, new[] { 2, 0, 1, 3 } }, // one item in order
                { new[] { r, l, l, l }, new[] { 3, 2, 1 }, null, null, null, null, new[] { 3, 2, 1, 0 } }, // full reverse order
                // remote only
                { new[] { l, r, r, r }, null, new[] { 2 }, null, null, null, new[] { 2, 0, 1, 3 } }, // one item in order
                { new[] { l, r, r, r }, null, new[] { 3, 2, 1 }, null, null, null, new[] { 3, 2, 1, 0 } }, // full reverse order
                // local and remote, note that results will be interleaved (odd but expected)
                { new[] { l, l, r, r }, new[] { 1 }, new[] { 3 }, null, null, null, new[] { 1, 3, 0, 2 } }, // one item in each order
                { new[] { l, l, l, r, r, r }, new[] { 2, 1, 0 }, new[] { 5, 4, 3 }, null, null, null, new[] { 2, 5, 1, 4, 0, 3 } }, // full reverse order

                // // server options ordering
                { new[] { l, l, r, r }, null, null, Array.Empty<int>(), Array.Empty<int>(), null, new[] { 0, 1, 2, 3 } }, // no order provided
                // local only
                { new[] { r, l, l, l }, null, null, new[] { 2 }, null, null, new[] { 2, 0, 1, 3 } }, // one item in order
                { new[] { r, l, l, l }, null, null, new[] { 3, 2, 1 }, null, null, new[] { 3, 2, 1, 0 } }, // full reverse order
                // remote only
                { new[] { l, r, r, r }, null, null, null, new[] { 2 }, null, new[] { 2, 0, 1, 3 } }, // one item in order
                { new[] { l, r, r, r }, null, null, null, new[] { 3, 2, 1 }, null, new[] { 3, 2, 1, 0 } }, // full reverse order
                // local and remote, note that results will be interleaved (odd but expected)
                { new[] { l, l, r, r }, null, null, new[] { 1 }, new[] { 3 }, null, new[] { 1, 3, 0, 2 } }, // one item in each order
                { new[] { l, l, l, r, r, r }, null, null, new[] { 2, 1, 0 }, new[] { 5, 4, 3 }, null, new[] { 2, 5, 1, 4, 0, 3 } }, // full reverse order

                // IHasOrder ordering (not interleaved, doesn't care about types)
                { new[] { l, l, r, r }, null, null, null, null, new int?[] { 2, null, 1, null }, new[] { 2, 0, 1, 3 } }, // partially defined
                { new[] { l, l, r, r }, null, null, null, null, new int?[] { 3, 2, 1, 0 }, new[] { 3, 2, 1, 0 } }, // full reverse order

                // multiple orders set
                { new[] { l, l, l, r, r, r }, new[] { 1 }, new[] { 4 }, new[] { 2, 1, 0 }, new[] { 5, 4, 3 }, null, new[] { 1, 4, 0, 2, 3, 5 } }, // partial library order first, server order ignored
                { new[] { l, l, l }, new[] { 1 }, null, null, null, new int?[] { 2, 0, 1 }, new[] { 1, 2, 0 } }, // library order first, then orderby
                { new[] { l, l, l, r, r, r }, new[] { 2, 1, 0 }, new[] { 5, 4, 3 }, new[] { 1, 2, 0 }, new[] { 4, 5, 3 }, new int?[] { 5, 4, 1, 6, 3, 2 }, new[] { 2, 5, 4, 1, 0, 3 } }, // library order wins (with orderby between local/remote)
            };
        }

        [Theory]
        [MemberData(nameof(GetMetadataProvidersOrderData))]
        public void GetMetadataProviders_ProviderOrder_MatchesExpected(
            string[] providers,
            int[]? libraryLocalOrder,
            int[]? libraryRemoteOrder,
            int[]? serverLocalOrder,
            int[]? serverRemoteOrder,
            int?[]? hasOrderOrder,
            int[] expectedOrder)
        {
            var item = new MetadataTestItem();

            var nameProvider = new Func<int, string>(i => "Provider" + i);

            var providerList = new List<IMetadataProvider<MetadataTestItem>>();
            for (var i = 0; i < providers.Length; i++)
            {
                var order = hasOrderOrder?[i];
                providerList.Add(MockIMetadataProviderMapper<MetadataTestItem, MetadataTestItemInfo>(providers[i], nameProvider(i), order: order));
            }

            var libraryOptions = CreateLibraryOptions(
                item.GetType().Name,
                localMetadataReaderOrder: libraryLocalOrder?.Select(nameProvider).ToArray(),
                metadataFetcherOrder: libraryRemoteOrder?.Select(nameProvider).ToArray());
            var serverConfiguration = CreateServerConfiguration(
                item.GetType().Name,
                localMetadataReaderOrder: serverLocalOrder?.Select(nameProvider).ToArray(),
                metadataFetcherOrder: serverRemoteOrder?.Select(nameProvider).ToArray());

            var baseItemManager = new Mock<IBaseItemManager>(MockBehavior.Strict);
            baseItemManager.Setup(i => i.IsMetadataFetcherEnabled(item, It.IsAny<TypeOptions>(), It.IsAny<string>()))
                .Returns(true);

            using var providerManager = GetProviderManager(serverConfiguration: serverConfiguration, baseItemManager: baseItemManager.Object);
            AddParts(providerManager, metadataProviders: providerList);

            var actualProviders = providerManager.GetMetadataProviders<MetadataTestItem>(item, libraryOptions).ToList();

            Assert.Equal(providerList.Count, actualProviders.Count);
            var actualOrder = actualProviders.Select(i => providerList.IndexOf(i)).ToArray();
            Assert.Equal(expectedOrder, actualOrder);
        }

        [Theory]
        [InlineData(nameof(IMetadataProvider))]
        [InlineData(nameof(ILocalMetadataProvider))]
        [InlineData(nameof(IRemoteMetadataProvider))]
        [InlineData(nameof(ICustomMetadataProvider))]
        public void GetMetadataProviders_CanRefreshMetadataBasic_ReturnsTrue(string providerType)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, true);
        }

        [Theory]
        [InlineData(nameof(ILocalMetadataProvider), false, true)]
        [InlineData(nameof(IRemoteMetadataProvider), false, false)]
        [InlineData(nameof(ICustomMetadataProvider), false, false)]
        [InlineData(nameof(ILocalMetadataProvider), true, true)]
        [InlineData(nameof(ICustomMetadataProvider), true, false)]
        public void GetMetadataProviders_CanRefreshMetadataLocked_WhenLocalOrForced(string providerType, bool forced, bool expected)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, expected, itemLocked: true, providerForced: forced);
        }

        [Theory]
        [InlineData(nameof(ILocalMetadataProvider), false, true)]
        [InlineData(nameof(ICustomMetadataProvider), false, true)]
        [InlineData(nameof(IRemoteMetadataProvider), false, false)]
        [InlineData(nameof(IRemoteMetadataProvider), true, true)]
        public void GetMetadataProviders_CanRefreshMetadataBaseItemEnabled_WhenEnabledOrNotRemote(string providerType, bool baseItemEnabled, bool expected)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, expected, baseItemEnabled: baseItemEnabled);
        }

        [Theory]
        [InlineData(nameof(IRemoteMetadataProvider), false, true)]
        [InlineData(nameof(ICustomMetadataProvider), false, true)]
        [InlineData(nameof(ILocalMetadataProvider), false, false)]
        [InlineData(nameof(ILocalMetadataProvider), true, true)]
        public void GetMetadataProviders_CanRefreshMetadataSupportsLocal_WhenSupportsOrNotLocal(string providerType, bool supportsLocalMetadata, bool expected)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, expected, supportsLocalMetadata: supportsLocalMetadata);
        }

        [Theory]
        [InlineData(nameof(ICustomMetadataProvider), true)]
        [InlineData(nameof(IRemoteMetadataProvider), true)]
        [InlineData(nameof(ILocalMetadataProvider), true)]
        public void GetMetadataProviders_CanRefreshMetadataOwned(string providerType, bool expected)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, expected, ownedItem: true);
        }

        private static void GetMetadataProviders_CanRefreshMetadata_Tester(
            string providerType,
            bool expected,
            bool itemLocked = false,
            bool baseItemEnabled = true,
            bool providerForced = false,
            bool supportsLocalMetadata = true,
            bool ownedItem = false)
        {
            var item = new MetadataTestItem
            {
                IsLocked = itemLocked,
                OwnerId = ownedItem ? Guid.NewGuid() : Guid.Empty,
                EnableLocalMetadata = supportsLocalMetadata
            };

            var providerName = "provider";
            var provider = MockIMetadataProviderMapper<MetadataTestItem, MetadataTestItemInfo>(providerType, providerName, forced: providerForced);

            var baseItemManager = new Mock<IBaseItemManager>(MockBehavior.Strict);
            baseItemManager.Setup(i => i.IsMetadataFetcherEnabled(item, It.IsAny<TypeOptions>(), providerName))
                .Returns(baseItemEnabled);

            using var providerManager = GetProviderManager(baseItemManager: baseItemManager.Object);
            AddParts(providerManager, metadataProviders: new[] { provider });

            var actualProviders = providerManager.GetMetadataProviders<MetadataTestItem>(item, new LibraryOptions()).ToArray();

            Assert.Equal(expected ? 1 : 0, actualProviders.Length);
        }

        private static Mock<IMetadataService> MockIMetadataService(bool refreshPrimary, bool canRefresh, int order = 0)
        {
            var service = new Mock<IMetadataService>(MockBehavior.Strict);
            service.Setup(s => s.Order)
                .Returns(order);
            service.Setup(s => s.CanRefreshPrimary(It.IsAny<Type>()))
                .Returns(refreshPrimary);
            service.Setup(s => s.CanRefresh(It.IsAny<BaseItem>()))
                .Returns(canRefresh);
            service.Setup(s => s.RefreshMetadata(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(ItemUpdateType.MetadataDownload));
            return service;
        }

        private static IImageProvider MockIImageProvider<TProviderType>(string name, BaseItem expectedType, bool supports = true, int? order = null, bool errorOnSupported = false)
            where TProviderType : class, IImageProvider
        {
            Mock<IHasOrder>? hasOrder = null;
            if (order is not null)
            {
                hasOrder = new Mock<IHasOrder>(MockBehavior.Strict);
                hasOrder.Setup(i => i.Order)
                    .Returns((int)order);
            }

            var provider = hasOrder is null
                ? new Mock<TProviderType>(MockBehavior.Strict)
                : hasOrder.As<TProviderType>();
            provider.Setup(p => p.Name)
                .Returns(name);
            if (errorOnSupported)
            {
                provider.Setup(p => p.Supports(It.IsAny<BaseItem>()))
                    .Throws(new ArgumentException("Provider threw exception on Supports(item)"));
            }
            else
            {
                provider.Setup(p => p.Supports(expectedType))
                    .Returns(supports);
            }

            return provider.Object;
        }

        private static IMetadataProvider<TItemType> MockIMetadataProviderMapper<TItemType, TLookupInfoType>(string typeName, string providerName, int? order = null, bool forced = false)
            where TItemType : BaseItem, IHasLookupInfo<TLookupInfoType>
            where TLookupInfoType : ItemLookupInfo, new()
            => typeName switch
            {
                "ILocalMetadataProvider" => MockIMetadataProvider<ILocalMetadataProvider<TItemType>, TItemType>(providerName, order, forced),
                "IRemoteMetadataProvider" => MockIMetadataProvider<IRemoteMetadataProvider<TItemType, TLookupInfoType>, TItemType>(providerName, order, forced),
                "ICustomMetadataProvider" => MockIMetadataProvider<ICustomMetadataProvider<TItemType>, TItemType>(providerName, order, forced),
                _ => MockIMetadataProvider<IMetadataProvider<TItemType>, TItemType>(providerName, order, forced)
            };

        private static IMetadataProvider<TItemType> MockIMetadataProvider<TProviderType, TItemType>(string name, int? order = null, bool forced = false)
            where TProviderType : class, IMetadataProvider<TItemType>
            where TItemType : BaseItem
        {
            Mock<IForcedProvider>? forcedProvider = null;
            if (forced)
            {
                forcedProvider = new Mock<IForcedProvider>();
            }

            Mock<IHasOrder>? hasOrder = null;
            if (order is not null)
            {
                hasOrder = forcedProvider is null ? new Mock<IHasOrder>() : forcedProvider.As<IHasOrder>();
                hasOrder.Setup(i => i.Order)
                    .Returns((int)order);
            }

            var provider = hasOrder is null
                ? new Mock<TProviderType>(MockBehavior.Strict)
                : hasOrder.As<TProviderType>();
            provider.Setup(p => p.Name)
                .Returns(name);

            return provider.Object;
        }

        private static LibraryOptions CreateLibraryOptions(
            string typeName,
            string[]? imageFetcherOrder = null,
            string[]? localMetadataReaderOrder = null,
            string[]? metadataFetcherOrder = null)
        {
            var libraryOptions = new LibraryOptions
            {
                LocalMetadataReaderOrder = localMetadataReaderOrder
            };

            // only create type options if populating it with something
            if (imageFetcherOrder is not null || metadataFetcherOrder is not null)
            {
                imageFetcherOrder ??= Array.Empty<string>();
                metadataFetcherOrder ??= Array.Empty<string>();

                libraryOptions.TypeOptions = new[]
                {
                    new TypeOptions
                    {
                        Type = typeName,
                        ImageFetcherOrder = imageFetcherOrder,
                        MetadataFetcherOrder = metadataFetcherOrder
                    }
                };
            }

            return libraryOptions;
        }

        private static ServerConfiguration CreateServerConfiguration(
            string typeName,
            string[]? imageFetcherOrder = null,
            string[]? localMetadataReaderOrder = null,
            string[]? metadataFetcherOrder = null)
        {
            var serverConfiguration = new ServerConfiguration();

            // only create type options if populating it with something
            if (imageFetcherOrder is not null || localMetadataReaderOrder is not null || metadataFetcherOrder is not null)
            {
                imageFetcherOrder ??= Array.Empty<string>();
                localMetadataReaderOrder ??= Array.Empty<string>();
                metadataFetcherOrder ??= Array.Empty<string>();

                serverConfiguration.MetadataOptions = new[]
                {
                    new MetadataOptions
                    {
                        ItemType = typeName,
                        ImageFetcherOrder = imageFetcherOrder,
                        LocalMetadataReaderOrder = localMetadataReaderOrder,
                        MetadataFetcherOrder = metadataFetcherOrder
                    }
                };
            }

            return serverConfiguration;
        }

        private static ProviderManager GetProviderManager(
            ServerConfiguration? serverConfiguration = null,
            LibraryOptions? libraryOptions = null,
            IBaseItemManager? baseItemManager = null)
        {
            var serverConfigurationManager = new Mock<IServerConfigurationManager>(MockBehavior.Strict);
            serverConfigurationManager.Setup(i => i.Configuration)
                .Returns(serverConfiguration ?? new ServerConfiguration());

            var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
            libraryManager.Setup(i => i.GetLibraryOptions(It.IsAny<BaseItem>()))
                .Returns(libraryOptions ?? new LibraryOptions());

            var providerManager = new ProviderManager(
                Mock.Of<IHttpClientFactory>(),
                Mock.Of<ISubtitleManager>(),
                serverConfigurationManager.Object,
                Mock.Of<ILibraryMonitor>(),
                _logger,
                Mock.Of<IFileSystem>(),
                Mock.Of<IServerApplicationPaths>(),
                libraryManager.Object,
                baseItemManager!,
                Mock.Of<ILyricManager>(),
                Mock.Of<IMemoryCache>(),
                Mock.Of<IMediaSegmentManager>());

            return providerManager;
        }

        private static void AddParts(
            ProviderManager providerManager,
            IEnumerable<IImageProvider>? imageProviders = null,
            IEnumerable<IMetadataService>? metadataServices = null,
            IEnumerable<IMetadataProvider>? metadataProviders = null,
            IEnumerable<IMetadataSaver>? metadataSavers = null,
            IEnumerable<IExternalId>? externalIds = null,
            IEnumerable<IExternalUrlProvider>? externalUrlProviders = null)
        {
            imageProviders ??= Array.Empty<IImageProvider>();
            metadataServices ??= Array.Empty<IMetadataService>();
            metadataProviders ??= Array.Empty<IMetadataProvider>();
            metadataSavers ??= Array.Empty<IMetadataSaver>();
            externalIds ??= Array.Empty<IExternalId>();
            externalUrlProviders ??= Array.Empty<IExternalUrlProvider>();

            providerManager.AddParts(imageProviders, metadataServices, metadataProviders, metadataSavers, externalIds, externalUrlProviders);
        }

        /// <summary>
        /// Simple <see cref="BaseItem"/> extension to make SupportsLocalMetadata directly settable.
        /// </summary>
        internal class MetadataTestItem : BaseItem, IHasLookupInfo<MetadataTestItemInfo>
        {
            public bool EnableLocalMetadata { get; set; } = true;

            public override bool SupportsLocalMetadata => EnableLocalMetadata;

            public MetadataTestItemInfo GetLookupInfo()
            {
                return GetItemLookupInfo<MetadataTestItemInfo>();
            }
        }

        internal class MetadataTestItemInfo : ItemLookupInfo
        {
        }
    }
}
