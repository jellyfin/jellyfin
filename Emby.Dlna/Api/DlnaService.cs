#pragma warning disable CS1591

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Services;

namespace Emby.Dlna.Api
{
    [Route("/Dlna/ProfileInfos", "GET", Summary = "Gets a list of profiles")]
    public class GetProfileInfos : IReturn<DeviceProfileInfo[]>
    {
    }

    [Route("/Dlna/Profiles/{Id}", "DELETE", Summary = "Deletes a profile")]
    public class DeleteProfile : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Profile Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Dlna/Profiles/Default", "GET", Summary = "Gets the default profile")]
    public class GetDefaultProfile : IReturn<DeviceProfile>
    {
    }

    [Route("/Dlna/Profiles/{Id}", "GET", Summary = "Gets a single profile")]
    public class GetProfile : IReturn<DeviceProfile>
    {
        [ApiMember(Name = "Id", Description = "Profile Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Dlna/Profiles/{Id}", "POST", Summary = "Updates a profile")]
    public class UpdateProfile : DeviceProfile, IReturnVoid
    {
    }

    [Route("/Dlna/Profiles", "POST", Summary = "Creates a profile")]
    public class CreateProfile : DeviceProfile, IReturnVoid
    {
    }

    [Authenticated(Roles = "Admin")]
    public class DlnaService : IService
    {
        private readonly IDlnaManager _dlnaManager;

        public DlnaService(IDlnaManager dlnaManager)
        {
            _dlnaManager = dlnaManager;
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetProfileInfos request)
        {
            return _dlnaManager.GetProfileInfos().ToArray();
        }

        public object Get(GetProfile request)
        {
            return _dlnaManager.GetProfile(request.Id);
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetDefaultProfile request)
        {
            return _dlnaManager.GetDefaultProfile();
        }

        public void Delete(DeleteProfile request)
        {
            _dlnaManager.DeleteProfile(request.Id);
        }

        public void Post(UpdateProfile request)
        {
            _dlnaManager.UpdateProfile(request);
        }

        public void Post(CreateProfile request)
        {
            _dlnaManager.CreateProfile(request);
        }
    }
}
