/*****************************************************************
|
|   Platinum - HTTP Protocol Helper
|
| Copyright (c) 2004-2010, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| licensing@plutinosoft.com
|  
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltHttp.h"
#include "PltDatagramStream.h"
#include "PltVersion.h"
#include "PltUtilities.h"

NPT_SET_LOCAL_LOGGER("platinum.core.http")

/*----------------------------------------------------------------------
|   external references
+---------------------------------------------------------------------*/
extern NPT_String HttpServerHeader;

/*----------------------------------------------------------------------
|   NPT_HttpHeaderFinder
+---------------------------------------------------------------------*/
class NPT_HttpHeaderFinder
{
 public:
    // methods
    NPT_HttpHeaderFinder(const char* name) : m_Name(name) {}
    bool operator()(const NPT_HttpHeader* const & header) const {
		if (header->GetName().Compare(m_Name, true)) {
			return true;
		} else {
			return false;
		}
    }

 private:
    // members
    NPT_String m_Name;
};

/*----------------------------------------------------------------------
|   NPT_HttpHeaderPrinter
+---------------------------------------------------------------------*/
class NPT_HttpHeaderPrinter
{
public:
    // methods
    NPT_HttpHeaderPrinter(NPT_OutputStreamReference& stream) : 
        m_Stream(stream) {}
    NPT_Result operator()(NPT_HttpHeader*& header) const {
        m_Stream->WriteString(header->GetName());
        m_Stream->Write(": ", 2);
        m_Stream->WriteString(header->GetValue());
        m_Stream->Write("\r\n", 2, NULL);
        return NPT_SUCCESS;
    }

private:
    // members
    NPT_OutputStreamReference& m_Stream;
};

/*----------------------------------------------------------------------
|   NPT_HttpHeaderLogger
+---------------------------------------------------------------------*/
class NPT_HttpHeaderLogger
{
public:
    // methods
    NPT_HttpHeaderLogger(NPT_LoggerReference& logger, int level) : 
      m_Logger(logger), m_Level(level) {}
	NPT_Result operator()(NPT_HttpHeader*& header) const {
        NPT_COMPILER_UNUSED(header);

        NPT_LOG_L2(m_Logger, m_Level, "%s: %s", 
            (const char*)header->GetName(), 
            (const char*)header->GetValue());
        return NPT_SUCCESS;
    }

    NPT_LoggerReference& m_Logger;
    int                  m_Level;
};


/*----------------------------------------------------------------------
|   PLT_HttpHelper::GetContentType
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::GetContentType(const NPT_HttpMessage& message, 
                               NPT_String&            type) 
{ 
    type = "";

    const NPT_String* val = 
        message.GetHeaders().GetHeaderValue(NPT_HTTP_HEADER_CONTENT_TYPE);
    NPT_CHECK_POINTER(val);

    type = *val;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::GetContentLength
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::GetContentLength(const NPT_HttpMessage& message, 
                                 NPT_LargeSize&         len) 
{ 
    len = 0;

    const NPT_String* val = 
        message.GetHeaders().GetHeaderValue(NPT_HTTP_HEADER_CONTENT_LENGTH);
    NPT_CHECK_POINTER(val);

    return val->ToInteger64(len);
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::SetBody
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::SetBody(NPT_HttpMessage& message, 
                        NPT_String&      text, 
                        NPT_HttpEntity** entity /* = NULL */)
{
    return SetBody(message, (const char*)text, text.GetLength(), entity);
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::SetBody
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::SetBody(NPT_HttpMessage& message, 
                        const char*      text, 
                        NPT_HttpEntity** entity /* = NULL */)
{
    return SetBody(message, (const char*)text, NPT_StringLength(text), entity);
}

/*----------------------------------------------------------------------
|   NPT_HttpMessage::SetBody
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::SetBody(NPT_HttpMessage& message, 
                        const void*      body, 
                        NPT_LargeSize    len, 
                        NPT_HttpEntity** entity /* = NULL */)
{
    if (len == 0) return NPT_SUCCESS;

    // dump the body in a memory stream
    NPT_MemoryStreamReference stream(new NPT_MemoryStream);
    stream->Write(body, (NPT_Size)len);

    // set content length
    return SetBody(message, (NPT_InputStreamReference)stream, entity);
}

