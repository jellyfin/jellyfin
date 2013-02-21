using System;
using System.Collections.Generic;

namespace MediaBrowser.Common.Progress
{
    /// <summary>
    /// Class ActionableProgress
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionableProgress<T> : Progress<T>, IDisposable
    {
        /// <summary>
        /// The _actions
        /// </summary>
        private readonly List<Action<T>> _actions = new List<Action<T>>();

        /// <summary>
        /// Registers the action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void RegisterAction(Action<T> action)
        {
            _actions.Add(action);

            ProgressChanged -= ActionableProgress_ProgressChanged;
            ProgressChanged += ActionableProgress_ProgressChanged;
        }

        /// <summary>
        /// Actionables the progress_ progress changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void ActionableProgress_ProgressChanged(object sender, T e)
        {
            foreach (var action in _actions)
            {
                action(e);
            }
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
                ProgressChanged -= ActionableProgress_ProgressChanged;
                _actions.Clear();
            }
        }
    }
}
