/*****************************************************************
|
|   Platinum - AV Media Item
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
#include "PltProtocolInfo.h"
#include "PltMimeType.h"
#include "PltHttp.h"

NPT_SET_LOCAL_LOGGER("platinum.media.server.protocolinfo")

/*----------------------------------------------------------------------
|   defines
+---------------------------------------------------------------------*/
#define PLT_FIELD_ALPHA "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
#define PLT_FIELD_NUM   "0123456789"

#define PLT_SP_FLAG_MASK                0x80000000
#define PLT_LOP_NPT_MASK                0x40000000
#define PLT_LOP_BYTES_MASK              0x20000000
#define PLT_PLAYCONTAINER_PARAM_MASK    0x10000000
#define PLT_S0_INCREASING_MASK          0x08000000
#define PLT_SN_INCREASING_MASK          0x04000000
#define PLT_RTSP_PAUSE_MASK             0x02000000
#define PLT_TM_S_MASK                   0x01000000
#define PLT_TM_I_MASK                   0x00800000
#define PLT_TM_B_MASK                   0x00400000
#define PLT_HTTP_STALLING_MASK          0x00200000
#define PLT_DLNA_V1_5_FLAG_MASK         0x00100000
#define PLT_LP_FLAG_MASK                0x00010000
#define PLT_CLEARTEXTBYTESEEK_FULL_MASK 0x00008000
#define PLT_LOP_CLEARTEXTBYTES_MASK     0x00004000


/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
static const char PLT_DLNAPNCharsToValidate[]         = PLT_FIELD_ALPHA PLT_FIELD_NUM "_";
static const char PLT_DLNAPSCharsToValidate[]         = PLT_FIELD_NUM "-,/";
static const char PLT_DLNAFlagCharsToValidate[]       = "01";
static const char PLT_DLNAHexCharsToValidate[]        = PLT_FIELD_NUM "ABCDEFabcdef";
static const char PLT_DLNAOTherNameCharsToValidate[]  = PLT_FIELD_ALPHA PLT_FIELD_NUM;
static const char PLT_DLNAOTherValueCharsToValidate[] = PLT_FIELD_ALPHA PLT_FIELD_NUM "_-+,";

static const PLT_HttpFileRequestHandler_DefaultDlnaExtMapEntry 
PLT_HttpFileRequestHandler_DefaultDlnaMap[] = {
    {"image/gif",      "DLNA.ORG_PN=GIF_LRG"},
    {"image/jpeg",     "DLNA.ORG_PN=JPEG_LRG"},
    {"image/jp2",      "DLNA.ORG_PN=JPEG_LRG"},
    {"image/png",      "DLNA.ORG_PN=PNG_LRG"},
    {"image/bmp",      "DLNA.ORG_PN=BMP_LRG"},
    {"image/tiff",     "DLNA.ORG_PN=TIFF_LRG"},
    {"audio/L16;rate=44100;channels=2", "DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/L16;rate=44100;channels=1", "DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/L16;rate=32000;channels=1", "DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/mpeg",     "DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/mp4",      "DLNA.ORG_PN=AAC_ISO;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/x-ms-wma", "DLNA.ORG_PN=WMABASE;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/wav",      "DLNA.ORG_OP=01;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01500000000000000000000000000000"}, // UVerse
    {"audio/x-wav",    "DLNA.ORG_OP=01;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/avi",      "DLNA.ORG_PN=AVI;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/mp4",      "DLNA.ORG_PN=MPEG4_P2_SP_AAC;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/mpeg",     "DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/quicktime","DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/x-ms-wmv", "DLNA.ORG_PN=WMVHIGH_BASE;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/x-msvideo","DLNA.ORG_PN=AVI;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/x-ms-asf", "DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/x-matroska","DLNA.ORG_OP=01;DLNA.ORG_CI=0"},
    {"video/x-flv",    "DLNA.ORG_OP=01;DLNA.ORG_CI=0"},
};

