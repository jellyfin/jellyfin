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
#include "PltMediaItem.h"
#include "PltMediaServer.h"
#include "PltDidl.h"
#include "PltUtilities.h"
#include "PltService.h"
#include "PltMimeType.h"

NPT_SET_LOCAL_LOGGER("platinum.media.server.item")

/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(PLT_MediaObject)
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(PLT_MediaItem)
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(PLT_MediaContainer)

/*----------------------------------------------------------------------
|   PLT_PersonRoles::AddPerson
+---------------------------------------------------------------------*/
NPT_Result
PLT_PersonRoles::Add(const NPT_String& name, const NPT_String& role /* = "" */)
{
    PLT_PersonRole person;
    person.name = name;
    person.role = role;

    return NPT_List<PLT_PersonRole>::Add(person);
}

/*----------------------------------------------------------------------
|   PLT_PersonRoles::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_PersonRoles::ToDidl(NPT_String& didl, const NPT_String& tag)
{
    NPT_String tmp;
    for (NPT_List<PLT_PersonRole>::Iterator it = 
         NPT_List<PLT_PersonRole>::GetFirstItem(); it; it++) {
        // if there's an empty artist, allow it only if there's nothing else
        if (it->name.IsEmpty() && m_ItemCount>1 && !tmp.IsEmpty()) continue;

        tmp += "<upnp:" + tag;
        if (!it->role.IsEmpty()) {
            tmp += " role=\"";
            PLT_Didl::AppendXmlEscape(tmp, it->role);
            tmp += "\"";
        }
        tmp += ">";
        PLT_Didl::AppendXmlEscape(tmp, it->name);
        tmp += "</upnp:" + tag + ">";
    }

    didl += tmp;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_PersonRoles::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_PersonRoles::FromDidl(const NPT_Array<NPT_XmlElementNode*>& nodes)
{
    for (NPT_Cardinal i=0; i<nodes.GetItemCount(); i++) {
        PLT_PersonRole person;
        const NPT_String* name = nodes[i]->GetText();
        const NPT_String* role = nodes[i]->GetAttribute("role");
        // DLNA 7.3.17
        if (name) person.name = name->SubString(0, 1024);
        if (role) person.role = role->SubString(0, 1024);
        NPT_CHECK(NPT_List<PLT_PersonRole>::Add(person));
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaItemResource::PLT_MediaItemResource
+---------------------------------------------------------------------*/
PLT_MediaItemResource::PLT_MediaItemResource()
{
    m_Uri             = "";
    m_ProtocolInfo    = PLT_ProtocolInfo();
    m_Duration        = (NPT_UInt32)-1;
    m_Size            = (NPT_LargeSize)-1;
    m_Protection      = "";
    m_Bitrate         = (NPT_UInt32)-1;
    m_BitsPerSample   = (NPT_UInt32)-1;
    m_SampleFrequency = (NPT_UInt32)-1;
    m_NbAudioChannels = (NPT_UInt32)-1;
    m_Resolution      = "";
    m_ColorDepth      = (NPT_UInt32)-1;
}

/*----------------------------------------------------------------------
|   PLT_MediaObject::GetUPnPClass
+---------------------------------------------------------------------*/
const char*
PLT_MediaObject::GetUPnPClass(const char*                   filename, 
                              const PLT_HttpRequestContext* context /* = NULL */)
{
    NPT_COMPILER_UNUSED(context);

    const char* ret = NULL;
    NPT_String mime_type = PLT_MimeType::GetMimeType(filename, context);

    if (mime_type.StartsWith("audio")) {
        ret = "object.item.audioItem.musicTrack";
    } else if (mime_type.StartsWith("video")) {
        ret = "object.item.videoItem"; //Note: 360 wants "object.item.videoItem" and not "object.item.videoItem.Movie"
    } else if (mime_type.StartsWith("image")) {
        ret = "object.item.imageItem.photo";
    } else {
        ret = "object.item";
    }

    return ret;
}

