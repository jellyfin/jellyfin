using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Notifications;

namespace MediaBrowser.Controller.Health
{
    public interface IHealthMonitor
    {
        Task<List<Notification>> GetNotifications(CancellationToken cancellationToken);
    }
}
