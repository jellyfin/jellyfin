using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHttpListener.Net.WebSockets
{
    public enum WebSocketCloseStatus
    {
        NormalClosure = 1000,
        EndpointUnavailable = 1001,
        ProtocolError = 1002,
        InvalidMessageType = 1003,
        Empty = 1005,
        // AbnormalClosure = 1006, // 1006 is reserved and should never be used by user
        InvalidPayloadData = 1007,
        PolicyViolation = 1008,
        MessageTooBig = 1009,
        MandatoryExtension = 1010,
        InternalServerError = 1011
        // TLSHandshakeFailed = 1015, // 1015 is reserved and should never be used by user

        // 0 - 999 Status codes in the range 0-999 are not used.
        // 1000 - 1999 Status codes in the range 1000-1999 are reserved for definition by this protocol.
        // 2000 - 2999 Status codes in the range 2000-2999 are reserved for use by extensions.
        // 3000 - 3999 Status codes in the range 3000-3999 MAY be used by libraries and frameworks. The 
        //             interpretation of these codes is undefined by this protocol. End applications MUST 
        //             NOT use status codes in this range.       
        // 4000 - 4999 Status codes in the range 4000-4999 MAY be used by application code. The interpretation
        //             of these codes is undefined by this protocol.
    }
}
