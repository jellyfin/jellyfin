namespace SocketHttpListener.Net
{
  /// <summary>
  /// Contains the values of the HTTP status codes.
  /// </summary>
  /// <remarks>
  /// The HttpStatusCode enumeration contains the values of the HTTP status codes defined in
  /// <see href="http://tools.ietf.org/html/rfc2616#section-10">RFC 2616</see> for HTTP 1.1.
  /// </remarks>
  public enum HttpStatusCode
  {
    /// <summary>
    /// Equivalent to status code 100.
    /// Indicates that the client should continue with its request.
    /// </summary>
    Continue = 100,
    /// <summary>
    /// Equivalent to status code 101.
    /// Indicates that the server is switching the HTTP version or protocol on the connection.
    /// </summary>
    SwitchingProtocols = 101,
    /// <summary>
    /// Equivalent to status code 200.
    /// Indicates that the client's request has succeeded.
    /// </summary>
    OK = 200,
    /// <summary>
    /// Equivalent to status code 201.
    /// Indicates that the client's request has been fulfilled and resulted in a new resource being
    /// created.
    /// </summary>
    Created = 201,
    /// <summary>
    /// Equivalent to status code 202.
    /// Indicates that the client's request has been accepted for processing, but the processing
    /// hasn't been completed.
    /// </summary>
    Accepted = 202,
    /// <summary>
    /// Equivalent to status code 203.
    /// Indicates that the returned metainformation is from a local or a third-party copy instead of
    /// the origin server.
    /// </summary>
    NonAuthoritativeInformation = 203,
    /// <summary>
    /// Equivalent to status code 204.
    /// Indicates that the server has fulfilled the client's request but doesn't need to return
    /// an entity-body.
    /// </summary>
    NoContent = 204,
    /// <summary>
    /// Equivalent to status code 205.
    /// Indicates that the server has fulfilled the client's request, and the user agent should
    /// reset the document view which caused the request to be sent.
    /// </summary>
    ResetContent = 205,
    /// <summary>
    /// Equivalent to status code 206.
    /// Indicates that the server has fulfilled the partial GET request for the resource.
    /// </summary>
    PartialContent = 206,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 300.
    ///   Indicates that the requested resource corresponds to any of multiple representations.
    ///   </para>
    ///   <para>
    ///   MultipleChoices is a synonym for Ambiguous.
    ///   </para>
    /// </summary>
    MultipleChoices = 300,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 300.
    ///   Indicates that the requested resource corresponds to any of multiple representations.
    ///   </para>
    ///   <para>
    ///   Ambiguous is a synonym for MultipleChoices.
    ///   </para>
    /// </summary>
    Ambiguous = 300,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 301.
    ///   Indicates that the requested resource has been assigned a new permanent URI and
    ///   any future references to this resource should use one of the returned URIs.
    ///   </para>
    ///   <para>
    ///   MovedPermanently is a synonym for Moved.
    ///   </para>
    /// </summary>
    MovedPermanently = 301,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 301.
    ///   Indicates that the requested resource has been assigned a new permanent URI and
    ///   any future references to this resource should use one of the returned URIs.
    ///   </para>
    ///   <para>
    ///   Moved is a synonym for MovedPermanently.
    ///   </para>
    /// </summary>
    Moved = 301,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 302.
    ///   Indicates that the requested resource is located temporarily under a different URI.
    ///   </para>
    ///   <para>
    ///   Found is a synonym for Redirect.
    ///   </para>
    /// </summary>
    Found = 302,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 302.
    ///   Indicates that the requested resource is located temporarily under a different URI.
    ///   </para>
    ///   <para>
    ///   Redirect is a synonym for Found.
    ///   </para>
    /// </summary>
    Redirect = 302,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 303.
    ///   Indicates that the response to the request can be found under a different URI and
    ///   should be retrieved using a GET method on that resource.
    ///   </para>
    ///   <para>
    ///   SeeOther is a synonym for RedirectMethod.
    ///   </para>
    /// </summary>
    SeeOther = 303,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 303.
    ///   Indicates that the response to the request can be found under a different URI and
    ///   should be retrieved using a GET method on that resource.
    ///   </para>
    ///   <para>
    ///   RedirectMethod is a synonym for SeeOther.
    ///   </para>
    /// </summary>
    RedirectMethod = 303,
    /// <summary>
    /// Equivalent to status code 304.
    /// Indicates that the client has performed a conditional GET request and access is allowed,
    /// but the document hasn't been modified.
    /// </summary>
    NotModified = 304,
    /// <summary>
    /// Equivalent to status code 305.
    /// Indicates that the requested resource must be accessed through the proxy given by
    /// the Location field.
    /// </summary>
    UseProxy = 305,
    /// <summary>
    /// Equivalent to status code 306.
    /// This status code was used in a previous version of the specification, is no longer used,
    /// and is reserved for future use.
    /// </summary>
    Unused = 306,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 307.
    ///   Indicates that the requested resource is located temporarily under a different URI.
    ///   </para>
    ///   <para>
    ///   TemporaryRedirect is a synonym for RedirectKeepVerb.
    ///   </para>
    /// </summary>
    TemporaryRedirect = 307,
    /// <summary>
    ///   <para>
    ///   Equivalent to status code 307.
    ///   Indicates that the requested resource is located temporarily under a different URI.
    ///   </para>
    ///   <para>
    ///   RedirectKeepVerb is a synonym for TemporaryRedirect.
    ///   </para>
    /// </summary>
    RedirectKeepVerb = 307,
    /// <summary>
    /// Equivalent to status code 400.
    /// Indicates that the client's request couldn't be understood by the server due to
    /// malformed syntax.
    /// </summary>
    BadRequest = 400,
    /// <summary>
    /// Equivalent to status code 401.
    /// Indicates that the client's request requires user authentication.
    /// </summary>
    Unauthorized = 401,
    /// <summary>
    /// Equivalent to status code 402.
    /// This status code is reserved for future use.
    /// </summary>
    PaymentRequired = 402,
    /// <summary>
    /// Equivalent to status code 403.
    /// Indicates that the server understood the client's request but is refusing to fulfill it.
    /// </summary>
    Forbidden = 403,
    /// <summary>
    /// Equivalent to status code 404.
    /// Indicates that the server hasn't found anything matching the request URI.
    /// </summary>
    NotFound = 404,
    /// <summary>
    /// Equivalent to status code 405.
    /// Indicates that the method specified in the request line isn't allowed for the resource
    /// identified by the request URI.
    /// </summary>
    MethodNotAllowed = 405,
    /// <summary>
    /// Equivalent to status code 406.
    /// Indicates that the server doesn't have the appropriate resource to respond to the Accept
    /// headers in the client's request.
    /// </summary>
    NotAcceptable = 406,
    /// <summary>
    /// Equivalent to status code 407.
    /// Indicates that the client must first authenticate itself with the proxy.
    /// </summary>
    ProxyAuthenticationRequired = 407,
    /// <summary>
    /// Equivalent to status code 408.
    /// Indicates that the client didn't produce a request within the time that the server was
    /// prepared to wait.
    /// </summary>
    RequestTimeout = 408,
    /// <summary>
    /// Equivalent to status code 409.
    /// Indicates that the client's request couldn't be completed due to a conflict on the server.
    /// </summary>
    Conflict = 409,
    /// <summary>
    /// Equivalent to status code 410.
    /// Indicates that the requested resource is no longer available at the server and
    /// no forwarding address is known.
    /// </summary>
    Gone = 410,
    /// <summary>
    /// Equivalent to status code 411.
    /// Indicates that the server refuses to accept the client's request without a defined
    /// Content-Length.
    /// </summary>
    LengthRequired = 411,
    /// <summary>
    /// Equivalent to status code 412.
    /// Indicates that the precondition given in one or more of the request headers evaluated to
    /// false when it was tested on the server.
    /// </summary>
    PreconditionFailed = 412,
    /// <summary>
    /// Equivalent to status code 413.
    /// Indicates that the entity of the client's request is larger than the server is willing or
    /// able to process.
    /// </summary>
    RequestEntityTooLarge = 413,
    /// <summary>
    /// Equivalent to status code 414.
    /// Indicates that the request URI is longer than the server is willing to interpret.
    /// </summary>
    RequestUriTooLong = 414,
    /// <summary>
    /// Equivalent to status code 415.
    /// Indicates that the entity of the client's request is in a format not supported by
    /// the requested resource for the requested method.
    /// </summary>
    UnsupportedMediaType = 415,
    /// <summary>
    /// Equivalent to status code 416.
    /// Indicates that none of the range specifier values in a Range request header overlap
    /// the current extent of the selected resource.
    /// </summary>
    RequestedRangeNotSatisfiable = 416,
    /// <summary>
    /// Equivalent to status code 417.
    /// Indicates that the expectation given in an Expect request header couldn't be met by
    /// the server.
    /// </summary>
    ExpectationFailed = 417,
    /// <summary>
    /// Equivalent to status code 500.
    /// Indicates that the server encountered an unexpected condition which prevented it from
    /// fulfilling the client's request.
    /// </summary>
    InternalServerError = 500,
    /// <summary>
    /// Equivalent to status code 501.
    /// Indicates that the server doesn't support the functionality required to fulfill the client's
    /// request.
    /// </summary>
    NotImplemented = 501,
    /// <summary>
    /// Equivalent to status code 502.
    /// Indicates that a gateway or proxy server received an invalid response from the upstream
    /// server.
    /// </summary>
    BadGateway = 502,
    /// <summary>
    /// Equivalent to status code 503.
    /// Indicates that the server is currently unable to handle the client's request due to
    /// a temporary overloading or maintenance of the server.
    /// </summary>
    ServiceUnavailable = 503,
    /// <summary>
    /// Equivalent to status code 504.
    /// Indicates that a gateway or proxy server didn't receive a timely response from the upstream
    /// server or some other auxiliary server.
    /// </summary>
    GatewayTimeout = 504,
    /// <summary>
    /// Equivalent to status code 505.
    /// Indicates that the server doesn't support the HTTP version used in the client's request.
    /// </summary>
    HttpVersionNotSupported = 505,
  }
}
