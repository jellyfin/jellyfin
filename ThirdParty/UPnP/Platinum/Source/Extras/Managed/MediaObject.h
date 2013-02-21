/*****************************************************************
|
|   Platinum - Managed ActionDescription
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
#pragma once

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltMediaItem.h"
#include "Http.h"

namespace Platinum
{

/*----------------------------------------------------------------------
|   ObjectClass
+---------------------------------------------------------------------*/
public ref class ObjectClass : public ManagedWrapper<PLT_ObjectClass>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Type, type, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, FriendlyName, friendly_name, m_pHandle);

internal:

    ObjectClass(PLT_ObjectClass& native) :
        ManagedWrapper<PLT_ObjectClass>(native)
    {}

public:

    ObjectClass(String^ type)
    {
        Type = type;
	}

    ObjectClass(String^ type, String^ friendly_name)
    {
        Type = type;
        FriendlyName = friendly_name;
    }
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::ObjectClass, PLT_ObjectClass);

namespace Platinum
{

/*----------------------------------------------------------------------
|   PersonRole
+---------------------------------------------------------------------*/
public ref class PersonRole : public ManagedWrapper<PLT_PersonRole>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Name, name, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Role, role, m_pHandle);

internal:

    PersonRole(PLT_PersonRole& native) :
        ManagedWrapper<PLT_PersonRole>(native)
    {}

public:
	PersonRole(String^ name)
    {
        Name = name;
    }

	PersonRole(String^ name, String^ role)
    {
        Name = name;
		Role = role;
    }
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::PersonRole, PLT_PersonRole);

namespace Platinum
{

/*----------------------------------------------------------------------
|   PeopleInfo
+---------------------------------------------------------------------*/
public ref class PeopleInfo : public ManagedWrapper<PLT_PeopleInfo>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Producer, producer, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Director, director, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Publisher, publisher, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Contributor, contributor, m_pHandle);

    property IEnumerable<PersonRole^>^ Artists
    {
        IEnumerable<PersonRole^>^ get();
    }

    property IEnumerable<PersonRole^>^ Actors
    {
        IEnumerable<PersonRole^>^ get();
    }

    property IEnumerable<PersonRole^>^ Authors
    {
        IEnumerable<PersonRole^>^ get();
    }

public:

    void AddArtist(PersonRole^ artist)
    {
        ((NPT_List<PLT_PersonRole>&)m_pHandle->artists).Add(artist->Handle);
    }
    
    void AddActor(PersonRole^ actor)
    {
        ((NPT_List<PLT_PersonRole>&)m_pHandle->actors).Add(actor->Handle);
    }   
    
    void AddAuthors(PersonRole^ author)
    {
        ((NPT_List<PLT_PersonRole>&)m_pHandle->authors).Add(author->Handle);
    }

internal:

    PeopleInfo(PLT_PeopleInfo& native) :
        ManagedWrapper<PLT_PeopleInfo>(native)
    {}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::PeopleInfo, PLT_PeopleInfo);


namespace Platinum
{

/*----------------------------------------------------------------------
|   AffiliationInfo
+---------------------------------------------------------------------*/
public ref class AffiliationInfo : public ManagedWrapper<PLT_AffiliationInfo>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Album, album, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Playlist, playlist, m_pHandle);

    property IEnumerable<String^>^ Genres
    {
        IEnumerable<String^>^ get();
    }

public:

    void AddGenre(String^ genre)
    {
        m_pHandle->genres.Add(NPT_String(StringConv(genre)));
    }

internal:

    AffiliationInfo(PLT_AffiliationInfo& native) :
        ManagedWrapper<PLT_AffiliationInfo>(native)
    {}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::AffiliationInfo, PLT_AffiliationInfo);

namespace Platinum
{

/*----------------------------------------------------------------------
|   DescriptionInfo
+---------------------------------------------------------------------*/
public ref class DescriptionInfo : public ManagedWrapper<PLT_Description>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, DescriptionText, description, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, LongDescriptionText, long_description, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, IconUri, icon_uri, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Region, region, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Rating, rating, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Date, date, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Language, language, m_pHandle);

