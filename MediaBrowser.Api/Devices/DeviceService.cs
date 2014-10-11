using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Devices;
using ServiceStack;
using ServiceStack.Web;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Devices
{
    [Route("/Devices", "GET", Summary = "Gets all devices")]
    public class GetDevices : IReturn<List<DeviceInfo>>
    {
        [ApiMember(Name = "SupportsContentUploading", Description = "SupportsContentUploading", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? SupportsContentUploading { get; set; }
    }

    [Route("/Devices", "DELETE", Summary = "Deletes a device")]
    public class DeleteDevice
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Devices/CameraUploads", "GET", Summary = "Gets camera upload history for a device")]
    public class GetCameraUploads : IReturn<ContentUploadHistory>
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }
    }

    [Route("/Devices/CameraUploads", "POST", Summary = "Uploads content")]
    public class PostCameraUpload : IRequiresRequestStream, IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Device Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }

        [ApiMember(Name = "Album", Description = "Album", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Album { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Name { get; set; }

        [ApiMember(Name = "FullPath", Description = "FullPath", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string FullPath { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Authenticated]
    public class DeviceService : BaseApiService
    {
        private readonly IDeviceManager _deviceManager;

        public DeviceService(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public object Get(GetDevices request)
        {
            var devices = _deviceManager.GetDevices();

            if (request.SupportsContentUploading.HasValue)
            {
                var val = request.SupportsContentUploading.Value;

                devices = devices.Where(i => i.Capabilities.SupportsContentUploading == val);
            }

            return ToOptimizedResult(devices.ToList());
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
            var deviceId = request.DeviceId;

            var album = Request.QueryString["Album"];
            var fullPath = Request.QueryString["FullPath"];
            var name = Request.QueryString["Name"];

            var task = _deviceManager.AcceptCameraUpload(deviceId, request.RequestStream, new LocalFileInfo
            {
                MimeType = Request.ContentType,
                Album = album,
                Name = name,
                FullPath = fullPath
            });

            Task.WaitAll(task);
        }
    }
}
