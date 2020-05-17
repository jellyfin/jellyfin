using System.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Devices
{
    [Route("/Devices", "GET", Summary = "Gets all devices")]
    [Authenticated(Roles = "Admin")]
    public class GetDevices : DeviceQuery, IReturn<QueryResult<DeviceInfo>>
    {
    }

    [Route("/Devices/Info", "GET", Summary = "Gets info for a device")]
    [Authenticated(Roles = "Admin")]
    public class GetDeviceInfo : IReturn<DeviceInfo>
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Devices/Options", "GET", Summary = "Gets options for a device")]
    [Authenticated(Roles = "Admin")]
    public class GetDeviceOptions : IReturn<DeviceOptions>
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Devices", "DELETE", Summary = "Deletes a device")]
    public class DeleteDevice
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Devices/CameraUploads", "GET", Summary = "Gets camera upload history for a device")]
    [Authenticated]
    public class GetCameraUploads : IReturn<ContentUploadHistory>
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }
    }

    [Route("/Devices/CameraUploads", "POST", Summary = "Uploads content")]
    [Authenticated]
    public class PostCameraUpload : IRequiresRequestStream, IReturnVoid
    {
        [ApiMember(Name = "DeviceId", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string DeviceId { get; set; }

        [ApiMember(Name = "Album", Description = "Album", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Album { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Name { get; set; }

        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Id { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/Devices/Options", "POST", Summary = "Updates device options")]
    [Authenticated(Roles = "Admin")]
    public class PostDeviceOptions : DeviceOptions, IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
    }

    public class DeviceService : BaseApiService
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IAuthenticationRepository _authRepo;
        private readonly ISessionManager _sessionManager;

        public DeviceService(
            ILogger<DeviceService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IDeviceManager deviceManager,
            IAuthenticationRepository authRepo,
            ISessionManager sessionManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _deviceManager = deviceManager;
            _authRepo = authRepo;
            _sessionManager = sessionManager;
        }

        public void Post(PostDeviceOptions request)
        {
            _deviceManager.UpdateDeviceOptions(request.Id, request);
        }

        public object Get(GetDevices request)
        {
            return ToOptimizedResult(_deviceManager.GetDevices(request));
        }

        public object Get(GetDeviceInfo request)
        {
            return _deviceManager.GetDevice(request.Id);
        }

        public object Get(GetDeviceOptions request)
        {
            return _deviceManager.GetDeviceOptions(request.Id);
        }

        public void Delete(DeleteDevice request)
        {
            var sessions = _authRepo.Get(new AuthenticationInfoQuery
            {
                DeviceId = request.Id

            }).Items;

            foreach (var session in sessions)
            {
                _sessionManager.Logout(session);
            }
        }
    }
}