internal:

    DescriptionInfo(PLT_Description& native) :
        ManagedWrapper<PLT_Description>(native)
    {}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::DescriptionInfo, PLT_Description);

namespace Platinum
{

/*----------------------------------------------------------------------
|   RecordedInfo
+---------------------------------------------------------------------*/
public ref class RecordedInfo : public ManagedWrapper<PLT_RecordedInfo>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, ProgramTitle, program_title, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, SeriesTitle, series_title, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, EpisodeNumber, episode_number, m_pHandle);

internal:

    RecordedInfo(PLT_RecordedInfo& native) :
        ManagedWrapper<PLT_RecordedInfo>(native)
    {}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::RecordedInfo, PLT_RecordedInfo);

namespace Platinum
{

/*----------------------------------------------------------------------
|   AlbumArtInfo
+---------------------------------------------------------------------*/
public ref class AlbumArtInfo : public ManagedWrapper<PLT_AlbumArtInfo>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Uri, uri, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, DlnaProfile, dlna_profile, m_pHandle);

internal:

    AlbumArtInfo(PLT_AlbumArtInfo& native) :
        ManagedWrapper<PLT_AlbumArtInfo>(native)
    {}
        
public:
    AlbumArtInfo(String^ uri)
    {
        Uri = uri;
	}

    AlbumArtInfo(String^ uri, String^ dlna_profile)
    {
        Uri = uri;
        DlnaProfile = dlna_profile;
    }
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::AlbumArtInfo, PLT_AlbumArtInfo);

namespace Platinum
{

/*----------------------------------------------------------------------
|   ExtraInfo
+---------------------------------------------------------------------*/
public ref class ExtraInfo : public ManagedWrapper<PLT_ExtraInfo>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, ArtistDiscographyUri, artist_discography_uri, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, LyricsUri, lyrics_uri, m_pHandle);

    property IEnumerable<String^>^ Relations
    {
        IEnumerable<String^>^ get();
    }

    property IEnumerable<AlbumArtInfo^>^ AlbumArts
    {
        IEnumerable<AlbumArtInfo^>^ get();
    }

public:

    void AddGenre(String^ relation)
    {
        m_pHandle->relations.Add(NPT_String(StringConv(relation)));
    }

    void AddAlbumArtInfo(AlbumArtInfo^ info)
    {
        m_pHandle->album_arts.Add(info->Handle);
    }


internal:

    ExtraInfo(PLT_ExtraInfo& native) :
        ManagedWrapper<PLT_ExtraInfo>(native)
    {}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::ExtraInfo, PLT_ExtraInfo);

namespace Platinum
{

/*----------------------------------------------------------------------
|   MiscInfo
+---------------------------------------------------------------------*/
public ref class MiscInfo : public ManagedWrapper<PLT_MiscInfo>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, DvdRegionCode, dvdregioncode, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, OriginalTrackNumber, original_track_number, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Toc, toc, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, UserAnnotation, user_annotation, m_pHandle);

internal:

    MiscInfo(PLT_MiscInfo& native) :
        ManagedWrapper<PLT_MiscInfo>(native)
    {}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::MiscInfo, PLT_MiscInfo);

