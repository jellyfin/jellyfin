using System;
using System.Linq;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api.Devices
{
    [Route("/Devices", "GET", Summary = "Gets all devices")]
    [Authenticated(Roles = "Admin")]
    public class GetDevices : DeviceQuery, IReturn<QueryResult<DeviceInfo>>
    {
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
        [ApiMember(Name = "DeviceId", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }

        [ApiMember(Name = "Album", Description = "Album", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Album { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Name { get; set; }

        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/Devices/Info", "GET", Summary = "Gets device info")]
    [Authenticated]
    public class GetDeviceInfo : IReturn<DeviceInfo>
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Devices/Capabilities", "GET", Summary = "Gets device capabilities")]
    [Authenticated]
    public class GetDeviceCapabilities : IReturn<ClientCapabilities>
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
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

        public DeviceService(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public void Post(PostDeviceOptions request)
        {
            var task = _deviceManager.UpdateDeviceInfo(request.Id, new DeviceOptions
            {
                CustomName = request.CustomName,
                CameraUploadPath = request.CameraUploadPath
            });

            Task.WaitAll(task);
        }

        public object Get(GetDeviceInfo request)
        {
            return ToOptimizedResult(_deviceManager.GetDevice(request.Id));
        }

        public object Get(GetDeviceCapabilities request)
        {
            return ToOptimizedResult(_deviceManager.GetCapabilities(request.Id));
        }

        public object Get(GetDevices request)
        {
            return ToOptimizedResult(_deviceManager.GetDevices(request));
        }

        public object Get(GetCameraUploads request)
        {
            return ToOptimizedResult(_deviceManager.GetCameraUploadHistory(request.DeviceId));
        }

        public void Delete(DeleteDevice request)
        {
            var task = _deviceManager.DeleteDevice(request.Id);

            Task.WaitAll(task);
        }

        public void Post(PostCameraUpload request)
        {
            var deviceId = Request.QueryString["DeviceId"];
            var album = Request.QueryString["Album"];
            var id = Request.QueryString["Id"];
            var name = Request.QueryString["Name"];

            if (Request.ContentType.IndexOf("multi", StringComparison.OrdinalIgnoreCase) == -1)
            {
                var task = _deviceManager.AcceptCameraUpload(deviceId, request.RequestStream, new LocalFileInfo
                {
                    MimeType = Request.ContentType,
                    Album = album,
                    Name = name,
                    Id = id
                });

                Task.WaitAll(task);
            }
            else
            {
                var file = Request.Files.First();

                var task = _deviceManager.AcceptCameraUpload(deviceId, file.InputStream, new LocalFileInfo
                {
                    MimeType = file.ContentType,
                    Album = album,
                    Name = name,
                    Id = id
                });

                Task.WaitAll(task);
            }
        }
    }
}
