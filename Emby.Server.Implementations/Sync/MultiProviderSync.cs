using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Sync
{
    public class MultiProviderSync
    {
        private readonly SyncManager _syncManager;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IConfigurationManager _config;
        private readonly ICryptoProvider _cryptographyProvider;

        public MultiProviderSync(SyncManager syncManager, IServerApplicationHost appHost, ILogger logger, IFileSystem fileSystem, IConfigurationManager config, ICryptoProvider cryptographyProvider)
        {
            _syncManager = syncManager;
            _appHost = appHost;
            _logger = logger;
            _fileSystem = fileSystem;
            _config = config;
            _cryptographyProvider = cryptographyProvider;
        }

        public async Task Sync(IEnumerable<IServerSyncProvider> providers, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var targets = providers
                .SelectMany(i => i.GetAllSyncTargets().Select(t => new Tuple<IServerSyncProvider, SyncTarget>(i, t)))
                .ToList();

            var numComplete = 0;
            double startingPercent = 0;
            double percentPerItem = 1;
            if (targets.Count > 0)
            {
                percentPerItem /= targets.Count;
            }

            foreach (var target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentPercent = startingPercent;
                var innerProgress = new ActionableProgress<double>();
                innerProgress.RegisterAction(pct =>
                {
                    var totalProgress = pct * percentPerItem;
                    totalProgress += currentPercent;
                    progress.Report(totalProgress);
                });

                var dataProvider = _syncManager.GetDataProvider(target.Item1, target.Item2);

                await new MediaSync(_logger, _syncManager, _appHost, _fileSystem, _config, _cryptographyProvider)
                    .Sync(target.Item1, dataProvider, target.Item2, innerProgress, cancellationToken)
                    .ConfigureAwait(false);

                numComplete++;
                startingPercent = numComplete;
                startingPercent /= targets.Count;
                startingPercent *= 100;
                progress.Report(startingPercent);
            }
        }
    }
}