namespace Platinum
{

/*----------------------------------------------------------------------
|   ProtocolInfo
+---------------------------------------------------------------------*/
public ref class ProtocolInfo : public ManagedWrapper<PLT_ProtocolInfo>
{
public:

    // properties

public:
    static ProtocolInfo^ GetProtocolInfo(String^ filename) 
    { 
        return GetProtocolInfo(filename, true, nullptr); 
    }

    static ProtocolInfo^ GetProtocolInfo(String^ filename, 
                                         bool with_dlna_extension) 
    { 
        return GetProtocolInfo(filename, with_dlna_extension, nullptr); 
    }

    static ProtocolInfo^ GetProtocolInfo(String^ filename, 
                                         bool with_dlna_extension, 
                                         HttpRequestContext^ context);

    static ProtocolInfo^ GetProtocolInfoFromMimeType(String^ mime_type)
    {
        return GetProtocolInfoFromMimeType(mime_type, true, nullptr);
    }

    static ProtocolInfo^ GetProtocolInfoFromMimeType(String^ mime_type, 
                                                     bool with_dlna_extension)
    {
        return GetProtocolInfoFromMimeType(mime_type, with_dlna_extension, nullptr);
    }

    static ProtocolInfo^ GetProtocolInfoFromMimeType(String^ mime_type, 
                                                     bool with_dlna_extension, 
                                                     HttpRequestContext^ context);

internal:

    ProtocolInfo(PLT_ProtocolInfo& native) :
        ManagedWrapper<PLT_ProtocolInfo>(native)
    {}

public:

    ProtocolInfo(NPT_String& protocol_info) :
        ManagedWrapper<PLT_ProtocolInfo>()
    {
		*m_pHandle = PLT_ProtocolInfo(protocol_info);
	}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::ProtocolInfo, PLT_ProtocolInfo);
PLATINUM_MANAGED_MARSHAL_AS(Platinum::ProtocolInfo, NPT_String);

namespace Platinum
{

/*----------------------------------------------------------------------
|   MediaResource
+---------------------------------------------------------------------*/
public ref class MediaResource : public ManagedWrapper<PLT_MediaItemResource>
{
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, URI, m_Uri, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(ProtocolInfo^, ProtoInfo, m_ProtocolInfo, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, Duration, m_Duration, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt64, Size, m_Size, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Protection, m_Protection, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, Bitrate, m_Bitrate, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, BitsPerSample, m_BitsPerSample, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, SampleFrequency, m_SampleFrequency, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, NbAudioChannels, m_NbAudioChannels, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Resolution, m_Resolution, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, ColorDepth, m_ColorDepth, m_pHandle);

internal:

    MediaResource(PLT_MediaItemResource& native) :
        ManagedWrapper<PLT_MediaItemResource>(native)
    {}

public:
    MediaResource() :
        ManagedWrapper<PLT_MediaItemResource>()
    {}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::MediaResource, PLT_MediaItemResource);

namespace Platinum
{
ref class MediaItem;
ref class MediaContainer;

/*----------------------------------------------------------------------
|   MediaObject
+---------------------------------------------------------------------*/
public ref class MediaObject
{
protected:

	PLT_MediaObjectReference* m_pHandle;
   
public:

    // properties

    /* common properties */
    PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(ObjectClass^, Class, m_ObjectClass, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, ObjectID, m_ObjectID, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, ParentID, m_ParentID, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, ReferenceID, m_ReferenceID, (*m_pHandle));

    /* metadata */
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Title, m_Title, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Creator, m_Creator, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, Date, m_Date, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(PeopleInfo^, People, m_People, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(AffiliationInfo^, Affiliation, m_Affiliation, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(DescriptionInfo^, Description, m_Description, (*m_pHandle));
    PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(RecordedInfo^, Recorded, m_Recorded, (*m_pHandle));

    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(bool, Restricted, m_Restricted, (*m_pHandle));
    
    /* extras */
    PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(ExtraInfo^, Extra, m_ExtraInfo, (*m_pHandle));

    /* miscellaneous info */
    PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(MiscInfo^, Miscellaneous, m_MiscInfo, (*m_pHandle));

    property IEnumerable<MediaResource^>^ Resources
    {
        IEnumerable<MediaResource^>^ get();
    }

public:

    void AddResource(MediaResource^ resource)
    {
        (*m_pHandle)->m_Resources.Add(resource->Handle);
    }

internal:

    property PLT_MediaObjectReference& Handle
    {
        PLT_MediaObjectReference& get()
        {
            return *m_pHandle;
        }
    }

public:

    MediaItem^      item();
    MediaContainer^ container();

	String^ ToDidl(String^ filter)
    {
        NPT_String didl;
        didl.Reserve(1024);
        
        marshal_context c;
        NPT_String _filter(c.marshal_as<const char*>(filter));

        (*m_pHandle)->ToDidl(_filter, didl);

        return marshal_as<String^>(didl.GetChars());
    }

public:

	virtual Boolean Equals(Object^ obj) override
	{
		if (obj == nullptr)
			return false;

		if (!this->GetType()->IsInstanceOfType(obj))
			return false;

		return (*m_pHandle == *((MediaObject^)obj)->m_pHandle);
	}

internal:

	MediaObject(PLT_MediaObjectReference& media)
	{
		if (media.IsNull())
			throw gcnew ArgumentNullException("media");

		m_pHandle = new PLT_MediaObjectReference(media);
	}

	MediaObject(PLT_MediaObject& media)
	{
		m_pHandle = new PLT_MediaObjectReference(&media);
	}

protected:

    MediaObject(PLT_MediaObject* media)
    {
        if (media == NULL)
            throw gcnew ArgumentNullException("media");

        m_pHandle = new PLT_MediaObjectReference(media);
    }

public:

	~MediaObject()
	{
        // clean-up managed

        // clean-up unmanaged
		this->!MediaObject();
	}

	!MediaObject()
    {
        // clean-up unmanaged
		if (m_pHandle != 0)
		{
			delete m_pHandle;

			m_pHandle = 0;
		}
	}

};

/*----------------------------------------------------------------------
|   MediaItem
+---------------------------------------------------------------------*/
public ref class MediaItem : public MediaObject
{
internal:

    MediaItem(PLT_MediaItem& media) : MediaObject(media)
    {}

public:

    MediaItem() : MediaObject(new PLT_MediaItem)
    {}

    MediaItem(PLT_MediaObjectReference& media) : MediaObject(media)
    {
        if (media->IsContainer())
            throw gcnew ArgumentException("object");
    }

    MediaItem(MediaObject^ media) : MediaObject(*media->Handle)
    {
        if (media->Handle->IsContainer())
            throw gcnew ArgumentException("object");
    }

    ~MediaItem()
    {
        this->!MediaItem();
    }

    !MediaItem()
    {}
};

/*----------------------------------------------------------------------
|   MediaContainer
+---------------------------------------------------------------------*/
public ref class MediaContainer : public MediaObject
{
public:

    // properties
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(bool, Searchable, m_Searchable, ((PLT_MediaContainer*)m_pHandle->AsPointer()));
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(Int32, ChildrenCount, m_ChildrenCount, ((PLT_MediaContainer*)m_pHandle->AsPointer()));
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(UInt32, ContainerUpdateID, m_ContainerUpdateID, ((PLT_MediaContainer*)m_pHandle->AsPointer()));

internal:

    MediaContainer(PLT_MediaContainer& media) : 
        MediaObject(media)
    {}

public:
    MediaContainer() : 
        MediaObject(new PLT_MediaContainer)
    {}

    MediaContainer(PLT_MediaObjectReference& media) : 
        MediaObject(media)
    {
        if (!media->IsContainer())
            throw gcnew ArgumentException("object");
    }

    MediaContainer(MediaObject^ media) : 
        MediaObject(*media->Handle)
    {
        if (!media->Handle->IsContainer())
            throw gcnew ArgumentException("object");
    }

    ~MediaContainer()
    {
        this->!MediaContainer();
    }

    !MediaContainer()
    {}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::MediaObject, PLT_MediaObject);
PLATINUM_MANAGED_MARSHAL_AS(Platinum::MediaObject, PLT_MediaObjectReference);

PLATINUM_MANAGED_MARSHAL_AS(Platinum::MediaItem, PLT_MediaItem);
PLATINUM_MANAGED_MARSHAL_AS(Platinum::MediaContainer, PLT_MediaContainer);