/*----------------------------------------------------------------------
|   PLT_MediaObject::Reset
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaObject::Reset() 
{
    m_ObjectClass.type = "";
    m_ObjectClass.friendly_name = "";
    m_ObjectID   = "";
    m_ParentID   = "";

    m_Title      = "";
    m_Creator    = "";
    m_Date		 = "";
    m_Restricted = true;

    m_People.actors.Clear();
    m_People.artists.Clear();    
    m_People.authors.Clear();

    m_Affiliation.album     = "";
    m_Affiliation.genres.Clear();
    m_Affiliation.playlist  = "";

    m_Description.description	       = "";
    m_Description.long_description     = "";
    m_Description.icon_uri             = "";
    m_ExtraInfo.album_arts.Clear();
    m_ExtraInfo.artist_discography_uri = "";

    m_MiscInfo.original_track_number = 0;
    m_MiscInfo.dvdregioncode		 = 0;
    m_MiscInfo.toc					 = "";
    m_MiscInfo.user_annotation		 = "";

    m_Recorded.program_title  = "";
    m_Recorded.series_title   = "";
    m_Recorded.episode_number = 0;

    m_Resources.Clear();

    m_Didl = "";

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaObject::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaObject::ToDidl(const NPT_String& filter, NPT_String& didl)
{
    return ToDidl(PLT_Didl::ConvertFilterToMask(filter), didl);
}

/*----------------------------------------------------------------------
|   PLT_MediaObject::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaObject::ToDidl(NPT_UInt32 mask, NPT_String& didl)
{
    // title is required
    didl += "<dc:title>";
    PLT_Didl::AppendXmlEscape(didl, m_Title);
    didl += "</dc:title>";

    // creator
    if (mask & PLT_FILTER_MASK_CREATOR) {
        didl += "<dc:creator>";
        if (m_Creator.IsEmpty()) m_Creator = "Unknown";
        PLT_Didl::AppendXmlEscape(didl, m_Creator);
        didl += "</dc:creator>";
    }

    // date
    if ((mask & PLT_FILTER_MASK_DATE) && !m_Date.IsEmpty()) {
        didl += "<dc:date>";
        PLT_Didl::AppendXmlEscape(didl, m_Date);
        didl += "</dc:date>";
    } 

    // artist
    if (mask & PLT_FILTER_MASK_ARTIST) {
        // force an empty artist just in case (not DLNA Compliant though)
        //if (m_People.artists.GetItemCount() == 0) m_People.artists.Add("");
        m_People.artists.ToDidl(didl, "artist");
    }

    // actor
    if (mask & PLT_FILTER_MASK_ACTOR) {
        m_People.actors.ToDidl(didl, "actor");
    }

    // actor
    if (mask & PLT_FILTER_MASK_AUTHOR) {
        m_People.authors.ToDidl(didl, "author");
    }
    
    // album
    if ((mask & PLT_FILTER_MASK_ALBUM) && !m_Affiliation.album.IsEmpty()) {
        didl += "<upnp:album>";
        PLT_Didl::AppendXmlEscape(didl, m_Affiliation.album);
        didl += "</upnp:album>";
    }

    // genre
    if (mask & PLT_FILTER_MASK_GENRE) {
        // Add unknown genre
        if (m_Affiliation.genres.GetItemCount() == 0) 
            m_Affiliation.genres.Add("Unknown");

        for (NPT_List<NPT_String>::Iterator it = 
             m_Affiliation.genres.GetFirstItem(); it; ++it) {
            didl += "<upnp:genre>";
            PLT_Didl::AppendXmlEscape(didl, (*it));
            didl += "</upnp:genre>";        
        }
    }

    // album art URI
    if ((mask & PLT_FILTER_MASK_ALBUMARTURI) && m_ExtraInfo.album_arts.GetItemCount()) {
        for (NPT_List<PLT_AlbumArtInfo>::Iterator iter = m_ExtraInfo.album_arts.GetFirstItem();
             iter;
             iter++) {
            didl += "<upnp:albumArtURI";
            if (!(*iter).dlna_profile.IsEmpty()) {
                didl += " dlna:profileID=\"";
                PLT_Didl::AppendXmlEscape(didl, (*iter).dlna_profile);
                didl += "\"";
            }
            didl += ">";
            PLT_Didl::AppendXmlEscape(didl, (*iter).uri);
            didl += "</upnp:albumArtURI>";
        }
    }
    
    // description
    if ((mask & PLT_FILTER_MASK_DESCRIPTION) && !m_Description.description.IsEmpty()) {
        didl += "<dc:description>";
        PLT_Didl::AppendXmlEscape(didl, m_Description.description);
        didl += "</dc:description>";
    }

    // long description
    if ((mask & PLT_FILTER_MASK_LONGDESCRIPTION) && !m_Description.long_description.IsEmpty()) {
        didl += "<upnp:longDescription>";
        PLT_Didl::AppendXmlEscape(didl, m_Description.long_description);
        didl += "</upnp:longDescription>";
    }
    
    // icon
    if ((mask & PLT_FILTER_MASK_ICON) && !m_Description.icon_uri.IsEmpty()) {
        didl += "<upnp:icon>";
        PLT_Didl::AppendXmlEscape(didl, m_Description.icon_uri);
        didl += "</upnp:icon>";
    }

    // original track number
    if ((mask & PLT_FILTER_MASK_ORIGINALTRACK) && m_MiscInfo.original_track_number > 0) {
        didl += "<upnp:originalTrackNumber>";
        didl += NPT_String::FromInteger(m_MiscInfo.original_track_number);
        didl += "</upnp:originalTrackNumber>";
    }

    // program title
    if (mask & PLT_FILTER_MASK_PROGRAMTITLE && !m_Recorded.program_title.IsEmpty()) {
        didl += "<upnp:programTitle>";
        PLT_Didl::AppendXmlEscape(didl, m_Recorded.program_title);
        didl += "</upnp:programTitle>";
    }

    // series title
    if ((mask & PLT_FILTER_MASK_SERIESTITLE) && !m_Recorded.series_title.IsEmpty()) {
        didl += "<upnp:seriesTitle>";
        PLT_Didl::AppendXmlEscape(didl, m_Recorded.series_title);
        didl += "</upnp:seriesTitle>";
    }

    // episode number
    if ((mask & PLT_FILTER_MASK_EPISODE) && m_Recorded.episode_number > 0) {
        didl += "<upnp:episodeNumber>";
        didl += NPT_String::FromInteger(m_Recorded.episode_number);
        didl += "</upnp:episodeNumber>";
    }

	if ((mask & PLT_FILTER_MASK_TOC) & !m_MiscInfo.toc.IsEmpty()) {
        didl += "<upnp:toc>";
		PLT_Didl::AppendXmlEscape(didl, m_MiscInfo.toc);
        didl += "</upnp:toc>";
    }

    // resource
    if (mask & PLT_FILTER_MASK_RES) {
        for (NPT_Cardinal i=0; i<m_Resources.GetItemCount(); i++) {
            didl += "<res";
            
            if ((mask & PLT_FILTER_MASK_RES_DURATION) && m_Resources[i].m_Duration != (NPT_UInt32)-1) {
                didl += " duration=\"";
                didl += PLT_Didl::FormatTimeStamp(m_Resources[i].m_Duration);
                didl += "\"";
            }

            if ((mask & PLT_FILTER_MASK_RES_SIZE) && m_Resources[i].m_Size != (NPT_LargeSize)-1) {
                didl += " size=\"";
                didl += NPT_String::FromIntegerU(m_Resources[i].m_Size);
                didl += "\"";
            }

            if ((mask & PLT_FILTER_MASK_RES_PROTECTION) && !m_Resources[i].m_Protection.IsEmpty()) {
                didl += " protection=\"";
                PLT_Didl::AppendXmlEscape(didl, m_Resources[i].m_Protection);
                didl += "\"";
            }
            
            if ((mask & PLT_FILTER_MASK_RES_RESOLUTION) && !m_Resources[i].m_Resolution.IsEmpty()) {
                didl += " resolution=\"";
                PLT_Didl::AppendXmlEscape(didl, m_Resources[i].m_Resolution);
                didl += "\"";
            }
            
            if ((mask & PLT_FILTER_MASK_RES_BITRATE) && m_Resources[i].m_Bitrate != (NPT_Size)-1) {                    
                didl += " bitrate=\"";
                didl += NPT_String::FromIntegerU(m_Resources[i].m_Bitrate);
                didl += "\"";
            }

			if ((mask & PLT_FILTER_MASK_RES_BITSPERSAMPLE) && m_Resources[i].m_BitsPerSample != (NPT_Size)-1) {                    
                didl += " bitsPerSample=\"";
                didl += NPT_String::FromIntegerU(m_Resources[i].m_BitsPerSample);
                didl += "\"";
            }

			if ((mask & PLT_FILTER_MASK_RES_SAMPLEFREQUENCY) && m_Resources[i].m_SampleFrequency != (NPT_Size)-1) {                    
                didl += " sampleFrequency=\"";
                didl += NPT_String::FromIntegerU(m_Resources[i].m_SampleFrequency);
                didl += "\"";
            }

			if ((mask & PLT_FILTER_MASK_RES_NRAUDIOCHANNELS) && m_Resources[i].m_NbAudioChannels != (NPT_Size)-1) {                    
                didl += " nrAudioChannels=\"";
                didl += NPT_String::FromIntegerU(m_Resources[i].m_NbAudioChannels);
                didl += "\"";
            }
            
            didl += " protocolInfo=\"";
            PLT_Didl::AppendXmlEscape(didl, m_Resources[i].m_ProtocolInfo.ToString());
            didl += "\">";
            PLT_Didl::AppendXmlEscape(didl, m_Resources[i].m_Uri);
            didl += "</res>";
        }
    }

    // class is required
    didl += "<upnp:class";
	if (!m_ObjectClass.friendly_name.IsEmpty()) {
		didl += " name=\"" + m_ObjectClass.friendly_name+"\"";
	}
	didl += ">";
    PLT_Didl::AppendXmlEscape(didl, m_ObjectClass.type);
    didl += "</upnp:class>";

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaObject::FromDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaObject::FromDidl(NPT_XmlElementNode* entry)
{
    NPT_String str, xml;
    NPT_Array<NPT_XmlElementNode*> children;
    NPT_Result res;

    // check if item is restricted (is default true?)
    if (NPT_SUCCEEDED(PLT_XmlHelper::GetAttribute(entry, "restricted", str, "", 5))) {
        m_Restricted = PLT_Service::IsTrue(str);
    }
    
    // read non-required elements
    PLT_XmlHelper::GetChildText(entry, "creator", m_Creator, didl_namespace_dc, 256);
    PLT_XmlHelper::GetChildText(entry, "date", m_Date, didl_namespace_dc, 256);
    
    // parse date and make sure it's valid
    NPT_String parsed_date;
    for (int format=0; format<=NPT_DateTime::FORMAT_RFC_1036; format++) {
        NPT_DateTime date;
        if (NPT_SUCCEEDED(date.FromString(m_Date, (NPT_DateTime::Format)format))) {
            parsed_date = date.ToString((NPT_DateTime::Format)format);
            break;
        }
    }
    m_Date = parsed_date;

    res = PLT_XmlHelper::GetAttribute(entry, "id", m_ObjectID);
    NPT_CHECK_SEVERE(res);

    res = PLT_XmlHelper::GetAttribute(entry, "parentID", m_ParentID);
    NPT_CHECK_SEVERE(res);

    PLT_XmlHelper::GetAttribute(entry, "refID", m_ReferenceID);

    res = PLT_XmlHelper::GetChildText(entry, "title", m_Title, didl_namespace_dc);
    NPT_CHECK_SEVERE(res);
    
    res = PLT_XmlHelper::GetChildText(entry, "class", m_ObjectClass.type, didl_namespace_upnp);
    NPT_CHECK_SEVERE(res);
    
    // DLNA 7.3.17.3 max bytes for dc:title and upnp:class is 256 bytes
    m_Title = m_Title.SubString(0, 256);    
    m_ObjectClass.type =  m_ObjectClass.type.SubString(0, 256);

    PLT_XmlHelper::GetChildren(entry, children, "artist", didl_namespace_upnp);
    m_People.artists.FromDidl(children);
    
    PLT_XmlHelper::GetChildren(entry, children, "author", didl_namespace_upnp);
    m_People.authors.FromDidl(children);
    
    PLT_XmlHelper::GetChildren(entry, children, "actors", didl_namespace_upnp);
    m_People.actors.FromDidl(children);

    PLT_XmlHelper::GetChildText(entry, "album", m_Affiliation.album, didl_namespace_upnp, 256);
    PLT_XmlHelper::GetChildText(entry, "programTitle", m_Recorded.program_title, didl_namespace_upnp);
    PLT_XmlHelper::GetChildText(entry, "seriesTitle", m_Recorded.series_title, didl_namespace_upnp);
    PLT_XmlHelper::GetChildText(entry, "episodeNumber", str, didl_namespace_upnp);
    NPT_UInt32 value;
    if (NPT_FAILED(str.ToInteger(value))) value = 0;
    m_Recorded.episode_number = value;

    children.Clear();
    PLT_XmlHelper::GetChildren(entry, children, "genre", didl_namespace_upnp);
    for (NPT_Cardinal i=0; i<children.GetItemCount(); i++) {
        if (children[i]->GetText()) {
            m_Affiliation.genres.Add(children[i]->GetText()->SubString(0, 256));
        }
    }
    
    PLT_XmlHelper::GetChildText(entry, "description", m_Description.description, didl_namespace_dc);
    PLT_XmlHelper::GetChildText(entry, "longDescription", m_Description.long_description, didl_namespace_upnp);
    PLT_XmlHelper::GetChildText(entry, "icon", m_Description.icon_uri, didl_namespace_upnp);
	PLT_XmlHelper::GetChildText(entry, "toc", m_MiscInfo.toc, didl_namespace_upnp);
    
    // album arts
    children.Clear();
    PLT_XmlHelper::GetChildren(entry, children, "albumArtURI", didl_namespace_upnp);
    for (NPT_Cardinal i=0; i<children.GetItemCount(); i++) {
        if (children[i]->GetText()) {
            PLT_AlbumArtInfo info;
            info.uri = children[i]->GetText()->SubString(0, 1024);
            PLT_XmlHelper::GetAttribute(children[i], "profileID", info.dlna_profile, didl_namespace_dlna);
            m_ExtraInfo.album_arts.Add(info);
        }
    }
    
    PLT_XmlHelper::GetChildText(entry, "originalTrackNumber", str, didl_namespace_upnp);
    if (NPT_FAILED(str.ToInteger(value))) value = 0;
    m_MiscInfo.original_track_number = value;

    children.Clear();
    PLT_XmlHelper::GetChildren(entry, children, "res");
    for (NPT_Cardinal i=0; i<children.GetItemCount(); i++) {
        PLT_MediaItemResource resource;

        // extract url
        if (children[i]->GetText() == NULL) {
            NPT_LOG_WARNING_1("No resource text found in: %s", (const char*)PLT_XmlHelper::Serialize(*children[i]));
        } else {
            resource.m_Uri = children[i]->GetText()->SubString(0, 1024);

            // basic uri validation, ignoring scheme (could be rtsp)
            NPT_HttpUrl url(resource.m_Uri, true);
            if (!url.IsValid()) {
                NPT_LOG_WARNING_1("Invalid resource uri: %s", (const char*)resource.m_Uri);
                continue;
            }
        }

        // extract protocol info
        NPT_String protocol_info;
        res = PLT_XmlHelper::GetAttribute(children[i], "protocolInfo", protocol_info, "", 256);
        if (NPT_FAILED(res)) {
            NPT_LOG_WARNING_1("No protocol info found in: %s", (const char*)PLT_XmlHelper::Serialize(*children[i]));
        } else {
            resource.m_ProtocolInfo = PLT_ProtocolInfo(protocol_info);
            if (!resource.m_ProtocolInfo.IsValid()) {
                NPT_LOG_WARNING_1("Invalid resource protocol info: %s", (const char*)protocol_info);
            }
        }

        // extract known attributes
        PLT_XmlHelper::GetAttribute(children[i], "protection", resource.m_Protection, "", 256);
        PLT_XmlHelper::GetAttribute(children[i], "resolution", resource.m_Resolution, "", 256);

        if (NPT_SUCCEEDED(PLT_XmlHelper::GetAttribute(children[i], "size", str, "", 256))) {
            if (NPT_FAILED(str.ToInteger64(resource.m_Size))) resource.m_Size = (NPT_Size)-1;
        }

        if (NPT_SUCCEEDED(PLT_XmlHelper::GetAttribute(children[i], "duration", str, "", 256))) {
            if (NPT_FAILED(PLT_Didl::ParseTimeStamp(str, resource.m_Duration))) {
                // if error while converting, ignore and set to -1 to indicate we don't know the duration
                resource.m_Duration = (NPT_UInt32)-1;
                PLT_XmlHelper::RemoveAttribute(children[i], "duration");
            } else {
                // DLNA: reformat duration in case it was not compliant
                str = PLT_Didl::FormatTimeStamp(resource.m_Duration);
                PLT_XmlHelper::SetAttribute(children[i], "duration", str); 
            }
        }    
        m_Resources.Add(resource);
    }

    // re serialize the entry didl as a we might need to pass it to a renderer
    // we may have modified the tree to "fix" issues, so as not to break a renderer
    // (don't write xml prefix as this didl could be part of a larger document)
    //res = PLT_XmlHelper::Serialize(*entry, xml, false);
    m_Didl = "";
    res = ToDidl(PLT_FILTER_MASK_ALL, m_Didl);
    NPT_CHECK_SEVERE(res);
    
    m_Didl = didl_header + m_Didl + didl_footer;    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaObjectList::PLT_MediaObjectList
+---------------------------------------------------------------------*/
PLT_MediaObjectList::PLT_MediaObjectList()
{
}

