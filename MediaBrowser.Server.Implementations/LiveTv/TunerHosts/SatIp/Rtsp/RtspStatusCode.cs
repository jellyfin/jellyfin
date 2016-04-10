/*  
    Copyright (C) <2007-2016>  <Kay Diefenthal>

    SatIp.RtspSample is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SatIp.RtspSample is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SatIp.RtspSample.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.ComponentModel;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp.Rtsp
{
    /// <summary>
    /// Standard RTSP status codes.
    /// </summary>
    public enum RtspStatusCode
    {
        /// <summary>
        /// 100 continue
        /// </summary>
        Continue = 100,

        /// <summary>
        /// 200 OK
        /// </summary>
        [Description("Okay")]
        Ok = 200,
        /// <summary>
        /// 201 created
        /// </summary>
        Created = 201,

        /// <summary>
        /// 250 low on storage space
        /// </summary>
        [Description("Low On Storage Space")]
        LowOnStorageSpace = 250,

        /// <summary>
        /// 300 multiple choices
        /// </summary>
        [Description("Multiple Choices")]
        MultipleChoices = 300,
        /// <summary>
        /// 301 moved permanently
        /// </summary>
        [Description("Moved Permanently")]
        MovedPermanently = 301,
        /// <summary>
        /// 302 moved temporarily
        /// </summary>
        [Description("Moved Temporarily")]
        MovedTemporarily = 302,
        /// <summary>
        /// 303 see other
        /// </summary>
        [Description("See Other")]
        SeeOther = 303,
        /// <summary>
        /// 304 not modified
        /// </summary>
        [Description("Not Modified")]
        NotModified = 304,
        /// <summary>
        /// 305 use proxy
        /// </summary>
        [Description("Use Proxy")]
        UseProxy = 305,

        /// <summary>
        /// 400 bad request
        /// </summary>
        [Description("Bad Request")]
        BadRequest = 400,
        /// <summary>
        /// 401 unauthorised
        /// </summary>
        Unauthorised = 401,
        /// <summary>
        /// 402 payment required
        /// </summary>
        [Description("Payment Required")]
        PaymentRequired = 402,
        /// <summary>
        /// 403 forbidden
        /// </summary>
        Forbidden = 403,
        /// <summary>
        /// 404 not found
        /// </summary>
        [Description("Not Found")]
        NotFound = 404,
        /// <summary>
        /// 405 method not allowed
        /// </summary>
        [Description("Method Not Allowed")]
        MethodNotAllowed = 405,
        /// <summary>
        /// 406 not acceptable
        /// </summary>
        [Description("Not Acceptable")]
        NotAcceptable = 406,
        /// <summary>
        /// 407 proxy authentication required
        /// </summary>
        [Description("Proxy Authentication Required")]
        ProxyAuthenticationRequred = 407,
        /// <summary>
        /// 408 request time-out
        /// </summary>
        [Description("Request Time-Out")]
        RequestTimeOut = 408,

        /// <summary>
        /// 410 gone
        /// </summary>
        Gone = 410,
        /// <summary>
        /// 411 length required
        /// </summary>
        [Description("Length Required")]
        LengthRequired = 411,
        /// <summary>
        /// 412 precondition failed
        /// </summary>
        [Description("Precondition Failed")]
        PreconditionFailed = 412,
        /// <summary>
        /// 413 request entity too large
        /// </summary>
        [Description("Request Entity Too Large")]
        RequestEntityTooLarge = 413,
        /// <summary>
        /// 414 request URI too large
        /// </summary>
        [Description("Request URI Too Large")]
        RequestUriTooLarge = 414,
        /// <summary>
        /// 415 unsupported media type
        /// </summary>
        [Description("Unsupported Media Type")]
        UnsupportedMediaType = 415,

        /// <summary>
        /// 451 parameter not understood
        /// </summary>
        [Description("Parameter Not Understood")]
        ParameterNotUnderstood = 451,
        /// <summary>
        /// 452 conference not found
        /// </summary>
        [Description("Conference Not Found")]
        ConferenceNotFound = 452,
        /// <summary>
        /// 453 not enough bandwidth
        /// </summary>
        [Description("Not Enough Bandwidth")]
        NotEnoughBandwidth = 453,
        /// <summary>
        /// 454 session not found
        /// </summary>
        [Description("Session Not Found")]
        SessionNotFound = 454,
        /// <summary>
        /// 455 method not valid in this state
        /// </summary>
        [Description("Method Not Valid In This State")]
        MethodNotValidInThisState = 455,
        /// <summary>
        /// 456 header field not valid for this resource
        /// </summary>
        [Description("Header Field Not Valid For This Resource")]
        HeaderFieldNotValidForThisResource = 456,
        /// <summary>
        /// 457 invalid range
        /// </summary>
        [Description("Invalid Range")]
        InvalidRange = 457,
        /// <summary>
        /// 458 parameter is read-only
        /// </summary>
        [Description("Parameter Is Read-Only")]
        ParameterIsReadOnly = 458,
        /// <summary>
        /// 459 aggregate operation not allowed
        /// </summary>
        [Description("Aggregate Operation Not Allowed")]
        AggregateOperationNotAllowed = 459,
        /// <summary>
        /// 460 only aggregate operation allowed
        /// </summary>
        [Description("Only Aggregate Operation Allowed")]
        OnlyAggregateOperationAllowed = 460,
        /// <summary>
        /// 461 unsupported transport
        /// </summary>
        [Description("Unsupported Transport")]
        UnsupportedTransport = 461,
        /// <summary>
        /// 462 destination unreachable
        /// </summary>
        [Description("Destination Unreachable")]
        DestinationUnreachable = 462,

        /// <summary>
        /// 500 internal server error
        /// </summary>
        [Description("Internal Server Error")]
        InternalServerError = 500,
        /// <summary>
        /// 501 not implemented
        /// </summary>
        [Description("Not Implemented")]
        NotImplemented = 501,
        /// <summary>
        /// 502 bad gateway
        /// </summary>
        [Description("Bad Gateway")]
        BadGateway = 502,
        /// <summary>
        /// 503 service unavailable
        /// </summary>
        [Description("Service Unavailable")]
        ServiceUnavailable = 503,
        /// <summary>
        /// 504 gateway time-out
        /// </summary>
        [Description("Gateway Time-Out")]
        GatewayTimeOut = 504,
        /// <summary>
        /// 505 RTSP version not supported
        /// </summary>
        [Description("RTSP Version Not Supported")]
        RtspVersionNotSupported = 505,

        /// <summary>
        /// 551 option not supported
        /// </summary>
        [Description("Option Not Supported")]
        OptionNotSupported = 551
    }
}
