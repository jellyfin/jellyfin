using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.Activity;
using Microsoft.EntityFrameworkCore;

namespace Emby.Server.Implementations.Activity
{
    public class ActivityRepository : DbContext, IActivityRepository
    {
        protected string _dataDirPath;

        public DbSet<ActivityLogEntry> ActivityLogs { get; set; }

        public ActivityRepository(string dataDirPath)
        {
            _dataDirPath = dataDirPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Ensure the dir exists
            if (!Directory.Exists(_dataDirPath)) Directory.CreateDirectory(_dataDirPath);

            optionsBuilder.UseSqlite($"Filename={Path.Combine(_dataDirPath, "activitylog.sqlite.db")}");
        }

        public async Task CreateAsync(ActivityLogEntry entry)
        {
            await ActivityLogs.AddAsync(entry);
            await SaveChangesAsync();
        }

        public IQueryable<ActivityLogEntry> GetActivityLogEntries()
            => ActivityLogs;
    }
}
