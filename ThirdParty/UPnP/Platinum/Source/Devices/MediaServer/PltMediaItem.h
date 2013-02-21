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

/** @file
 UPnP AV Media Object reprensentation.
 */

#ifndef _PLT_MEDIA_ITEM_H_
#define _PLT_MEDIA_ITEM_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttp.h"
#include "PltProtocolInfo.h"

/*----------------------------------------------------------------------
|   typedefs
+---------------------------------------------------------------------*/
/**
 The PLT_ObjectClass struct is used to assign a type to a PLT_MediaObject.
 */
typedef struct { 
    NPT_String type;
    NPT_String friendly_name;
} PLT_ObjectClass;

typedef struct { 
    NPT_String type;
    NPT_String friendly_name;
    bool       include_derived;
} PLT_SearchClass;

typedef struct {
    NPT_String name;
    NPT_String role;
} PLT_PersonRole;

class PLT_PersonRoles  : public NPT_List<PLT_PersonRole>
{
public:
    NPT_Result Add(const NPT_String& name, const NPT_String& role = "");
    NPT_Result ToDidl(NPT_String& didl, const NPT_String& tag);
    NPT_Result FromDidl(const NPT_Array<NPT_XmlElementNode*>& nodes);
};

typedef struct {
    NPT_String allowed_use; // (CSV)
    NPT_String validity_start;
    NPT_String validity_end;
    NPT_String remaining_time;
    NPT_String usage_info;
    NPT_String rights_info_uri;
    NPT_String content_info_uri;
} PLT_Constraint;

typedef struct {
    PLT_PersonRoles artists;
    PLT_PersonRoles actors;
    PLT_PersonRoles authors;
    NPT_String      producer; //TODO: can be multiple
    NPT_String      director; //TODO: can be multiple
    NPT_String      publisher; //TODO: can be multiple
    NPT_String      contributor; // should match m_Creator (dc:creator) //TODO: can be multiple
} PLT_PeopleInfo;

typedef struct {
    NPT_List<NPT_String> genres;
    NPT_String album; //TODO: can be multiple
    NPT_String playlist; // dc:title of the playlist item the content belongs too //TODO: can be multiple
} PLT_AffiliationInfo;

typedef struct {
    NPT_String description;
    NPT_String long_description;
    NPT_String icon_uri;
    NPT_String region;
    NPT_String rating;
    NPT_String rights; //TODO: can be multiple
    NPT_String date;
    NPT_String language;
} PLT_Description;

typedef struct {
    NPT_String uri;
    NPT_String dlna_profile;
} PLT_AlbumArtInfo;

typedef struct {
    NPT_List<PLT_AlbumArtInfo> album_arts;
    NPT_String artist_discography_uri;
    NPT_String lyrics_uri;
    NPT_List<NPT_String> relations; // dc:relation
} PLT_ExtraInfo;

typedef struct {
    NPT_UInt32 dvdregioncode;
    NPT_UInt32 original_track_number;
    NPT_String toc;
    NPT_String user_annotation; //TODO: can be multiple
} PLT_MiscInfo;

typedef struct {
    NPT_UInt64 total;
    NPT_UInt64 used;
    NPT_UInt64 free;
    NPT_UInt64 max_partition;
    NPT_UInt64 medium;
} PLT_StorageInfo;

typedef struct {
    NPT_String program_title;
    NPT_String series_title;
    NPT_UInt32 episode_number;
} PLT_RecordedInfo;

/*----------------------------------------------------------------------
|   PLT_MediaItemResource
+---------------------------------------------------------------------*/
class PLT_MediaItemResource
{
public:
    PLT_MediaItemResource();
    ~PLT_MediaItemResource() {}

    NPT_String       m_Uri;
    PLT_ProtocolInfo m_ProtocolInfo;
    NPT_UInt32       m_Duration; /* seconds */
    NPT_LargeSize    m_Size;
    NPT_String       m_Protection;
    NPT_UInt32       m_Bitrate; /* bytes/seconds */
    NPT_UInt32       m_BitsPerSample;
    NPT_UInt32       m_SampleFrequency;
    NPT_UInt32       m_NbAudioChannels;
    NPT_String       m_Resolution;
    NPT_UInt32       m_ColorDepth;
};