static const PLT_HttpFileRequestHandler_DefaultDlnaExtMapEntry 
PLT_HttpFileRequestHandler_360DlnaMap[] = {
    {"video/quicktime","DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"video/mp4",      "DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/wav",      "DLNA.ORG_OP=01;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/mp4",      "DLNA.ORG_PN=AAC_ISO;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01500000000000000000000000000000"},
    {"audio/mpeg",     "DLNA.ORG_PN=MP3;DLNA.ORG_OP=01"},
    {"audio/L16",      "DLNA.ORG_PN=LPCM;DLNA.ORG_OP=01;DLNA.ORG_CI=1"},
    {"audio/x-ms-wma", "DLNA.ORG_PN=WMABASE;DLNA.ORG_OP=01;DLNA.ORG_CI=0"}
};

static const PLT_HttpFileRequestHandler_DefaultDlnaExtMapEntry 
PLT_HttpFileRequestHandler_SonosDlnaMap[] = {
    {"audio/wav",      "*"}
};

static const PLT_HttpFileRequestHandler_DefaultDlnaExtMapEntry 
PLT_HttpFileRequestHandler_PS3DlnaMap[] = {
    {"image/jpg",  "DLNA.ORG_OP=01"},
    {"image/png",  "DLNA.ORG_OP=01"}
};

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::PLT_ProtocolInfo
+---------------------------------------------------------------------*/
PLT_ProtocolInfo::PLT_ProtocolInfo() :
    m_Valid(false)
{
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::PLT_ProtocolInfo
+---------------------------------------------------------------------*/
/*PLT_ProtocolInfo::PLT_ProtocolInfo(NPT_String protocol_info) :
    m_Valid(false)
{
    SetProtocolInfo(protocol_info);
}*/

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::PLT_ProtocolInfo
+---------------------------------------------------------------------*/
PLT_ProtocolInfo::PLT_ProtocolInfo(const char* protocol_info) :
    m_Valid(false)
{
    SetProtocolInfo(protocol_info);
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::PLT_ProtocolInfo
+---------------------------------------------------------------------*/
PLT_ProtocolInfo::PLT_ProtocolInfo(const char* protocol,
                                   const char* mask,
                                   const char* content_type,
                                   const char* extra) :
    m_Protocol(protocol),
    m_Mask(mask),
    m_ContentType(content_type),
    m_Extra(extra),
    m_Valid(false)
{
    ValidateExtra();
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::SetProtocolInfo
+---------------------------------------------------------------------*/
NPT_Result
PLT_ProtocolInfo::SetProtocolInfo(const char* protocol_info)
{
    if (!protocol_info || protocol_info[0] == '\0') 
        return NPT_ERROR_INVALID_PARAMETERS;
    
    NPT_List<NPT_String> parts = NPT_String(protocol_info).Split(":");
    if (parts.GetItemCount() != 4) 
        return NPT_ERROR_INVALID_SYNTAX;
    
    NPT_List<NPT_String>::Iterator part = parts.GetFirstItem();
    m_Protocol    = *part++;
    m_Mask        = *part++;
    m_ContentType = *part++;
    m_Extra       = *part;
    
    return ValidateExtra();
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::ParseExtra
+---------------------------------------------------------------------*/
NPT_Result
PLT_ProtocolInfo::ParseExtra(NPT_List<FieldEntry>& entries)
{
    if (m_Extra == "*") return NPT_SUCCESS;
    
    // remove extra characters which could cause parsing errors
    m_Extra.Trim(";");
    
    NPT_List<NPT_String> fields = m_Extra.Split(";");
    NPT_List<NPT_String>::Iterator field = fields.GetFirstItem();
    if (!field) NPT_CHECK_SEVERE(NPT_ERROR_INVALID_SYNTAX);

    while (field) {
        NPT_List<NPT_String> entry = (*field).Split("=");
        if (entry.GetItemCount() != 2) NPT_CHECK_SEVERE(NPT_ERROR_INVALID_SYNTAX);
        entries.Add(FieldEntry(*entry.GetFirstItem(), *entry.GetLastItem()));
        ++field;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::ValidateField
+---------------------------------------------------------------------*/
NPT_Result
PLT_ProtocolInfo::ValidateField(const char*  val, 
                                const char*  valid_chars, 
                                NPT_Cardinal num_chars /* = 0 */)
{
    if (!valid_chars || !val || val[0] == '\0') 
        return NPT_ERROR_INVALID_PARAMETERS;
    
    // shortcut
    if (num_chars && NPT_StringLength(val) != num_chars)
        return NPT_ERROR_INVALID_SYNTAX;

    while (val) {
        char c = *val++;
        if (c == '\0') return NPT_SUCCESS;

        // look for character in valid chars
        const char* p = valid_chars;
        while (*p != c && ++p) {};

        // reached end of valid chars means we didn't find it
        if (!p) break;
    }

    return NPT_ERROR_INVALID_SYNTAX;

}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::ValidateExtra
+---------------------------------------------------------------------*/
NPT_Result
PLT_ProtocolInfo::ValidateExtra()
{
    if (m_Extra != "*") {
		m_Valid = false;

        NPT_List<FieldEntry> entries;
        NPT_CHECK(ParseExtra(entries));

        NPT_List<FieldEntry>::Iterator entry = 
            entries.GetFirstItem();

        // parse other optional fields
        PLT_ProtocolInfoParserState state = PLT_PROTINFO_PARSER_STATE_START;
        for (;entry;entry++) {
            if (entry->m_Key == "DLNA.ORG_PN") {
                // pn-param only allowed as first param
                if (state > PLT_PROTINFO_PARSER_STATE_START) {
                    NPT_CHECK_LABEL_SEVERE(NPT_ERROR_INVALID_SYNTAX, failure);
                }
                
                NPT_CHECK_LABEL_SEVERE(ValidateField(
                    entry->m_Value, 
                    PLT_DLNAPNCharsToValidate), failure);

                m_DLNA_PN = entry->m_Value;
                state = PLT_PROTINFO_PARSER_STATE_PN;
                continue;
            } else if (entry->m_Key == "DLNA.ORG_OP") {
                // op-param only allowed after pn-param
                if (state > PLT_PROTINFO_PARSER_STATE_PN) {
                    NPT_CHECK_LABEL_SEVERE(NPT_ERROR_INVALID_SYNTAX, failure);
                }

                // validate value
                NPT_CHECK_LABEL_SEVERE(ValidateField(
                    entry->m_Value, 
                    PLT_DLNAFlagCharsToValidate, 
                    2), failure);

                m_DLNA_OP = entry->m_Value;
                state = PLT_PROTINFO_PARSER_STATE_OP;
                continue;
            } else if (entry->m_Key == "DLNA.ORG_PS") {
                // ps-param only allowed after op-param
                if (state > PLT_PROTINFO_PARSER_STATE_OP) {
                    NPT_CHECK_LABEL_SEVERE(NPT_ERROR_INVALID_SYNTAX, failure);
                }

                // validate value
                NPT_CHECK_LABEL_SEVERE(ValidateField(
                    entry->m_Value, 
                    PLT_DLNAPSCharsToValidate), failure);

                m_DLNA_PS = entry->m_Value;
                state = PLT_PROTINFO_PARSER_STATE_PS;
                continue;
            } else if (entry->m_Key == "DLNA.ORG_CI") {
                // ci-param only allowed after ps-param
                if (state > PLT_PROTINFO_PARSER_STATE_PS) {
                    NPT_CHECK_LABEL_SEVERE(NPT_ERROR_INVALID_SYNTAX, failure);
                }

                // validate value
                NPT_CHECK_LABEL_SEVERE(ValidateField(
                    entry->m_Value, 
                    PLT_DLNAFlagCharsToValidate, 
                    1), failure);

                m_DLNA_CI = entry->m_Value;
                state = PLT_PROTINFO_PARSER_STATE_CI;
                continue;
            } else if (entry->m_Key == "DLNA.ORG_FLAGS") {
                // flags-param only allowed after ci-param
                if (state > PLT_PROTINFO_PARSER_STATE_CI) {
                    NPT_CHECK_LABEL_SEVERE(NPT_ERROR_INVALID_SYNTAX, failure);
                }

                // validate value
                NPT_CHECK_LABEL_SEVERE(ValidateField(
                    entry->m_Value, 
                    PLT_DLNAHexCharsToValidate, 
                    32), failure);

                m_DLNA_FLAGS = entry->m_Value;
                state = PLT_PROTINFO_PARSER_STATE_FLAGS;
                continue;
            } else if (entry->m_Key == "DLNA.ORG_MAXSP") {
                // maxsp-param only allowed after flags-param
                if (state > PLT_PROTINFO_PARSER_STATE_FLAGS) { 
                    NPT_CHECK_LABEL_SEVERE(NPT_ERROR_INVALID_SYNTAX, failure);
                }

                // validate value
                NPT_CHECK_LABEL_SEVERE(ValidateField(
                    entry->m_Value, 
                    PLT_FIELD_NUM "."), failure);

                m_DLNA_MAXSP = entry->m_Value;
                state = PLT_PROTINFO_PARSER_STATE_MAXSP;
                continue;
            } else {
                // don't switch state for unknown value so we don't break parsing next ones
                // Sony TVs for example have DLNA.ORG_PN=xx;SONY.COM_PN=xx;DLNA.ORG_FLAGS=xxx
                //state = PLT_PROTINFO_PARSER_STATE_OTHER;

                // validate key first which should IANA_*<"a"-"z","A"-"Z","0"-"9">
                int index = entry->m_Key.Find("_");
                if (index == -1) {
                    NPT_CHECK_LABEL_SEVERE(NPT_ERROR_INVALID_SYNTAX, failure);
                }

                // validate key
                if (NPT_FAILED(ValidateField(
                    entry->m_Key.GetChars()+index, 
                    PLT_DLNAOTherNameCharsToValidate))) {
                        NPT_LOG_WARNING_2("Invalid protocolinfo 4th field other param: %s=%s",
                            (const char*)entry->m_Key, 
                            (const char*)entry->m_Value);
                    continue;
                }

                // validate value
                if (NPT_FAILED(ValidateField(
                        entry->m_Value, 
                        PLT_DLNAOTherValueCharsToValidate))) {
                
                    NPT_LOG_WARNING_2("Invalid protocolinfo 4th field other param: %s=%s",
                        (const char*)entry->m_Key, 
                        (const char*)entry->m_Value);
                    continue;
                }
                
                m_DLNA_OTHER.Add(*entry);
                continue;
            }
        }
    }

    m_Valid = true;
    return NPT_SUCCESS;
    
failure:
    NPT_LOG_WARNING_1("Failure to parse Protocol Info Extras:%s", m_Extra.GetChars());
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::ToString
+---------------------------------------------------------------------*/
NPT_String
PLT_ProtocolInfo::ToString() const
{
    NPT_String output = m_Protocol + ":";
    output += m_Mask + ":";
    output += m_ContentType + ":";
	// if it wasn't valid or extra is not DLNA, just use it as is
    if (!m_Valid || m_Extra == "*") {
        output += m_Extra;
    } else {
        bool add_semicolon = false;
		if (!m_DLNA_PN.IsEmpty()) {
			output += "DLNA.ORG_PN=" + m_DLNA_PN;
            add_semicolon = true;
		}
        if (!m_DLNA_OP.IsEmpty()) {
            if (add_semicolon) output += ";";
            output += "DLNA.ORG_OP=" + m_DLNA_OP;
            add_semicolon = true;
        }
        if (!m_DLNA_PS.IsEmpty()) {
            if (add_semicolon) output += ";";
            output += "DLNA.ORG_PS=" + m_DLNA_PS;
            add_semicolon = true;
        }
        if (!m_DLNA_CI.IsEmpty()) {
            if (add_semicolon) output += ";";
            output += "DLNA.ORG_CI=" + m_DLNA_CI;
            add_semicolon = true;
        }
        if (!m_DLNA_FLAGS.IsEmpty()) {
            if (add_semicolon) output += ";";
            output += "DLNA.ORG_FLAGS=" + m_DLNA_FLAGS;
            add_semicolon = true;
        }
        if (!m_DLNA_MAXSP.IsEmpty()) {
            if (add_semicolon) output += ";";
            output += "DLNA.ORG_MAXSP=" + m_DLNA_MAXSP;
            add_semicolon = true;
        }
        if (m_DLNA_OTHER.GetItemCount()) {
            for (NPT_List<FieldEntry>::Iterator iter = m_DLNA_OTHER.GetFirstItem();
                 iter;
                 iter++) {
                if (add_semicolon) output += ";";
                output += iter->m_Key + "=" + iter->m_Value;
                add_semicolon = true;
            }
        }
    }

    return output;
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::Match
+---------------------------------------------------------------------*/
bool
PLT_ProtocolInfo::Match(const PLT_ProtocolInfo& other) const
{
    // we need the first 3 params
    if (m_Protocol != '*' &&
        other.GetProtocol() != '*' &&
        m_Protocol != other.GetProtocol()) return false;
        
    if (m_Mask != '*' &&
        other.GetMask() != '*' &&
        m_Mask != other.GetMask()) return false;

    if (m_ContentType != '*' &&
        other.GetContentType() != '*' &&
        m_ContentType != other.GetContentType()) return false;

    // match DLNAPn of 4th item if not '*'
    if (m_Extra == '*' ||
        other.GetExtra() == '*' ||
        (!m_DLNA_PN.IsEmpty() && m_DLNA_PN == other.GetDLNA_PN())) return true;

    return false;
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::GetMimeTypeFromProtocolInfo
+---------------------------------------------------------------------*/
NPT_String  
PLT_ProtocolInfo::GetMimeTypeFromProtocolInfo(const char* protocol_info)
{
	/*NPT_String info = protocol_info;
	NPT_List<NPT_String> fragments = info.Split(":");
	if (fragments.GetItemCount() != 4) return "";
	return *fragments.GetItem(2);*/

    PLT_ProtocolInfo info(protocol_info);
    return info.m_ContentType;
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::GetDlnaExtension
+---------------------------------------------------------------------*/
const char* 
PLT_ProtocolInfo::GetDlnaExtension(const char*                   mime_type,
                                   const PLT_HttpRequestContext* context /* = NULL */)
{
    return GetDlnaExtension(mime_type, 
                            context?PLT_HttpHelper::GetDeviceSignature(context->GetRequest()):PLT_DEVICE_UNKNOWN);
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::GetDlnaExtension
+---------------------------------------------------------------------*/
const char* 
PLT_ProtocolInfo::GetDlnaExtension(const char*         mime_type,
                                   PLT_DeviceSignature signature /* = PLT_DEVICE_UNKNOWN */)
{
    NPT_String _mime_type = mime_type;
    
    if (signature != PLT_DEVICE_UNKNOWN) {
        // look for special case for 360
        if (signature == PLT_DEVICE_XBOX || signature == PLT_DEVICE_WMP) {
			for (unsigned int i=0; i<NPT_ARRAY_SIZE(PLT_HttpFileRequestHandler_360DlnaMap); i++) {
                if (_mime_type.Compare(PLT_HttpFileRequestHandler_360DlnaMap[i].mime_type, true) == 0) {
                    return PLT_HttpFileRequestHandler_360DlnaMap[i].dlna_ext;
                }
            }
		} else if (signature == PLT_DEVICE_SONOS) {
			for (unsigned int i=0; i<NPT_ARRAY_SIZE(PLT_HttpFileRequestHandler_SonosDlnaMap); i++) {
                if (_mime_type.Compare(PLT_HttpFileRequestHandler_SonosDlnaMap[i].mime_type, true) == 0) {
                    return PLT_HttpFileRequestHandler_SonosDlnaMap[i].dlna_ext;
                }
            }
		} else if (signature == PLT_DEVICE_PS3) {
            for (unsigned int i=0; i<NPT_ARRAY_SIZE(PLT_HttpFileRequestHandler_PS3DlnaMap); i++) {
                if (_mime_type.Compare(PLT_HttpFileRequestHandler_PS3DlnaMap[i].mime_type, true) == 0) {
                    return PLT_HttpFileRequestHandler_PS3DlnaMap[i].dlna_ext;
                }
            }
            
            return "DLNA.ORG_OP=01"; // Should we try default dlna instead?
        }
    }

    for (unsigned int i=0; i<NPT_ARRAY_SIZE(PLT_HttpFileRequestHandler_DefaultDlnaMap); i++) {
        if (_mime_type.Compare(PLT_HttpFileRequestHandler_DefaultDlnaMap[i].mime_type, true) == 0) {
            return PLT_HttpFileRequestHandler_DefaultDlnaMap[i].dlna_ext;
        }
    }

    return "*";
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::GetProtocolInfoFromMimeType
+---------------------------------------------------------------------*/
PLT_ProtocolInfo
PLT_ProtocolInfo::GetProtocolInfoFromMimeType(const char*         mime_type, 
                                              bool                with_dlna_extension /* = true */,
                                              PLT_DeviceSignature signature /* = PLT_DEVICE_UNKNOWN */)
{
    return PLT_ProtocolInfo("http-get:*:"+NPT_String(mime_type)+":"+ \
                            (with_dlna_extension?GetDlnaExtension(mime_type, signature):"*"));
}


/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::GetProtocolInfoFromMimeType
+---------------------------------------------------------------------*/
PLT_ProtocolInfo
PLT_ProtocolInfo::GetProtocolInfoFromMimeType(const char*                   mime_type, 
                                              bool                          with_dlna_extension /* = true */,
                                              const PLT_HttpRequestContext* context /* = NULL */)
{
    return GetProtocolInfoFromMimeType(mime_type, 
                                       with_dlna_extension, 
                                       context?PLT_HttpHelper::GetDeviceSignature(context->GetRequest()):PLT_DEVICE_UNKNOWN);
}

/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::GetProtocolInfo
+---------------------------------------------------------------------*/
PLT_ProtocolInfo
PLT_ProtocolInfo::GetProtocolInfo(const char*         filename, 
                                  bool                with_dlna_extension /* = true */,
                                  PLT_DeviceSignature signature /* = PLT_DEVICE_UNKNOWN */)
{
    return GetProtocolInfoFromMimeType(PLT_MimeType::GetMimeType(filename, signature), 
                                       with_dlna_extension, 
                                       signature);
}
/*----------------------------------------------------------------------
|   PLT_ProtocolInfo::GetProtocolInfo
+---------------------------------------------------------------------*/
PLT_ProtocolInfo
PLT_ProtocolInfo::GetProtocolInfo(const char*                   filename, 
                                  bool                          with_dlna_extension /* = true */,
								  const PLT_HttpRequestContext* context /* = NULL */)
{
    return GetProtocolInfoFromMimeType(PLT_MimeType::GetMimeType(filename, context), 
                                       with_dlna_extension, 
                                       context);
}
