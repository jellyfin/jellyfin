/*****************************************************************
|
|   Platinum - DIDL
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
#include "PltDidl.h"
#include "PltUtilities.h"
#include "PltService.h"

NPT_SET_LOCAL_LOGGER("platinum.media.server.didl")

/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
const char* didl_header         = "<DIDL-Lite xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\""
                                            " xmlns:dc=\"http://purl.org/dc/elements/1.1/\""
                                            " xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\""
                                            " xmlns:dlna=\"urn:schemas-dlna-org:metadata-1-0/\">";
const char* didl_footer         = "</DIDL-Lite>";
const char* didl_namespace_dc   = "http://purl.org/dc/elements/1.1/";
const char* didl_namespace_upnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";
const char* didl_namespace_dlna = "urn:schemas-dlna-org:metadata-1-0/";

/*----------------------------------------------------------------------
|   PLT_Didl::ConvertFilterToMask
+---------------------------------------------------------------------*/
NPT_UInt32 
PLT_Didl::ConvertFilterToMask(const NPT_String& filter)
{
    // easy out
    if (filter.GetLength() == 0) return PLT_FILTER_MASK_ALL;
    
    // a filter string is a comma delimited set of fields identifying
    // a given DIDL property (or set of properties).  
    // These fields are or start with: upnp:, @, res@, res, dc:, container@

    NPT_UInt32  mask = 0;
    const char* s = filter;
    int         i = 0;

    while (s[i] != '\0') {
        int next_comma = filter.Find(',', i);
        int len = ((next_comma < 0)?(int)filter.GetLength():next_comma)-i;

        if (NPT_String::CompareN(s+i, "*", 1) == 0) {
            // return now, there's no point in parsing the rest
            return PLT_FILTER_MASK_ALL;
        }
        
        // title is required, so we return a non empty mask
        mask |= PLT_FILTER_MASK_TITLE;

        if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_TITLE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_TITLE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_REFID, len, true) == 0) {
            mask |= PLT_FILTER_MASK_REFID;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_CREATOR, len, true) == 0) {
            mask |= PLT_FILTER_MASK_CREATOR;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_ARTIST, len, true) == 0) {
            mask |= PLT_FILTER_MASK_ARTIST;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_ACTOR, len, true) == 0) {
            mask |= PLT_FILTER_MASK_ACTOR;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_AUTHOR, len, true) == 0) {
            mask |= PLT_FILTER_MASK_AUTHOR;       
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_DATE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_DATE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_ALBUM, len, true) == 0) {
            mask |= PLT_FILTER_MASK_ALBUM;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_GENRE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_GENRE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_ALBUMARTURI, len, true) == 0 ||
                   NPT_String::CompareN(s+i, PLT_FILTER_FIELD_ALBUMARTURI_DLNAPROFILEID, len, true) == 0) {
            mask |= PLT_FILTER_MASK_ALBUMARTURI;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_DESCRIPTION, len, true) == 0) {
            mask |= PLT_FILTER_MASK_DESCRIPTION;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_ORIGINALTRACK, len, true) == 0) {
            mask |= PLT_FILTER_MASK_ORIGINALTRACK;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_SEARCHABLE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_SEARCHABLE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_SEARCHCLASS, len, true) == 0) {
            mask |= PLT_FILTER_MASK_SEARCHCLASS;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_CONTAINER_SEARCHABLE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_SEARCHABLE;       
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_CHILDCOUNT, len, true) == 0) {
            mask |= PLT_FILTER_MASK_CHILDCOUNT;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_CONTAINER_CHILDCOUNT, len, true) == 0) {
            mask |= PLT_FILTER_MASK_CHILDCOUNT;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_PROGRAMTITLE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_PROGRAMTITLE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_SERIESTITLE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_SERIESTITLE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_EPISODE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_EPISODE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_DURATION, len, true) == 0 ||
				   NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_DURATION_SHORT, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES | PLT_FILTER_MASK_RES_DURATION;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_SIZE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES | PLT_FILTER_MASK_RES_SIZE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_PROTECTION, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES | PLT_FILTER_MASK_RES_PROTECTION;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_RESOLUTION, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES | PLT_FILTER_MASK_RES_RESOLUTION;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_BITRATE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES | PLT_FILTER_MASK_RES_BITRATE;
        } else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_BITSPERSAMPLE, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES | PLT_FILTER_MASK_RES_BITSPERSAMPLE;
		} else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_NRAUDIOCHANNELS, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES | PLT_FILTER_MASK_RES_NRAUDIOCHANNELS;
		} else if (NPT_String::CompareN(s+i, PLT_FILTER_FIELD_RES_SAMPLEFREQUENCY, len, true) == 0) {
            mask |= PLT_FILTER_MASK_RES | PLT_FILTER_MASK_RES_SAMPLEFREQUENCY;
		}

        if (next_comma < 0) {
            return mask;
        }

        i = next_comma + 1;
    }

    return mask;
}

/*----------------------------------------------------------------------
|   PLT_Didl::AppendXmlUnEscape
+---------------------------------------------------------------------*/
void
PLT_Didl::AppendXmlUnEscape(NPT_String& out, const char* in)
{
    unsigned int i=0;
    while (i<NPT_StringLength(in)) {
        if (NPT_String::CompareN(in+i, "&lt;", 4) == 0) {
            out += '<';
            i   +=4;
        } else if (NPT_String::CompareN(in+i, "&gt;", 4) == 0) {
            out += '>';
            i   += 4;
        } else if (NPT_String::CompareN(in+i, "&amp;", 5) == 0) {
            out += '&';
            i   += 5;
        } else if (NPT_String::CompareN(in+i, "&quot;", 6) == 0) {
            out += '"';
            i   += 6;
        } else if (NPT_String::CompareN(in+i, "&apos;", 6) == 0) {
            out += '\'';
            i   += 6;
        } else {
            out += *(in+i);
            i++;
        }
    }
}