/*----------------------------------------------------------------------
|   PLT_MediaObjectList::~PLT_MediaObjectList
+---------------------------------------------------------------------*/
PLT_MediaObjectList::~PLT_MediaObjectList()
{
    Apply(NPT_ObjectDeleter<PLT_MediaObject>());
}

/*----------------------------------------------------------------------
|   PLT_MediaItem::PLT_MediaItem
+---------------------------------------------------------------------*/
PLT_MediaItem::PLT_MediaItem()
{
    Reset();
}

/*----------------------------------------------------------------------
|   PLT_MediaItem::~PLT_MediaItem
+---------------------------------------------------------------------*/
PLT_MediaItem::~PLT_MediaItem()
{
}

/*----------------------------------------------------------------------
|   PLT_MediaItem::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaItem::ToDidl(const NPT_String& filter, NPT_String& didl)
{
    return PLT_MediaObject::ToDidl(filter, didl);
}

/*----------------------------------------------------------------------
|   PLT_MediaItem::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaItem::ToDidl(NPT_UInt32 mask, NPT_String& didl)
{
    didl += "<item id=\"";

    PLT_Didl::AppendXmlEscape(didl, m_ObjectID);
    didl += "\" parentID=\"";
    PLT_Didl::AppendXmlEscape(didl, m_ParentID);

    if ((mask & PLT_FILTER_MASK_REFID) && !m_ReferenceID.IsEmpty()) {
		didl += "\" refID=\"";
		PLT_Didl::AppendXmlEscape(didl, m_ReferenceID);
    }

    didl += "\" restricted=\"";
    didl += m_Restricted?"1\"":"0\"";

    didl += ">";

    NPT_CHECK_SEVERE(PLT_MediaObject::ToDidl(mask, didl));

    /* close tag */
    didl += "</item>";

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaItem::FromDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaItem::FromDidl(NPT_XmlElementNode* entry)
{
    /* reset first */
    Reset();

    if (entry->GetTag().Compare("item", true) != 0) {
        NPT_CHECK_SEVERE(NPT_ERROR_INTERNAL);
    }

    NPT_Result result = PLT_MediaObject::FromDidl(entry);
    
    // make sure we have at least one valid resource
    if (m_Resources.GetItemCount() == 0) {
        NPT_CHECK_SEVERE(NPT_ERROR_INVALID_PARAMETERS);
    }

    return result;
}

