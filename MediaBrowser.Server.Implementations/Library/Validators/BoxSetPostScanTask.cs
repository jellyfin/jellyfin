using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library.Validators
{
    public class BoxSetPostScanTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;

        public BoxSetPostScanTask(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var items = _libraryManager.RootFolder.RecursiveChildren.ToList();

            var boxsets = items.OfType<BoxSet>().ToList();

            var numComplete = 0;

            foreach (var boxset in boxsets)
            {
                foreach (var child in boxset.Children.Concat(boxset.GetLinkedChildren()).OfType<ISupportsBoxSetGrouping>())
                {
                    var boxsetIdList = child.BoxSetIdList.ToList();
                    if (!boxsetIdList.Contains(boxset.Id))
                    {
                        boxsetIdList.Add(boxset.Id);
                    }
                    child.BoxSetIdList = boxsetIdList;
                }

                numComplete++;
                double percent = numComplete;
                percent /= boxsets.Count;
                progress.Report(percent * 100);
            }

            progress.Report(100);
            return Task.FromResult(true);
        }
    }
}