/*----------------------------------------------------------------------
|   PLT_Didl::AppendXmlEscape
+---------------------------------------------------------------------*/
void
PLT_Didl::AppendXmlEscape(NPT_String& out, const char* in)
{
    if (!in) return;

    for (int i=0; i<(int)NPT_StringLength(in); i++) {
        if (*(in+i) == '<') {
            out += "&lt;";
        } else if (*(in+i) == '>') {
            out += "&gt;";
        } else if (*(in+i) == '&') {
            out += "&amp;";
        } else if (*(in+i) == '"') {
            out += "&quot;";
        }  else if (*(in+i) == '\'') {
            out += "&apos;";
        } else {
            out += *(in+i);
        }
    }
}

/*----------------------------------------------------------------------
|   PLT_Didl::FormatTimeStamp
+---------------------------------------------------------------------*/
NPT_String
PLT_Didl::FormatTimeStamp(NPT_UInt32 seconds)
{
    NPT_String result;
    int hours = seconds/3600;
    if (hours == 0) {
        result += "0:";
    } else {
        result += NPT_String::FromInteger(hours) + ":";
    }

    int minutes = (seconds/60)%60;
    if (minutes == 0) {
        result += "00:";
    } else {
        if (minutes < 10) {
            result += '0';
        }
        result += NPT_String::FromInteger(minutes) + ":";
    }

    int secs = seconds%60;
    if (secs == 0) {
        result += "00";
    } else {
        if (secs < 10) {
            result += '0';
        }
        result += NPT_String::FromInteger(secs);
    }

	result += ".000"; // needed for XBOX360 otherwise it won't play the track
    return result;
}

/*----------------------------------------------------------------------
|   PLT_Didl::ParseTimeStamp
+---------------------------------------------------------------------*/
NPT_Result
PLT_Didl::ParseTimeStamp(const NPT_String& timestamp, NPT_UInt32& seconds)
{
    // assume a timestamp in the format HH:MM:SS.FFF
    int separator;
    NPT_String str = timestamp;
    NPT_UInt32 value;

    // reset output params first
    seconds = 0;
    
    // remove milliseconds first if any
    if ((separator = str.ReverseFind('.')) != -1) {
        str = str.Left(separator);
    }

    // look for next separator
    if ((separator = str.ReverseFind(':')) == -1) return NPT_FAILURE;
    
    // extract seconds
    NPT_CHECK_WARNING(str.SubString(separator+1).ToInteger(value));
    seconds = value;
    str = str.Left(separator);

    // look for next separator
    if ((separator = str.ReverseFind(':')) == -1) return NPT_FAILURE;
    
    // extract minutes
    NPT_CHECK_WARNING(str.SubString(separator+1).ToInteger(value));
    seconds += 60*value;
    str = str.Left(separator);
    
    // extract hours
    NPT_CHECK_WARNING(str.ToInteger(value));
    seconds += 3600*value;
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Didl::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_Didl::ToDidl(PLT_MediaObject& object, const NPT_String& filter, NPT_String& didl)
{
    NPT_UInt32 mask = ConvertFilterToMask(filter);

    // Allocate enough space for the didl
    didl.Reserve(2048);

    return object.ToDidl(mask, didl);
}

/*----------------------------------------------------------------------
|   PLT_Didl::FromDidl
+---------------------------------------------------------------------*/
NPT_Result  
PLT_Didl::FromDidl(const char* xml, PLT_MediaObjectListReference& objects)
{
    NPT_String          str;
    PLT_MediaObject*    object = NULL;
    NPT_XmlNode*        node = NULL;
    NPT_XmlElementNode* didl = NULL;
	NPT_XmlParser		parser;

    NPT_LOG_FINE("Parsing Didl...");

	NPT_CHECK_LABEL_SEVERE(parser.Parse(xml, node), cleanup);
    if (!node || !node->AsElementNode()) {
		NPT_LOG_SEVERE("Invalid node type");
        goto cleanup;
    }

    didl = node->AsElementNode();

	if (didl->GetTag().Compare("DIDL-Lite", true)) {
		NPT_LOG_SEVERE("Invalid node tag");
        goto cleanup;
    }

    // create entry list
    objects = new PLT_MediaObjectList();

    // for each child, find out if it's a container or not
    // and then invoke the FromDidl on it
    for (NPT_List<NPT_XmlNode*>::Iterator children = didl->GetChildren().GetFirstItem(); children; children++) {
        NPT_XmlElementNode* child = (*children)->AsElementNode();
        if (!child) continue;

        if (child->GetTag().Compare("Container", true) == 0) {
            object = new PLT_MediaContainer();
        } else if (child->GetTag().Compare("item", true) == 0) {
            object = new PLT_MediaItem();
		} else {
			NPT_LOG_WARNING("Invalid node tag");
            continue;
        }

        if (NPT_FAILED(object->FromDidl(child))) {
            NPT_LOG_WARNING_1("Invalid didl for object: %s", 
                (const char*) PLT_XmlHelper::Serialize(*child, false));
          	continue;
        }

        objects->Add(object);
        object = NULL; // reset to make sure it doesn't get deleted twice in case of error
    }

    delete node;
    return NPT_SUCCESS;

cleanup:
    objects = NULL;
    delete node;
    delete object;
    return NPT_FAILURE;
}
