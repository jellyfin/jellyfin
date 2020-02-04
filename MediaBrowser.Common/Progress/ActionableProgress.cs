#pragma warning disable CS1591
#pragma warning disable SA1600

using System;

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
            ProgressChanged?.Invoke(this, value);

            _action?.Invoke(value);
        }
    }

    public class SimpleProgress<T> : IProgress<T>
    {
        public event EventHandler<T> ProgressChanged;

        public void Report(T value)
        {
            ProgressChanged?.Invoke(this, value);
        }
    }
}
