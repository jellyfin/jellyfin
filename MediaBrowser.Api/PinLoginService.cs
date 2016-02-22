using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using ServiceStack;

namespace MediaBrowser.Api
{
    [Route("/Auth/Pin", "POST", Summary = "Creates a pin request")]
    public class CreatePinRequest : IReturn<PinCreationResult>
    {
        [ApiMember(Name = "DeviceId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string DeviceId { get; set; }
        [ApiMember(Name = "AppName", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string AppName { get; set; }
    }

    [Route("/Auth/Pin", "GET", Summary = "Gets pin status")]
    public class GetPinStatusRequest : IReturn<PinStatusResult>
    {
        [ApiMember(Name = "DeviceId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }
        [ApiMember(Name = "Pin", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Pin { get; set; }
    }

    [Route("/Auth/Pin/Exchange", "POST", Summary = "Exchanges a pin")]
    public class ExchangePinRequest : IReturn<PinExchangeResult>
    {
        [ApiMember(Name = "DeviceId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string DeviceId { get; set; }
        [ApiMember(Name = "Pin", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Pin { get; set; }
    }

    [Route("/Auth/Pin/Validate", "POST", Summary = "Validates a pin")]
    [Authenticated]
    public class ValidatePinRequest : IReturn<SessionInfoDto>
    {
        [ApiMember(Name = "Pin", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Pin { get; set; }
    }

    public class PinLoginService : BaseApiService
    {
        private static readonly ConcurrentDictionary<string, MyPinStatus> _activeRequests = new ConcurrentDictionary<string, MyPinStatus>(StringComparer.OrdinalIgnoreCase);
        private readonly ISessionManager _sessionManager;
        private readonly IUserManager _userManager;

        public PinLoginService(ISessionManager sessionManager, IUserManager userManager)
        {
            _sessionManager = sessionManager;
            _userManager = userManager;
        }

        public object Post(CreatePinRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                throw new ArgumentNullException("DeviceId");
            }
            if (string.IsNullOrWhiteSpace(request.AppName))
            {
                throw new ArgumentNullException("AppName");
            }

            var pin = GetNewPin();

            var value = new MyPinStatus
            {
                CreationTimeUtc = DateTime.UtcNow,
                IsConfirmed = false,
                IsExpired = false,
                Pin = pin,
                DeviceId = request.DeviceId,
                AppName = request.AppName
            };

            _activeRequests.AddOrUpdate(pin, value, (k, v) => value);

            return ToOptimizedResult(new PinCreationResult
            {
                DeviceId = request.DeviceId,
                IsConfirmed = false,
                IsExpired = false,
                Pin = pin
            });
        }

        public object Get(GetPinStatusRequest request)
        {
            MyPinStatus status;

            if (!_activeRequests.TryGetValue(request.Pin, out status))
            {
                Logger.Debug("Pin {0} not found.", request.Pin);
                throw new ResourceNotFoundException();
            }

            EnsureValid(request.DeviceId, status);

            return ToOptimizedResult(new PinStatusResult
            {
                Pin = status.Pin,
                IsConfirmed = status.IsConfirmed,
                IsExpired = status.IsExpired
            });
        }

        public async Task<object> Post(ExchangePinRequest request)
        {
            MyPinStatus status;

            if (!_activeRequests.TryGetValue(request.Pin, out status))
            {
                Logger.Debug("Pin {0} not found.", request.Pin);
                throw new ResourceNotFoundException();
            }

            EnsureValid(request.DeviceId, status);

            if (!status.IsConfirmed)
            {
                throw new ResourceNotFoundException();
            }

            var auth = AuthorizationContext.GetAuthorizationInfo(Request);
            var user = _userManager.GetUserById(status.UserId);

            var result = await _sessionManager.CreateNewSession(new AuthenticationRequest
            {
                App = auth.Client,
                AppVersion = auth.Version,
                DeviceId = auth.DeviceId,
                DeviceName = auth.Device,
                RemoteEndPoint = Request.RemoteIp,
                Username = user.Name

            }).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public object Post(ValidatePinRequest request)
        {
            MyPinStatus status;

            if (!_activeRequests.TryGetValue(request.Pin, out status))
            {
                throw new ResourceNotFoundException();
            }

            EnsureValid(status);

            status.IsConfirmed = true;
            status.UserId = AuthorizationContext.GetAuthorizationInfo(Request).UserId;

            return ToOptimizedResult(new ValidatePinResult
            {
                AppName = status.AppName
            });
        }

        private void EnsureValid(string requestedDeviceId, MyPinStatus status)
        {
            if (!string.Equals(requestedDeviceId, status.DeviceId, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Debug("Pin device Id's do not match. requestedDeviceId: {0}, status.DeviceId: {1}", requestedDeviceId, status.DeviceId);
                throw new ResourceNotFoundException();
            }

            EnsureValid(status);
        }

        private void EnsureValid(MyPinStatus status)
        {
            if ((DateTime.UtcNow - status.CreationTimeUtc).TotalMinutes > 10)
            {
                status.IsExpired = true;
            }

            if (status.IsExpired)
            {
                Logger.Debug("Pin {0} is expired", status.Pin);
                throw new ResourceNotFoundException();
            }
        }

        private string GetNewPin()
        {
            var pin = GetNewPinInternal();

            while (IsPinActive(pin))
            {
                pin = GetNewPinInternal();
            }

            return pin;
        }

        private string GetNewPinInternal()
        {
            return new Random().Next(10000, 99999).ToString(CultureInfo.InvariantCulture);
        }

        private bool IsPinActive(string pin)
        {
            MyPinStatus status;

            if (!_activeRequests.TryGetValue(pin, out status))
            {
                return false;
            }

            if (status.IsExpired)
            {
                return false;
            }

            return true;
        }

        public class MyPinStatus : PinStatusResult
        {
            public DateTime CreationTimeUtc { get; set; }
            public string DeviceId { get; set; }
            public string UserId { get; set; }
            public string AppName { get; set; }
        }
    }

    public class ValidatePinResult
    {
        public string AppName { get; set; }
    }
}
