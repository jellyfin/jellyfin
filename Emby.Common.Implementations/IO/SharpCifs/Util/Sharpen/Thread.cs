using SharpCifs.Util.DbsHelper;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SharpCifs.Util.Sharpen
{
    public class Thread : IRunnable
    {
        private static ThreadGroup DefaultGroup = new ThreadGroup();

        [ThreadStatic]
        private static Thread WrapperThread;

        public static Thread CurrentThread()
        {
            if (Thread.WrapperThread == null)
            {
                Thread.WrapperThread = new Thread(System.Environment.CurrentManagedThreadId);
            }

            return Thread.WrapperThread;
        }


        public CancellationTokenSource Canceller => this._canceller;

        public bool IsCanceled
        {
            get
            {
                if (this._canceller?.IsCancellationRequested == true
                    && !this._isCanceled)
                {
                    this._isCanceled = true;
                }

                return this._isCanceled;
            }
        }

        private IRunnable _runnable;
        private ThreadGroup _tgroup;
        private System.Threading.Tasks.Task _task = null;
        private CancellationTokenSource _canceller = null;

        private string _name = string.Empty;
        private bool _isBackground = true;
        private bool _interrupted = false;
        private int? _id = null;
        private bool _isRunning = false;
        private bool _isCanceled = false;



        public Thread() : this(null, null, null)
        {
        }


        public Thread(string name) : this(null, null, name)
        {
        }


        public Thread(ThreadGroup grp, string name) : this(null, grp, name)
        {
        }


        public Thread(IRunnable runnable) : this(runnable, null, null)
        {
        }


        private Thread(IRunnable runnable, ThreadGroup grp, string name)
        {
            this._runnable = runnable ?? this;
            this._tgroup = grp ?? DefaultGroup;
            this._tgroup.Add(this);

            if (name != null)
            {
                this._name = name;
            }
        }


        private Thread(int threadId)
        {
            this._id = threadId;

            this._tgroup = DefaultGroup;
            this._tgroup.Add(this);
        }
        

        public string GetName()
        {
            return this._name;
        }


        public ThreadGroup GetThreadGroup()
        {
            return this._tgroup;
        }


        public static void Yield()
        {
        }


        public void Interrupt()
        {
            this._interrupted = true;
            this._canceller?.Cancel(true);
        }


        public static bool Interrupted()
        {
            if (Thread.WrapperThread == null)
            {
                return false;
            }

            Thread wrapperThread = Thread.WrapperThread;
            lock (wrapperThread)
            {
                bool interrupted = Thread.WrapperThread._interrupted;
                Thread.WrapperThread._interrupted = false;
                return interrupted;
            }
        }


        public bool IsAlive()
        {
            if (this._task == null)
                return true; //実行されていない

            //Taskが存在し、続行中のときtrue
            return (!this._task.IsCanceled
                    && !this._task.IsFaulted
                    && !this._task.IsCompleted);
        }


        public void Join()
        {
            this._task?.Wait();
        }


        public void Join(long timeout)
        {
            this._task?.Wait((int) timeout);
        }


        public virtual void Run()
        {
        }


        public void SetDaemon(bool daemon)
        {
            this._isBackground = daemon;
        }


        public void SetName(string name)
        {
            this._name = name;
        }


        public static void Sleep(long milis)
        {
            System.Threading.Tasks.Task.Delay((int) milis).Wait();
        }


        public void Start(bool isSynced = false)
        {
            if (this._isRunning)
                throw new InvalidOperationException("Thread Already started.");

            this._canceller = new CancellationTokenSource();
            
            this._task = System.Threading.Tasks.Task.Run(() =>
            {
                Thread.WrapperThread = this;
                this._id = System.Environment.CurrentManagedThreadId;

                //Log.Out("Thread.Start - Task Start");
                this._isRunning = true;
                
                try
                {
                    this._runnable.Run();
                    //Log.Out("Thread.Start - Task Normaly End");
                }
                catch (Exception exception)
                {
                    //Log.Out("Thread.Start - Task Error End");
                    Console.WriteLine(exception);
                }
                finally
                {
                    this._isRunning = false;

                    this._tgroup?.Remove(this);

                    this._canceller?.Dispose();
                    this._canceller = null;

                    //Log.Out("Thread.Start - Task Close Completed");
                }
            }, this._canceller.Token);

            //同期的に実行するとき、動作中フラグONまで待つ。
            if (isSynced)
                while (!this._isRunning)
                    System.Threading.Tasks.Task.Delay(300).GetAwaiter().GetResult();
        }


        public void Cancel(bool isSynced = false)
        {
            //Log.Out("Thread.Cancel");

            this._isCanceled = true;
            this._canceller?.Cancel(true);

            //同期的に実行するとき、動作中フラグOFFまで待つ。
            if (isSynced)
                while (this._isRunning)
                    System.Threading.Tasks.Task.Delay(300).GetAwaiter().GetResult();
        }


        public bool Equals(Thread thread)
        {
            //渡し値スレッドがnullのとき、合致しない
            if (thread == null)
                return false;

            //自身か渡し値スレッドの、スレッドIDが取得出来ていない(=スレッド未生成)
            //　→合致しない
            if (this._id == null
                || thread._id == null)
                return false;

            return (this._id == thread._id);
        }


        public void Dispose()
        {
            //Log.Out("Thread.Dispose");

            this._runnable = null;
            this._tgroup = null;
            this._task = null;
            this._canceller?.Dispose();
            this._canceller = null;
            this._name = null;
            this._isRunning = false;
            this._id = null;
        }
    }

    public class ThreadGroup
    {
        private List<Thread> _threads = new List<Thread>();

        public ThreadGroup()
        {
        }

        public ThreadGroup(string name)
        {
        }

        internal void Add(Thread t)
        {
            lock (_threads)
            {
                _threads.Add(t);
            }
        }

        internal void Remove(Thread t)
        {
            lock (_threads)
            {
                _threads.Remove(t);
            }
        }

        public int Enumerate(Thread[] array)
        {
            lock (_threads)
            {
                int count = Math.Min(array.Length, _threads.Count);
                _threads.CopyTo(0, array, 0, count);
                return count;
            }
        }
    }
}
