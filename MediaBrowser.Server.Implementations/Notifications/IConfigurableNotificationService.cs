using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Notifications
{
    public interface IConfigurableNotificationService
    {
        bool IsHidden { get; }
        bool IsEnabled(string notificationType);
    }
}
