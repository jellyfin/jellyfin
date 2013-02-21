/*****************************************************************
|
|   Platinum - AV Media Protocol Info
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

/** @file
 UPnP AV Media Object Protocol Info.
 */

#ifndef _PLT_PROTOCOL_INFO_H_
#define _PLT_PROTOCOL_INFO_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttp.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_HttpRequestContext;

/*----------------------------------------------------------------------
|   typedefs
+---------------------------------------------------------------------*/
typedef struct PLT_HttpFileRequestHandler_DefaultDlnaExtMapEntry {
    const char* mime_type;
    const char* dlna_ext;
} PLT_HttpFileRequestHandler_DefaultDlnaExtMapEntry ;

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo
+---------------------------------------------------------------------*/
/**
 The PLT_ProtocolInfo class holds information about the protocol info of a 
 given UPnP Media Item resource.
 */
class PLT_ProtocolInfo
{
public:
    class FieldEntry {
    public:
        FieldEntry(const char* key, const char* value) :
            m_Key(key), m_Value(value) {}
        NPT_String m_Key;
        NPT_String m_Value;
    };

    // class methods
    static NPT_String  GetMimeTypeFromProtocolInfo(const char* protocol_info);
    
    static const char* GetDlnaExtension(const char* mime_type, 
                                        const PLT_HttpRequestContext* context = NULL);
    static const char* GetDlnaExtension(const char* mime_type, 
                                        PLT_DeviceSignature signature = PLT_DEVICE_UNKNOWN);
    
    static PLT_ProtocolInfo GetProtocolInfo(const char* filename, 
                                            bool with_dlna_extension = true, 
                                            const PLT_HttpRequestContext* context = NULL);
    static PLT_ProtocolInfo GetProtocolInfo(const char* filename, 
                                            bool with_dlna_extension = true, 
                                            PLT_DeviceSignature signature = PLT_DEVICE_UNKNOWN);
    
    static PLT_ProtocolInfo GetProtocolInfoFromMimeType(const char* mime_type, 
                                                        bool with_dlna_extension = true, 
                                                        const PLT_HttpRequestContext* context = NULL);
    static PLT_ProtocolInfo GetProtocolInfoFromMimeType(const char* mime_type, 
                                                        bool with_dlna_extension = true, 
                                                        PLT_DeviceSignature signature = PLT_DEVICE_UNKNOWN);

    // methods
    PLT_ProtocolInfo();
    //PLT_ProtocolInfo(NPT_String protocol_info);
    PLT_ProtocolInfo(const char* protocol_info);
    PLT_ProtocolInfo(const char* protocol,
                     const char* mask,
                     const char* content_type,
                     const char* extra);
    const NPT_String& GetProtocol()     const { return m_Protocol;  }
    const NPT_String& GetMask()         const { return m_Mask; }
    const NPT_String& GetContentType()  const { return m_ContentType;  }
    const NPT_String& GetExtra()        const { return m_Extra; }
    
    const NPT_String& GetDLNA_PN()      const { return m_DLNA_PN; }

    bool IsValid() { return m_Valid; }

    NPT_String ToString() const;

    bool Match(const PLT_ProtocolInfo& other) const;

private:
	typedef enum {
		PLT_PROTINFO_PARSER_STATE_START,
		PLT_PROTINFO_PARSER_STATE_PN,
		PLT_PROTINFO_PARSER_STATE_OP,
		PLT_PROTINFO_PARSER_STATE_PS,
		PLT_PROTINFO_PARSER_STATE_CI,
		PLT_PROTINFO_PARSER_STATE_FLAGS,
		PLT_PROTINFO_PARSER_STATE_MAXSP,
		PLT_PROTINFO_PARSER_STATE_OTHER
	} PLT_ProtocolInfoParserState;

    NPT_Result SetProtocolInfo(const char* protocol_info);
    NPT_Result ValidateField(const char*  val, 
                        const char*  valid_chars, 
                        NPT_Cardinal num_chars = 0); // 0 means variable number of chars
    NPT_Result ParseExtra(NPT_List<FieldEntry>& entries);
    NPT_Result ValidateExtra();

private:
    NPT_String           m_Protocol;
    NPT_String           m_Mask;
    NPT_String           m_ContentType;
    NPT_String           m_Extra;
    
    NPT_String           m_DLNA_PN;    // DLNA.ORG_PN Parameter (pn-param)
    NPT_String           m_DLNA_OP;    // Operations Parameter (op-param)
    NPT_String           m_DLNA_PS;    // Server-Side PlaySpeeds Parameter (ps-param)
    NPT_String           m_DLNA_CI;    // Conversion Indicator Flag (ci-param)
    NPT_String           m_DLNA_FLAGS; // Flags Parameter (flags-param)
    NPT_String           m_DLNA_MAXSP; // Maximum RTSP Speed Header value (maxsp-param)
    NPT_List<FieldEntry> m_DLNA_OTHER; // Vendor-defined 4th field Parameters (other-param)
    
    bool                 m_Valid;
};

#endif /* _PLT_PROTOCOL_INFO_H_ */
