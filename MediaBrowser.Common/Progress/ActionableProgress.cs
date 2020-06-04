#pragma warning disable CS1591

using System;

namespace MediaBrowser.Common.Progress
{
    /// <summary>
    /// Class ActionableProgress.
    /// </summary>
    /// <typeparam name="T">The type for the action parameter.</typeparam>
    public class ActionableProgress<T> : IProgress<T>
    {
        /// <summary>
        /// The _actions.
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
}
