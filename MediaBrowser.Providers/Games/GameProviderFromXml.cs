using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Savers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Games
{
    public class GameProviderFromXml : BaseMetadataProvider
    {
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logManager"></param>
        /// <param name="configurationManager"></param>
        public GameProviderFromXml(ILogManager logManager, IServerConfigurationManager configurationManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Supports(BaseItem item)
        {
            return item is Game;
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var savePath = GameXmlSaver.GetGameSavePath(item);

            var xml = item.ResolveArgs.GetMetaFileByPath(savePath) ?? new FileInfo(savePath);

            if (!xml.Exists)
            {
                return false;
            }

            return _fileSystem.GetLastWriteTimeUtc(xml) > item.DateLastSaved;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="force"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            return Fetch((Game)item, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> Fetch(Game game, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metaFile = GameXmlSaver.GetGameSavePath(game);

            if (File.Exists(metaFile))
            {
                await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    new GameXmlParser(Logger).Fetch(game, metaFile, cancellationToken);
                }
                finally
                {
                    XmlParsingResourcePool.Release();
                }
            }

            SetLastRefreshed(game, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }
    }
}