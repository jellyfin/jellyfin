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
        public string DeviceId { get; set; }
    }

    [Route("/Auth/Pin", "GET", Summary = "Gets pin status")]
    public class GetPinStatusRequest : IReturn<PinStatusResult>
    {
        public string DeviceId { get; set; }
        public string Pin { get; set; }
    }

    [Route("/Auth/Pin/Exchange", "POST", Summary = "Exchanges a pin")]
    public class ExchangePinRequest : IReturn<PinExchangeResult>
    {
        public string DeviceId { get; set; }
        public string Pin { get; set; }
    }

    [Route("/Auth/Pin/Validate", "POST", Summary = "Validates a pin")]
    [Authenticated]
    public class ValidatePinRequest : IReturnVoid
    {
        public string Pin { get; set; }
    }

    public class PinLoginService : BaseApiService
    {
        private readonly ConcurrentDictionary<string, MyPinStatus> _activeRequests = new ConcurrentDictionary<string, MyPinStatus>(StringComparer.OrdinalIgnoreCase);

        public object Post(CreatePinRequest request)
        {
            var pin = GetNewPin(5);
            var key = GetKey(request.DeviceId, pin);

            var value = new MyPinStatus
            {
                CreationTimeUtc = DateTime.UtcNow,
                IsConfirmed = false,
                IsExpired = false,
                Pin = pin
            };

            _activeRequests.AddOrUpdate(key, value, (k, v) => value);

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

            if (!_activeRequests.TryGetValue(GetKey(request.DeviceId, request.Pin), out status))
            {
                throw new ResourceNotFoundException();
            }

            CheckExpired(status);

            if (status.IsExpired)
            {
                throw new ResourceNotFoundException();
            }

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

            if (!_activeRequests.TryGetValue(GetKey(request.DeviceId, request.Pin), out status))
            {
                throw new ResourceNotFoundException();
            }

            CheckExpired(status);

            if (status.IsExpired)
            {
                throw new ResourceNotFoundException();
            }

            return ToOptimizedResult(new PinExchangeResult
            {
            });
        }

        public void Post(ValidatePinRequest request)
        {
        }

        private void CheckExpired(MyPinStatus status)
        {
            if ((DateTime.UtcNow - status.CreationTimeUtc).TotalMinutes > 10)
            {
                status.IsExpired = true;
            }
        }

        private string GetNewPin(int length)
        {
            var pin = string.Empty;

            while (pin.Length < length)
            {
                var digit = new Random().Next(0, 9);
                pin += digit.ToString(CultureInfo.InvariantCulture);
            }

            return pin;
        }

        private string GetKey(string deviceId, string pin)
        {
            return deviceId + pin;
        }

        public class MyPinStatus : PinStatusResult
        {
            public DateTime CreationTimeUtc { get; set; }
        }
    }
}
