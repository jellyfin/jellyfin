#pragma warning disable CS1591

using System;

namespace MediaBrowser.Common.Progress
{
    public class SimpleProgress<T> : IProgress<T>
    {
        public event EventHandler<T> ProgressChanged;

        public void Report(T value)
        {
            ProgressChanged?.Invoke(this, value);
        }
    }
}
