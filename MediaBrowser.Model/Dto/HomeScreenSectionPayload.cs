using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Dto
{
    public class HomeScreenSectionPayload
    {
        public Guid UserId { get; set; }

        public string? AdditionalData { get; set; }
    }
}
