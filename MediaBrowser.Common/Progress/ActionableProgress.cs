using System;
using System.Collections.Generic;

namespace MediaBrowser.Common.Progress
{
    /// <summary>
    /// Class ActionableProgress
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionableProgress<T> : IProgress<T>, IDisposable
    {
        /// <summary>
        /// The _actions
        /// </summary>
        private readonly List<Action<T>> _actions = new List<Action<T>>();
        public event EventHandler<T> ProgressChanged;

        /// <summary>
        /// Registers the action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void RegisterAction(Action<T> action)
        {
            _actions.Add(action);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _actions.Clear();
            }
        }

        public void Report(T value)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, value);
            }

            foreach (var action in _actions)
            {
                action(value);
            }
        }
    }

    public class SimpleProgress<T> : IProgress<T>
    {
        public event EventHandler<T> ProgressChanged;

        public void Report(T value)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, value);
            }
        }
    }
}
