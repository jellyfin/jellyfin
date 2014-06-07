using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library.Validators
{
    public class UserViewPostScanTask : ILibraryPostScanTask
    {
        private readonly IUserManager _userManager;
        private readonly IUserViewManager _userViewManager;

        public UserViewPostScanTask(IUserManager userManager, IUserViewManager userViewManager)
        {
            _userManager = userManager;
            _userViewManager = userViewManager;
        }

        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            foreach (var user in _userManager.Users)
            {
                foreach (var view in await _userViewManager.GetUserViews(new UserViewQuery
                {
                    UserId = user.Id.ToString("N")

                }, cancellationToken).ConfigureAwait(false))
                {
                    await view.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
