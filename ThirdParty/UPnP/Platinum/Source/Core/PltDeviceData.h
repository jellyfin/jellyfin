/*****************************************************************
|
|   Platinum - Device Data
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
 UPnP Device information
 */

#ifndef _PLT_DEVICE_DATA_H_
#define _PLT_DEVICE_DATA_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltConstants.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_Service;
class PLT_DeviceData;

typedef NPT_Reference<PLT_DeviceData> PLT_DeviceDataReference;
typedef NPT_List<PLT_DeviceDataReference> PLT_DeviceDataReferenceList;

/*----------------------------------------------------------------------
|   PLT_DeviceIcon class
+---------------------------------------------------------------------*/
/** 
 The PLT_DeviceIcon class represents a given instance of a UPnP device icon.
 */
class PLT_DeviceIcon
{
public:
    PLT_DeviceIcon(const char* mimetype = "", 
                   NPT_Int32   width = 0, 
                   NPT_Int32   height = 0,
                   NPT_Int32   depth = 0,
                   const char* urlpath = "") :
        m_MimeType(mimetype),
        m_Width(width),
        m_Height(height),
        m_Depth(depth),
        m_UrlPath(urlpath) {}
    virtual ~PLT_DeviceIcon() {}
   
    NPT_String  m_MimeType;
    NPT_Int32   m_Width;
    NPT_Int32   m_Height;
    NPT_Int32   m_Depth;
    NPT_String  m_UrlPath;
};

/*----------------------------------------------------------------------
|   PLT_DeviceData class
+---------------------------------------------------------------------*/
/**
 The PLT_DeviceData class holds information about a device being advertised or
 found by a control point. It maintains a list of services and 
 embedded devices if any.
 */
class PLT_DeviceData
{
public:
    PLT_DeviceData(
        NPT_HttpUrl      description_url = NPT_HttpUrl(NULL, 0, "/"), 
        const char*      uuid = "",
        NPT_TimeInterval lease_time = *PLT_Constants::GetInstance().GetDefaultDeviceLease(),
        const char*      device_type = "",
        const char*      friendly_name = "");

    /* methods */
    virtual NPT_Result  GetDescription(NPT_String& desc);
    virtual NPT_String  GetDescriptionUrl(const char* ip_address = NULL);
    virtual NPT_HttpUrl GetURLBase();
    virtual NPT_HttpUrl NormalizeURL(const NPT_String& url);
    virtual NPT_Result  GetDescription(NPT_XmlElementNode* parent, NPT_XmlElementNode** device = NULL);
    virtual NPT_String  GetIconUrl(const char* mimetype = NULL, NPT_Int32 maxsize = 0, NPT_Int32 maxdepth = 0);

    const NPT_TimeInterval& GetLeaseTime()    const { return m_LeaseTime;        }
    const NPT_String&   GetUUID()             const { return m_UUID;             }
    const NPT_String&   GetFriendlyName()     const { return m_FriendlyName;     }
    const NPT_String&   GetType()             const { return m_DeviceType;       }
    const NPT_String&   GetModelDescription() const { return m_ModelDescription; }
    const NPT_String&   GetParentUUID()       const { return m_ParentUUID;       }
    bool                IsRoot()              { return m_ParentUUID.IsEmpty();   }
    const NPT_IpAddress& GetLocalIP()          const { return m_LocalIfaceIp;     }

    const NPT_Array<PLT_Service*>&            GetServices()        const { return m_Services; }
    const NPT_Array<PLT_DeviceDataReference>& GetEmbeddedDevices() const { return m_EmbeddedDevices; }

    NPT_Result FindEmbeddedDevice(const char* uuid, PLT_DeviceDataReference& device);
    NPT_Result FindEmbeddedDeviceByType(const char* type, PLT_DeviceDataReference& device);
    NPT_Result FindServiceById(const char* id, PLT_Service*& service);
    NPT_Result FindServiceByType(const char* type, PLT_Service*& service);
	NPT_Result FindServiceByName(const char* name, PLT_Service*& service);
    NPT_Result FindServiceBySCPDURL(const char* url, PLT_Service*& service, bool recursive = false);
    NPT_Result FindServiceByControlURL(const char* url, PLT_Service*& service, bool recursive = false);
    NPT_Result FindServiceByEventSubURL(const char* url, PLT_Service*& service, bool recursive = false);

