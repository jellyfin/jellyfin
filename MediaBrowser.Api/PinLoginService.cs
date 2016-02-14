using System;
using System.Collections.Concurrent;
using System.Globalization;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Connect;
using ServiceStack;

namespace MediaBrowser.Api
{
    [Route("/Auth/Pin", "POST", Summary = "Creates a pin request")]
    public class CreatePinRequest : IReturn<PinCreationResult>
    {
        [ApiMember(Name = "DeviceId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string DeviceId { get; set; }
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
    public class ValidatePinRequest : IReturnVoid
    {
        [ApiMember(Name = "Pin", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Pin { get; set; }
    }

    public class PinLoginService : BaseApiService
    {
        private readonly ConcurrentDictionary<string, MyPinStatus> _activeRequests = new ConcurrentDictionary<string, MyPinStatus>(StringComparer.OrdinalIgnoreCase);

        public object Post(CreatePinRequest request)
        {
            var pin = GetNewPin();

            var value = new MyPinStatus
            {
                CreationTimeUtc = DateTime.UtcNow,
                IsConfirmed = false,
                IsExpired = false,
                Pin = pin,
                DeviceId = request.DeviceId
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

        public object Post(ExchangePinRequest request)
        {
            MyPinStatus status;

            if (!_activeRequests.TryGetValue(request.Pin, out status))
            {
                throw new ResourceNotFoundException();
            }

            EnsureValid(request.DeviceId, status);

            if (!status.IsConfirmed)
            {
                throw new ResourceNotFoundException();
            }

            return ToOptimizedResult(new PinExchangeResult
            {
                // TODO: Add access token
                UserId = status.UserId
            });
        }

        public void Post(ValidatePinRequest request)
        {
            MyPinStatus status;

            if (!_activeRequests.TryGetValue(request.Pin, out status))
            {
                throw new ResourceNotFoundException();
            }

            EnsureValid(status);

            status.IsConfirmed = true;
            status.UserId = AuthorizationContext.GetAuthorizationInfo(Request).UserId;
        }

        private void EnsureValid(string requestedDeviceId, MyPinStatus status)
        {
            if (!string.Equals(requestedDeviceId, status.DeviceId, StringComparison.OrdinalIgnoreCase))
            {
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
            var length = 5;
            var pin = string.Empty;

            while (pin.Length < length)
            {
                var digit = new Random().Next(0, 9);
                pin += digit.ToString(CultureInfo.InvariantCulture);
            }

            return pin;
        }

        private bool IsPinActive(string pin)
        {
            MyPinStatus status;

            if (!_activeRequests.TryGetValue(pin, out status))
            {
                return true;
            }

            if (status.IsExpired)
            {
                return true;
            }

            return false;
        }

        public class MyPinStatus : PinStatusResult
        {
            public DateTime CreationTimeUtc { get; set; }
            public string DeviceId { get; set; }
            public string UserId { get; set; }
        }
    }
}
