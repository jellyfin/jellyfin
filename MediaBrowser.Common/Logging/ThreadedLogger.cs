using System;
using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.Common.Logging
{
    public abstract class ThreadedLogger : BaseLogger
    {
        Thread loggingThread;
        Queue<Action> queue = new Queue<Action>();
        AutoResetEvent hasNewItems = new AutoResetEvent(false);
        volatile bool terminate = false;
        bool waiting = false;

        public ThreadedLogger()
            : base()
        {
            loggingThread = new Thread(new ThreadStart(ProcessQueue));
            loggingThread.IsBackground = true;
            loggingThread.Start();
        }


        void ProcessQueue()
        {
            while (!terminate)
            {
                waiting = true;
                hasNewItems.WaitOne(10000, true);
                waiting = false;

                Queue<Action> queueCopy;
                lock (queue)
                {
                    queueCopy = new Queue<Action>(queue);
                    queue.Clear();
                }

                foreach (var log in queueCopy)
                {
                    log();
                }
            }
        }

        protected override void LogEntry(LogRow row)
        {
            lock (queue)
            {
                queue.Enqueue(() => AsyncLogMessage(row));
            }
            hasNewItems.Set();
        }

        protected abstract void AsyncLogMessage(LogRow row);

        protected override void Flush()
        {
            while (!waiting)
            {
                Thread.Sleep(1);
            }
        }

        public override void Dispose()
        {
            Flush();
            terminate = true;
            hasNewItems.Set();
            base.Dispose();
        }
    }
}
