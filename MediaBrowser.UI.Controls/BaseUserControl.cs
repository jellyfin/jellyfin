using System.ComponentModel;
using System.Windows.Controls;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Provides a base class for all user controls
    /// </summary>
    public abstract class BaseUserControl : UserControl
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
