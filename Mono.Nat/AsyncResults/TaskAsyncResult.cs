// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2019 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace Mono.Nat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="TaskAsyncResult" />.
    /// </summary>
    internal class TaskAsyncResult : IAsyncResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAsyncResult"/> class.
        /// </summary>
        /// <param name="task">The task<see cref="Task"/>.</param>
        /// <param name="callback">The callback<see cref="AsyncCallback"/>.</param>
        /// <param name="asyncState">The asyncState<see cref="object"/>.</param>
        public TaskAsyncResult(Task task, AsyncCallback callback, object asyncState)
        {
            AsyncState = asyncState;
            Callback = callback;
            CompletedSynchronously = task.IsCompleted;
            Task = task;
            WaitHandle = new ManualResetEvent(false);
        }

        /// <summary>
        /// Gets the AsyncState.
        /// </summary>
        public object AsyncState { get; }

        /// <summary>
        /// Gets the Callback.
        /// </summary>
        public AsyncCallback Callback { get; }

        /// <summary>
        /// Gets the AsyncWaitHandle.
        /// </summary>
        public WaitHandle AsyncWaitHandle => WaitHandle;

        /// <summary>
        /// Gets a value indicating whether CompletedSynchronously.
        /// </summary>
        public bool CompletedSynchronously { get; }

        /// <summary>
        /// Gets a value indicating whether IsCompleted.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Gets the Task.
        /// </summary>
        public Task Task { get; }

        /// <summary>
        /// Gets the WaitHandle.
        /// </summary>
        internal ManualResetEvent WaitHandle { get; }

        /// <summary>
        /// The Complete.
        /// </summary>
        public void Complete()
        {
            IsCompleted = true;
            WaitHandle.Set();
            Callback(this);
        }
    }
}
