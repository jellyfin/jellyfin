using System;
using System.Collections.Generic;

namespace MediaBrowser.Common.Progress
{
    /// <summary>
    /// Class ActionableProgress
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionableProgress<T> : IProgress<T>
    {
        /// <summary>
        /// The _actions
        /// </summary>
        private Action<T> _action;
        public event EventHandler<T> ProgressChanged;

        /// <summary>
        /// Registers the action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void RegisterAction(Action<T> action)
        {
            _action = action;
        }

        public void Report(T value)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, value);
            }

            var action = _action;
            if (action != null)
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
