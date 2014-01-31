using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Providers.Manager
{
    public abstract class ConcreteMetadataService<TItemType, TIdType> : MetadataService<TItemType, TIdType>
        where TItemType : IHasMetadata, new()
        where TIdType : ItemId, new()
    {
        protected ConcreteMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo)
            : base(serverConfigurationManager, logger, providerManager, providerRepo)
        {
        }

        protected override TItemType CreateNew()
        {
            return new TItemType();
        }
    }
}