/*----------------------------------------------------------------------
|   PLT_MediaContainer::PLT_MediaContainer
+---------------------------------------------------------------------*/
PLT_MediaContainer::PLT_MediaContainer()
{
    Reset();
}

/*----------------------------------------------------------------------
|   PLT_MediaContainer::~PLT_MediaContainer
+---------------------------------------------------------------------*/
PLT_MediaContainer::~PLT_MediaContainer(void)
{
}

/*----------------------------------------------------------------------
|   PLT_MediaContainer::Reset
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaContainer::Reset() 
{
    m_SearchClasses.Clear();
    m_Searchable        = false;
    m_ChildrenCount     = -1;
    m_ContainerUpdateID = 0;

    return PLT_MediaObject::Reset();
}

/*----------------------------------------------------------------------
|   PLT_MediaContainer::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaContainer::ToDidl(const NPT_String& filter, NPT_String& didl)
{
    return PLT_MediaObject::ToDidl(filter, didl);
}

/*----------------------------------------------------------------------
|   PLT_MediaContainer::ToDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaContainer::ToDidl(NPT_UInt32 mask, NPT_String& didl)
{
	// container id property
    didl += "<container id=\"";
    PLT_Didl::AppendXmlEscape(didl, m_ObjectID);

	// parent id property
    didl += "\" parentID=\"";
    PLT_Didl::AppendXmlEscape(didl, m_ParentID);
	
	// ref id
    if ((mask & PLT_FILTER_MASK_REFID) && !m_ReferenceID.IsEmpty()) {
		didl += "\" refID=\"";
		PLT_Didl::AppendXmlEscape(didl, m_ReferenceID);
    }

	// restricted property
    didl += "\" restricted=\"";
    didl += m_Restricted?"1\"":"0\"";

	// searchable property
    if (mask & PLT_FILTER_MASK_SEARCHABLE) {
        didl += " searchable=\"";
        didl += m_Searchable?"1\"":"0\"";
    }
    
	// childcount property
    if ((mask & PLT_FILTER_MASK_CHILDCOUNT) && m_ChildrenCount != -1) {
        didl += " childCount=\"";
        didl += NPT_String::FromInteger(m_ChildrenCount);
        didl += "\"";
    }

    didl += ">";

	if ((mask & PLT_FILTER_MASK_SEARCHCLASS) && m_SearchClasses.GetItemCount()) {
		NPT_List<PLT_SearchClass>::Iterator search_class = m_SearchClasses.GetFirstItem();
		while (search_class) {
			didl += "<upnp:searchClass includeDerived=\"";
			didl += (*search_class).include_derived?"1\"":"0\"";

			// frienly name is any
			if (!(*search_class).friendly_name.IsEmpty()) {
				didl += " name=\"" + (*search_class).friendly_name + "\"";
			}
			didl += ">";
			didl += (*search_class).type;
			didl += "</upnp:searchClass>";

			++search_class;
		}
	}

    NPT_CHECK_SEVERE(PLT_MediaObject::ToDidl(mask, didl));

    /* close tag */
    didl += "</container>";
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaContainer::FromDidl
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaContainer::FromDidl(NPT_XmlElementNode* entry)
{
    NPT_String str;

    /* reset first */
    Reset();

    // check entry type
    if (entry->GetTag().Compare("Container", true) != 0) 
        return NPT_ERROR_INTERNAL;

    // check if item is searchable (is default true?)
    if (NPT_SUCCEEDED(PLT_XmlHelper::GetAttribute(entry, "searchable", str, "", 5))) {
        m_Searchable = PLT_Service::IsTrue(str);
    }

    // look for childCount
    if (NPT_SUCCEEDED(PLT_XmlHelper::GetAttribute(entry, "childCount", str, "", 256))) {
        NPT_UInt32 count;
        NPT_CHECK_SEVERE(str.ToInteger(count));
        m_ChildrenCount = count;
    }

	// upnp:searchClass child elements
    NPT_Array<NPT_XmlElementNode*> children;
	PLT_XmlHelper::GetChildren(entry, children, "upnp:searchClass");

    for (NPT_Cardinal i=0; i<children.GetItemCount(); i++) {
        PLT_SearchClass search_class;

        // extract url
        if (children[i]->GetText() == NULL) {
            NPT_LOG_WARNING_1("No searchClass text found in: %s", 
				(const char*)PLT_XmlHelper::Serialize(*children[i]));
			continue;
        }
		
        // DLNA 7.3.17.4
		search_class.type = children[i]->GetText()->SubString(0, 256);

		// extract optional attribute name
		PLT_XmlHelper::GetAttribute(children[i], "name", search_class.friendly_name);
		    
		// includeDerived property
		if (NPT_FAILED(PLT_XmlHelper::GetAttribute(children[i], "includeDerived", str))) {
            NPT_LOG_WARNING_1("No required attribute searchClass@includeDerived found in: %s", 
				(const char*)PLT_XmlHelper::Serialize(*children[i]));
			continue;
		}

		search_class.include_derived = PLT_Service::IsTrue(str);
		m_SearchClasses.Add(search_class);
	}

    return PLT_MediaObject::FromDidl(entry);
}