    /* called by PLT_Device subclasses */
    NPT_Result AddEmbeddedDevice(PLT_DeviceDataReference& device);
    NPT_Result RemoveEmbeddedDevice(PLT_DeviceDataReference& device);
    NPT_Result AddService(PLT_Service* service);
	NPT_Result RemoveService(PLT_Service* service);

    operator const char* ();

protected:
    virtual ~PLT_DeviceData();
    virtual void       Cleanup();
    virtual NPT_Result OnAddExtraInfo(NPT_XmlElementNode* /*device_node*/) { return NPT_SUCCESS; }
    NPT_Result         SetLeaseTime(NPT_TimeInterval lease_time, NPT_TimeStamp lease_time_last_update = 0.);

private:
    /* called by PLT_CtrlPoint when new device is discovered */
    NPT_Result    SetURLBase(NPT_HttpUrl& url_base);
    NPT_TimeStamp GetLeaseTimeLastUpdate();
    
    /* class methods */
    static NPT_Result SetDescription(PLT_DeviceDataReference&      root_device,
                                     NPT_TimeInterval              leasetime,
                                     NPT_HttpUrl                   description_url,
                                     const char*                   description, 
                                     const NPT_HttpRequestContext& context);
    static NPT_Result SetDescriptionDevice(PLT_DeviceDataReference&      device,
                                           NPT_XmlElementNode*           device_node, 
                                           const NPT_HttpRequestContext& context);

public:
    NPT_String m_Manufacturer;
    NPT_String m_ManufacturerURL;
    NPT_String m_ModelDescription;
    NPT_String m_ModelName;
    NPT_String m_ModelNumber;
    NPT_String m_ModelURL;
    NPT_String m_SerialNumber;
    NPT_String m_UPC;
    NPT_String m_PresentationURL;
    NPT_String m_DlnaDoc;
    NPT_String m_DlnaCap;
    NPT_String m_AggregationFlags;

protected:
    friend class NPT_Reference<PLT_DeviceData>;
    friend class PLT_CtrlPoint;
    friend class PLT_DeviceReadyIterator;
    friend class PLT_DeviceHost;

    //members
    NPT_String                         m_ParentUUID;
    NPT_String                         m_UUID;
    NPT_HttpUrl                        m_URLDescription;
    NPT_HttpUrl                        m_URLBase;
    NPT_String                         m_DeviceType;
    NPT_String                         m_FriendlyName;
    NPT_TimeInterval                   m_LeaseTime;
    NPT_TimeStamp                      m_LeaseTimeLastUpdate;
    NPT_Array<PLT_Service*>            m_Services;
    NPT_Array<PLT_DeviceDataReference> m_EmbeddedDevices;
    NPT_Array<PLT_DeviceIcon>          m_Icons;

    /* IP address of interface used when retrieving device description.
       We need the info for the control point subscription callback */
    NPT_IpAddress                      m_LocalIfaceIp; 
    NPT_String                         m_Representation;
};

/*----------------------------------------------------------------------
|   PLT_DeviceDataFinder
+---------------------------------------------------------------------*/
/**
 The PLT_DeviceDataFinder class returns a PLT_DeviceData instance given
 a device UUID.
 */
class PLT_DeviceDataFinder
{
public:
    // methods
    PLT_DeviceDataFinder(const char* uuid) : m_UUID(uuid) {}
    virtual ~PLT_DeviceDataFinder() {}

    bool operator()(const PLT_DeviceDataReference& data) const {
        return data->GetUUID().Compare(m_UUID, true) ? false : true;
    }

private:
    // members
    NPT_String m_UUID;
};

/*----------------------------------------------------------------------
|   PLT_DeviceDataFinderByType
+---------------------------------------------------------------------*/
/**
 The PLT_DeviceDataFinderByType class returns a PLT_DeviceData instance 
 given a device type.
 */
class PLT_DeviceDataFinderByType
{
public:
    // methods
    PLT_DeviceDataFinderByType(const char* type) : m_Type(type) {}
    virtual ~PLT_DeviceDataFinderByType() {}

    bool operator()(const PLT_DeviceDataReference& data) const {
        return data->GetType().Compare(m_Type, true) ? false : true;
    }

private:
    // members
    NPT_String m_Type;
};

#endif /* _PLT_DEVICE_DATA_H_ */
