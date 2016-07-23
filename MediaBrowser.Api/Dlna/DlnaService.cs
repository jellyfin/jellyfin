using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.Dlna
{
    [Route("/Dlna/ProfileInfos", "GET", Summary = "Gets a list of profiles")]
    public class GetProfileInfos : IReturn<List<DeviceProfileInfo>>
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
    public class DlnaService : BaseApiService
    {
        private readonly IDlnaManager _dlnaManager;

        public DlnaService(IDlnaManager dlnaManager)
        {
            _dlnaManager = dlnaManager;
        }

        public object Get(GetProfileInfos request)
        {
            var result = _dlnaManager.GetProfileInfos().ToList();

            return ToOptimizedResult(result);
        }

        public object Get(GetProfile request)
        {
            var result = _dlnaManager.GetProfile(request.Id);

            return ToOptimizedResult(result);
        }

        public object Get(GetDefaultProfile request)
        {
            var result = _dlnaManager.GetDefaultProfile();

            return ToOptimizedResult(result);
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