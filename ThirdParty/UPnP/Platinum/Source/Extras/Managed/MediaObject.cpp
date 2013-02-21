/*****************************************************************
|
|   Platinum - Managed MediaObject
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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "StdAfx.h"
#include "MediaObject.h"
#include "EnumerableNptArray.h"
#include "EnumerableNptList.h"

IEnumerable<Platinum::PersonRole^>^ Platinum::PeopleInfo::Actors::get()
{
    return gcnew Enumerables::EnumerableNptListRef<PersonRole^, PLT_PersonRole>(
        m_pHandle->actors
        );
}

IEnumerable<Platinum::PersonRole^>^ Platinum::PeopleInfo::Artists::get()
{
    return gcnew Enumerables::EnumerableNptListRef<PersonRole^, PLT_PersonRole>(
        m_pHandle->artists
        );
}

IEnumerable<Platinum::PersonRole^>^ Platinum::PeopleInfo::Authors::get()
{
    return gcnew Enumerables::EnumerableNptListRef<PersonRole^, PLT_PersonRole>(
        m_pHandle->authors
        );
}

IEnumerable<System::String^>^ Platinum::AffiliationInfo::Genres::get()
{
    return gcnew Enumerables::EnumerableNptListRef<String^, NPT_String>(
        m_pHandle->genres
        );
}

IEnumerable<System::String^>^ Platinum::ExtraInfo::Relations::get()
{
    return gcnew Enumerables::EnumerableNptListRef<String^, NPT_String>(
        m_pHandle->relations
        );
}

IEnumerable<Platinum::AlbumArtInfo^>^ Platinum::ExtraInfo::AlbumArts::get()
{
    return gcnew Enumerables::EnumerableNptListRef<AlbumArtInfo^, PLT_AlbumArtInfo>(
        m_pHandle->album_arts
        );
}


Platinum::ProtocolInfo^ Platinum::ProtocolInfo::GetProtocolInfo(String^ filename, 
                                                                bool with_dlna_extension, 
                                                                HttpRequestContext^ context)
{
    return marshal_as<Platinum::ProtocolInfo^>(PLT_ProtocolInfo::GetProtocolInfo(StringConv(filename),
											  with_dlna_extension,
											  (context==nullptr)?NULL:&context->Handle).ToString());
}

Platinum::ProtocolInfo^ Platinum::ProtocolInfo::GetProtocolInfoFromMimeType(String^ mime_type, 
                                                                            bool with_dlna_extension, 
                                                                            HttpRequestContext^ context)
{
    return marshal_as<Platinum::ProtocolInfo^>(PLT_ProtocolInfo::GetProtocolInfoFromMimeType(StringConv(mime_type),
														  with_dlna_extension,
														  (context==nullptr)?NULL:&context->Handle).ToString());
}

Platinum::MediaItem^ Platinum::MediaObject::item()
{
    return gcnew MediaItem(this);
}

Platinum::MediaContainer^ Platinum::MediaObject::container()
{
    return gcnew MediaContainer(this);
}

IEnumerable<Platinum::MediaResource^>^ Platinum::MediaObject::Resources::get()
{
    return gcnew Enumerables::EnumerableNptArrayRef<MediaResource^, PLT_MediaItemResource>(
        (*m_pHandle)->m_Resources
        );
}
