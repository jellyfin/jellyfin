using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jellyfin.Data.Migrations
{
    class JellyfinDbFactory : IDesignTimeDbContextFactory<JellyfinDb>
    {
        public JellyfinDb CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDb>();
            optionsBuilder.UseSqlite("./jellyfin.db");

            return null;
        }
    }
}
