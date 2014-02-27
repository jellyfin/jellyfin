using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.LiveTv;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    class CleanDatabaseScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILiveTvManager _liveTvManager;

        public CleanDatabaseScheduledTask(ILiveTvManager liveTvManager)
        {
            _liveTvManager = liveTvManager;
        }

        public string Name
        {
            get { return "Clean TV Database"; }
        }

        public string Description
        {
            get { return "Deletes old programs from the tv database."; }
        }

        public string Category
        {
            get { return "Live TV"; }
        }

        public Task Execute(System.Threading.CancellationToken cancellationToken, IProgress<double> progress)
        {
            var manager = (LiveTvManager)_liveTvManager;

            return manager.CleanDatabase(progress, cancellationToken);
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[] 
            { 
                new IntervalTrigger{ Interval = TimeSpan.FromHours(24)}
            };
        }

        public bool IsHidden
        {
            get { return _liveTvManager.ActiveService == null; }
        }

        public bool IsEnabled
        {
            get { return true; }
        }
    }
}