/*----------------------------------------------------------------------
|   PLT_MediaObject
+---------------------------------------------------------------------*/
/**
 The PLT_MediaObject class is any data entity that can be returned by a
 ContentDirectory Service from a browsing or searching action. This is the
 base class from which PLT_MediaItem and PLT_MediaContainer derive.
 */
class PLT_MediaObject
{
protected:
    NPT_IMPLEMENT_DYNAMIC_CAST(PLT_MediaObject)

    PLT_MediaObject() {}

public:
    virtual ~PLT_MediaObject() {}

    bool IsContainer() { return m_ObjectClass.type.StartsWith("object.container"); }

    static const char* GetUPnPClass(const char* filename, 
                                    const PLT_HttpRequestContext* context = NULL);

    virtual NPT_Result Reset();
    virtual NPT_Result ToDidl(const NPT_String& filter, NPT_String& didl);
    virtual NPT_Result ToDidl(NPT_UInt32 mask, NPT_String& didl);
    virtual NPT_Result FromDidl(NPT_XmlElementNode* entry);

public:
    /* common properties */
    PLT_ObjectClass     m_ObjectClass;
    NPT_String          m_ObjectID;
    NPT_String          m_ParentID;
    NPT_String          m_ReferenceID;

    /* metadata */
    NPT_String          m_Title;
    NPT_String          m_Creator;
    NPT_String          m_Date;
    PLT_PeopleInfo      m_People;
    PLT_AffiliationInfo m_Affiliation;
    PLT_Description     m_Description;
    PLT_RecordedInfo    m_Recorded;

    /* properties */
    bool m_Restricted;

    /* extras */
    PLT_ExtraInfo m_ExtraInfo;

    /* miscellaneous info */
    PLT_MiscInfo m_MiscInfo;

    /* resources related */
    NPT_Array<PLT_MediaItemResource> m_Resources;

    /* original DIDL for Control Points to pass to a renderer when invoking SetAVTransportURI */
    NPT_String m_Didl;    
};

/*----------------------------------------------------------------------
|   PLT_MediaItem
+---------------------------------------------------------------------*/
/**
 The PLT_MediaItem class represents a first-level class derived directly from
 PLT_MediaObject. It most often represents a single piece of AV data. 
 */
class PLT_MediaItem : public PLT_MediaObject
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST_D(PLT_MediaItem, PLT_MediaObject)

    PLT_MediaItem();
    virtual ~PLT_MediaItem();

    // PLT_MediaObject methods
    NPT_Result ToDidl(const NPT_String& filter, NPT_String& didl);
    NPT_Result ToDidl(NPT_UInt32 mask, NPT_String& didl);
    NPT_Result FromDidl(NPT_XmlElementNode* entry);
};

/*----------------------------------------------------------------------
|   PLT_MediaContainer
+---------------------------------------------------------------------*/
/**
 The PLT_MediaContainer class represents a first-level class derived directly
 from PLT_MediaObject. A PLT_MediaContainer represents a collection of 
 PLT_MediaObject instances.
 */
class PLT_MediaContainer : public PLT_MediaObject
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST_D(PLT_MediaContainer, PLT_MediaObject)

    PLT_MediaContainer();
    virtual ~PLT_MediaContainer();

    // PLT_MediaObject methods
    NPT_Result Reset();
    NPT_Result ToDidl(const NPT_String& filter, NPT_String& didl);
    NPT_Result ToDidl(NPT_UInt32 mask, NPT_String& didl);
    NPT_Result FromDidl(NPT_XmlElementNode* entry);

public:
    NPT_List<PLT_SearchClass> m_SearchClasses;

    /* properties */
    bool m_Searchable;

    /* container info related */
    NPT_Int32  m_ChildrenCount;    
    NPT_UInt32 m_ContainerUpdateID;
};

/*----------------------------------------------------------------------
|   PLT_MediaObjectList
+---------------------------------------------------------------------*/
/**
 The PLT_MediaObjectList class is a list of PLT_MediaObject instances.
 */
class PLT_MediaObjectList : public NPT_List<PLT_MediaObject*>
{
public:
    PLT_MediaObjectList();

protected:
    virtual ~PLT_MediaObjectList(void);
    friend class NPT_Reference<PLT_MediaObjectList>;
};

typedef NPT_Reference<PLT_MediaObjectList> PLT_MediaObjectListReference;
typedef NPT_Reference<PLT_MediaObject> PLT_MediaObjectReference;

#endif /* _PLT_MEDIA_ITEM_H_ */
