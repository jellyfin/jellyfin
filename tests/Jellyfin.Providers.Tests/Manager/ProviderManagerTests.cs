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
                providerList.Add(MockIImageProvider<IImageProvider>(nameProvider(i), item, order: order));
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
        public void GetImageProviders_CanRefreshImagesBaseItemEnabled_WhenLocalOrEnabled(Type providerType, bool enabled, bool expected)
        {
            GetImageProviders_CanRefreshImages_Tester(providerType, true, expected, baseItemEnabled: enabled);
        }

        private static void GetImageProviders_CanRefreshImages_Tester(
            Type providerType,
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
            baseItemManager.Setup(i => i.IsImageFetcherEnabled(item, It.IsAny<TypeOptions>(), providerName))
                .Returns(baseItemEnabled);

            using var providerManager = GetProviderManager(baseItemManager: baseItemManager.Object);
            AddParts(providerManager, imageProviders: new[] { provider });

            var actualProviders = providerManager.GetImageProviders(item, refreshOptions).ToArray();

            Assert.Equal(expected ? 1 : 0, actualProviders.Length);
        }

        private static TheoryData<string[], int[]?, int[]?, int[]?, int[]?, int?[]?, int[]> GetMetadataProvidersOrderData()
        {
            var l = nameof(ILocalMetadataProvider);
            var r = nameof(IRemoteMetadataProvider);
            return new ()
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
                // note odd interaction - orderby determines order of slot when local and remote both have a slot 0
                { new[] { l, l, r, r }, new[] { 1 }, new[] { 3 }, null, null, new int?[] { null, 2, null, 1 }, new[] { 3, 1, 0, 2 } }, // sorts interleaved results

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
        [InlineData(typeof(IMetadataProvider))]
        [InlineData(typeof(ILocalMetadataProvider))]
        [InlineData(typeof(IRemoteMetadataProvider))]
        [InlineData(typeof(ICustomMetadataProvider))]
        public void GetMetadataProviders_CanRefreshMetadataBasic_ReturnsTrue(Type providerType)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, true);
        }

        [Theory]
        [InlineData(typeof(ILocalMetadataProvider), false, true)]
        [InlineData(typeof(IRemoteMetadataProvider), false, false)]
        [InlineData(typeof(ICustomMetadataProvider), false, false)]
        [InlineData(typeof(ILocalMetadataProvider), true, true)]
        [InlineData(typeof(ICustomMetadataProvider), true, false)]
        public void GetMetadataProviders_CanRefreshMetadataLocked_WhenLocalOrForced(Type providerType, bool forced, bool expected)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, expected, itemLocked: true, providerForced: forced);
        }

        [Theory]
        [InlineData(typeof(ILocalMetadataProvider), false, true)]
        [InlineData(typeof(ICustomMetadataProvider), false, true)]
        [InlineData(typeof(IRemoteMetadataProvider), false, false)]
        [InlineData(typeof(IRemoteMetadataProvider), true, true)]
        public void GetMetadataProviders_CanRefreshMetadataBaseItemEnabled_WhenEnabledOrNotRemote(Type providerType, bool baseItemEnabled, bool expected)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, expected, baseItemEnabled: baseItemEnabled);
        }

        [Theory]
        [InlineData(typeof(IRemoteMetadataProvider), false, true)]
        [InlineData(typeof(ICustomMetadataProvider), false, true)]
        [InlineData(typeof(ILocalMetadataProvider), false, false)]
        [InlineData(typeof(ILocalMetadataProvider), true, true)]
        public void GetMetadataProviders_CanRefreshMetadataSupportsLocal_WhenSupportsOrNotLocal(Type providerType, bool supportsLocalMetadata, bool expected)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, expected, supportsLocalMetadata: supportsLocalMetadata);
        }

        [Theory]
        [InlineData(typeof(ICustomMetadataProvider), true)]
        [InlineData(typeof(IRemoteMetadataProvider), false)]
        [InlineData(typeof(ILocalMetadataProvider), false)]
        public void GetMetadataProviders_CanRefreshMetadataOwned_WhenNotLocal(Type providerType, bool expected)
        {
            GetMetadataProviders_CanRefreshMetadata_Tester(providerType, expected, ownedItem: true);
        }

        private static void GetMetadataProviders_CanRefreshMetadata_Tester(
            Type providerType,
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
            var provider = MockIMetadataProviderMapper<MetadataTestItem, MetadataTestItemInfo>(providerType.Name, providerName, forced: providerForced);

            var baseItemManager = new Mock<IBaseItemManager>(MockBehavior.Strict);
            baseItemManager.Setup(i => i.IsMetadataFetcherEnabled(item, It.IsAny<TypeOptions>(), providerName))
                .Returns(baseItemEnabled);

            using var providerManager = GetProviderManager(baseItemManager: baseItemManager.Object);
            AddParts(providerManager, metadataProviders: new[] { provider });

            var actualProviders = providerManager.GetMetadataProviders<MetadataTestItem>(item, new LibraryOptions()).ToArray();

            Assert.Equal(expected ? 1 : 0, actualProviders.Length);
        }

        private static IImageProvider MockIImageProvider<TProviderType>(string name, BaseItem expectedType, bool supports = true, int? order = null, bool errorOnSupported = false)
            where TProviderType : class, IImageProvider
        {
            Mock<IHasOrder>? hasOrder = null;
            if (order != null)
            {
                hasOrder = new Mock<IHasOrder>(MockBehavior.Strict);
                hasOrder.Setup(i => i.Order)
                    .Returns((int)order);
            }

            var provider = hasOrder == null
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
            if (order != null)
            {
                hasOrder = forcedProvider == null ? new Mock<IHasOrder>() : forcedProvider.As<IHasOrder>();
                hasOrder.Setup(i => i.Order)
                    .Returns((int)order);
            }

            var provider = hasOrder == null
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
            if (imageFetcherOrder != null || metadataFetcherOrder != null)
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
            if (imageFetcherOrder != null || localMetadataReaderOrder != null || metadataFetcherOrder != null)
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

        /// <summary>
        /// Simple <see cref="BaseItem"/> extension to make SupportsLocalMetadata directly settable.
        /// </summary>
        public class MetadataTestItem : BaseItem, IHasLookupInfo<MetadataTestItemInfo>
        {
            public bool EnableLocalMetadata { get; set; } = true;

            public override bool SupportsLocalMetadata => EnableLocalMetadata;

            public MetadataTestItemInfo GetLookupInfo()
            {
                return GetItemLookupInfo<MetadataTestItemInfo>();
            }
        }

        public class MetadataTestItemInfo : ItemLookupInfo
        {
        }
    }
}