/*----------------------------------------------------------------------
|   NPT_HttpMessage::SetBody
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::SetBody(NPT_HttpMessage&         message, 
                        NPT_InputStreamReference stream,
                        NPT_HttpEntity**         entity /* = NULL */)
{
    // get the entity
    NPT_HttpEntity* _entity = message.GetEntity();
    if (_entity == NULL) {
        // no entity yet, create one
        message.SetEntity(_entity = new NPT_HttpEntity());
    }

    if (entity) *entity =_entity;

    // set the entity body
    return _entity->SetInputStream(stream, true);
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::GetBody
+---------------------------------------------------------------------*/
NPT_Result 
PLT_HttpHelper::GetBody(const NPT_HttpMessage& message, NPT_String& body) 
{
    NPT_Result res;
    NPT_InputStreamReference stream;

    // get stream
    NPT_HttpEntity* entity = message.GetEntity();
    if (!entity || 
        NPT_FAILED(entity->GetInputStream(stream)) || 
        stream.IsNull()) {
        return NPT_FAILURE;
    }

    // extract body
    NPT_StringOutputStream* output_stream = new NPT_StringOutputStream(&body);
    res = NPT_StreamToStreamCopy(*stream, 
                                 *output_stream, 
                                 0, 
                                 entity->GetContentLength());
    delete output_stream;
    return res;
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::ParseBody
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::ParseBody(const NPT_HttpMessage& message, 
                          NPT_XmlElementNode*&   tree) 
{
    // reset tree
    tree = NULL;

    // read body
    NPT_String body;
    NPT_CHECK_WARNING(GetBody(message, body));

    return PLT_XmlHelper::Parse(body, tree);
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::IsConnectionKeepAlive
+---------------------------------------------------------------------*/
bool
PLT_HttpHelper::IsConnectionKeepAlive(NPT_HttpMessage& message) 
{
    const NPT_String* connection = 
        message.GetHeaders().GetHeaderValue(NPT_HTTP_HEADER_CONNECTION);

    // the DLNA says that all HTTP 1.0 requests should be closed immediately by the server
    NPT_String protocol = message.GetProtocol();
    if (protocol.Compare(NPT_HTTP_PROTOCOL_1_0, true) == 0) return false;
    
    // all HTTP 1.1 requests without a Connection header 
    // or with a keep-alive Connection header should be kept alive if possible 
    return (!connection || connection->Compare("keep-alive", true) == 0);
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::IsBodyStreamSeekable
+---------------------------------------------------------------------*/
bool
PLT_HttpHelper::IsBodyStreamSeekable(NPT_HttpMessage& message)
{
    NPT_HttpEntity* entity = message.GetEntity();
    NPT_InputStreamReference stream;
    
    if (!entity || 
        NPT_FAILED(entity->GetInputStream(stream)) || 
        stream.IsNull()) {
        return true;
    }

    // try to get current position and seek there
    NPT_Position position;
    if (NPT_FAILED(stream->Tell(position)) || 
        NPT_FAILED(stream->Seek(position))) {
        return false;
    }

    return true;
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::GetHost
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::GetHost(const NPT_HttpRequest& request, NPT_String& value)    
{ 
    value = "";

    const NPT_String* val = 
        request.GetHeaders().GetHeaderValue(NPT_HTTP_HEADER_HOST);
    NPT_CHECK_POINTER(val);

    value = *val;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::SetHost
+---------------------------------------------------------------------*/
void         
PLT_HttpHelper::SetHost(NPT_HttpRequest& request, const char* host)
{ 
    request.GetHeaders().SetHeader(NPT_HTTP_HEADER_HOST, host); 
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::ToLog
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::ToLog(NPT_LoggerReference logger, 
                      int                 level,
                      const char*          prefix,
                      NPT_HttpRequest*    request)
{
    if (!request) {
        NPT_LOG_L(logger, level, "NULL HTTP Request!");
        return NPT_FAILURE;
    }

    return ToLog(logger, level, prefix, *request);
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::ToLog
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::ToLog(NPT_LoggerReference    logger, 
                      int                    level,
                      const char*            prefix, 
                      const NPT_HttpRequest& request)
{
    NPT_COMPILER_UNUSED(logger);
    NPT_COMPILER_UNUSED(level);

    NPT_StringOutputStreamReference stream(new NPT_StringOutputStream);
    NPT_OutputStreamReference output = stream;
    request.GetHeaders().GetHeaders().Apply(NPT_HttpHeaderPrinter(output));

    NPT_LOG_L5(logger, level, "%s\n%s %s %s\n%s", 
        prefix,
        (const char*)request.GetMethod(), 
        (const char*)request.GetUrl().ToRequestString(true), 
        (const char*)request.GetProtocol(),
        (const char*)stream->GetString());
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::GetDeviceSignature
+---------------------------------------------------------------------*/
PLT_DeviceSignature
PLT_HttpHelper::GetDeviceSignature(const NPT_HttpRequest& request)
{
	const NPT_String* agent  = request.GetHeaders().GetHeaderValue(NPT_HTTP_HEADER_USER_AGENT);
	const NPT_String* hdr    = request.GetHeaders().GetHeaderValue("X-AV-Client-Info");
    const NPT_String* server = request.GetHeaders().GetHeaderValue(NPT_HTTP_HEADER_SERVER);

	if ((agent && (agent->Find("XBox", 0, true) >= 0 || agent->Find("Xenon", 0, true) >= 0)) ||
        (server && server->Find("Xbox", 0, true) >= 0)) {
		return PLT_DEVICE_XBOX;
	} else if (agent && (agent->Find("Windows Media Player", 0, true) >= 0 || agent->Find("Windows-Media-Player", 0, true) >= 0 || agent->Find("Mozilla/4.0", 0, true) >= 0 || agent->Find("WMFSDK", 0, true) >= 0)) {
		return PLT_DEVICE_WMP;
	} else if (agent && (agent->Find("Sonos", 0, true) >= 0)) {
		return PLT_DEVICE_SONOS;
	} else if ((agent && agent->Find("PLAYSTATION 3", 0, true) >= 0) || 
               (hdr && hdr->Find("PLAYSTATION 3", 0, true) >= 0)) {
		return PLT_DEVICE_PS3;
	} else if (agent && agent->Find("Windows", 0, true) >= 0) {
        return PLT_DEVICE_WINDOWS;
    } else if (agent && (agent->Find("Mac", 0, true) >= 0 || agent->Find("OS X", 0, true) >= 0 || agent->Find("OSX", 0, true) >= 0)) {
        return PLT_DEVICE_MAC;
    } else if (agent && (agent->Find("VLC", 0, true) >= 0 || agent->Find("VideoLan", 0, true) >= 0)) {
        return PLT_DEVICE_VLC;
    } else {
        NPT_LOG_FINE_1("Unknown device signature (ua=%s)", agent?agent->GetChars():"none");
    }

	return PLT_DEVICE_UNKNOWN;
}

/*----------------------------------------------------------------------
|   NPT_HttpResponse::ToLog
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::ToLog(NPT_LoggerReference logger,
                      int                 level,
                      const char*         prefix, 
                      NPT_HttpResponse*   response)
{
    if (!response) {
        NPT_LOG_L(logger, level, "NULL HTTP Response!");
        return NPT_FAILURE;
    }

    return ToLog(logger, level, prefix, *response);
}

/*----------------------------------------------------------------------
|   NPT_HttpResponse::ToLog
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpHelper::ToLog(NPT_LoggerReference     logger, 
                      int                     level,
                      const char*             prefix, 
                      const NPT_HttpResponse& response)
{
    NPT_COMPILER_UNUSED(logger);
    NPT_COMPILER_UNUSED(level);

    NPT_StringOutputStreamReference stream(new NPT_StringOutputStream);
    NPT_OutputStreamReference output = stream;
    response.GetHeaders().GetHeaders().Apply(NPT_HttpHeaderPrinter(output));

    NPT_LOG_L5(logger, level, "%s\n%s %d %s\n%s", 
        prefix,
        (const char*)response.GetProtocol(), 
        response.GetStatusCode(), 
        (const char*)response.GetReasonPhrase(),
        (const char*)stream->GetString());
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpHelper::SetBasicAuthorization
+---------------------------------------------------------------------*/
void         
PLT_HttpHelper::SetBasicAuthorization(NPT_HttpRequest& request, 
                                      const char*      username, 
                                      const char*      password)
{ 
	NPT_String encoded;
	NPT_String cred =  NPT_String(username) + ":" + password;

	NPT_Base64::Encode((const NPT_Byte *)cred.GetChars(), cred.GetLength(), encoded);
	request.GetHeaders().SetHeader(NPT_HTTP_HEADER_AUTHORIZATION, NPT_String("Basic " + encoded)); 
}

