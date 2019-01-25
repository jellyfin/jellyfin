using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Activity
{
    public interface IActivityRepository
    {
        Task CreateAsync(ActivityLogEntry entry);

        IQueryable<ActivityLogEntry> GetActivityLogEntries();
    }
}
