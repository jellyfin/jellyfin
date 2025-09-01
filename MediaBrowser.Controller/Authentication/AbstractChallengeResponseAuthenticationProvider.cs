using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace MediaBrowser.Controller.Authentication
{
    /* TODO: implement
     * public class
        AbstractChallengeResponseAuthenticationProvider<TGlobalData, TUserData>(
        IDbContextFactory<JellyfinDbContext> contextFactory,
        IUserManager userManager)
        : AbstractAuthenticationProvider<ExternallyTriggeredAuthenticationData, TGlobalData, TUserData>(contextFactory, userManager)
        where TUserData : struct
        where TGlobalData : struct
    {
    }*/
}
