using System;
using System.Collections.Generic;
using ST = System.Threading;

namespace SharpCifs.Util.Sharpen
{
    class ThreadPoolExecutor
    {
        ThreadFactory _tf;
        int _corePoolSize;
        int _maxPoolSize;
        List<Thread> _pool = new List<Thread>();
        int _runningThreads;
        int _freeThreads;
        bool _shutdown;
        Queue<IRunnable> _pendingTasks = new Queue<IRunnable>();

        public ThreadPoolExecutor(int corePoolSize, ThreadFactory factory)
        {
            this._corePoolSize = corePoolSize;
            _maxPoolSize = corePoolSize;
            _tf = factory;
        }

        public void SetMaximumPoolSize(int size)
        {
            _maxPoolSize = size;
        }

        public bool IsShutdown()
        {
            return _shutdown;
        }

        public virtual bool IsTerminated()
        {
            lock (_pendingTasks)
            {
                return _shutdown && _pendingTasks.Count == 0;
            }
        }

        public virtual bool IsTerminating()
        {
            lock (_pendingTasks)
            {
                return _shutdown && !IsTerminated();
            }
        }

        public int GetCorePoolSize()
        {
            return _corePoolSize;
        }

        public void PrestartAllCoreThreads()
        {
            lock (_pendingTasks)
            {
                while (_runningThreads < _corePoolSize)
                    StartPoolThread();
            }
        }

        public void SetThreadFactory(ThreadFactory f)
        {
            _tf = f;
        }

        public void Execute(IRunnable r)
        {
            InternalExecute(r, true);
        }

        internal void InternalExecute(IRunnable r, bool checkShutdown)
        {
            lock (_pendingTasks)
            {
                if (_shutdown && checkShutdown)
                    throw new InvalidOperationException();
                if (_runningThreads < _corePoolSize)
                {
                    StartPoolThread();
                }
                else if (_freeThreads > 0)
                {
                    _freeThreads--;
                }
                else if (_runningThreads < _maxPoolSize)
                {
                    StartPoolThread();
                }
                _pendingTasks.Enqueue(r);
                ST.Monitor.PulseAll(_pendingTasks);
            }
        }

        void StartPoolThread()
        {
            _runningThreads++;
            _pool.Add(_tf.NewThread(new RunnableAction(RunPoolThread)));
        }

        public void RunPoolThread()
        {
            while (!IsTerminated())
            {
                try
                {
                    IRunnable r = null;
                    lock (_pendingTasks)
                    {
                        _freeThreads++;
                        while (!IsTerminated() && _pendingTasks.Count == 0)
                            ST.Monitor.Wait(_pendingTasks);
                        if (IsTerminated())
                            break;
                        r = _pendingTasks.Dequeue();
                    }
                    if (r != null)
                        r.Run();
                }
                //supress all errors, anyway
                //catch (ST.ThreadAbortException) {
                //	// Do not catch a thread abort. If we've been aborted just let the thread die.
                //	// Currently reseting an abort which was issued because the appdomain is being
                //	// torn down results in the process living forever and consuming 100% cpu time.
                //	return;
                //}
                catch
                {
                }
            }
        }

        public virtual void Shutdown()
        {
            lock (_pendingTasks)
            {
                _shutdown = true;
                ST.Monitor.PulseAll(_pendingTasks);
            }
        }

        public virtual List<IRunnable> ShutdownNow()
        {
            lock (_pendingTasks)
            {
                _shutdown = true;
                foreach (var t in _pool)
                {
                    try
                    {
                        t.Cancel(true);
                        t.Dispose();
                    }
                    catch { }
                }
                _pool.Clear();
                _freeThreads = 0;
                _runningThreads = 0;
                var res = new List<IRunnable>(_pendingTasks);
                _pendingTasks.Clear();
                return res;
            }
        }
    }

    class RunnableAction : IRunnable
    {
        Action _action;

        public RunnableAction(Action a)
        {
            _action = a;
        }

        public void Run()
        {
            _action();
        }
    }
}
