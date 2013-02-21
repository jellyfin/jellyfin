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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltDeviceData.h"
#include "PltService.h"
#include "PltUPnP.h"
#include "PltUtilities.h"

NPT_SET_LOCAL_LOGGER("platinum.core.devicedata")

/*----------------------------------------------------------------------
|   PLT_DeviceData::PLT_DeviceData
+---------------------------------------------------------------------*/
PLT_DeviceData::PLT_DeviceData(NPT_HttpUrl      description_url,
                               const char*      uuid,
                               NPT_TimeInterval lease_time,
                               const char*      device_type,
                               const char*      friendly_name) :
    m_Manufacturer("Plutinosoft LLC"),
    m_ManufacturerURL("http://www.plutinosoft.com"),
    m_UUID(uuid),
    m_URLDescription(description_url),
    m_DeviceType(device_type),
    m_FriendlyName(friendly_name)
{
    if (uuid == NULL || strlen(uuid) == 0) {
        PLT_UPnPMessageHelper::GenerateGUID(m_UUID);
    }

    SetLeaseTime(lease_time);
    SetURLBase(m_URLDescription);
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::~PLT_DeviceData
+---------------------------------------------------------------------*/
PLT_DeviceData::~PLT_DeviceData()
{
    Cleanup();
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::Cleanup
+---------------------------------------------------------------------*/
void
PLT_DeviceData::Cleanup()
{
    m_Services.Apply(NPT_ObjectDeleter<PLT_Service>());
    m_Services.Clear();
    m_EmbeddedDevices.Clear();
    m_Icons.Clear();
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::SetDescriptionUrl
+---------------------------------------------------------------------*/
/*NPT_Result
PLT_DeviceData::SetDescriptionUrl(NPT_HttpUrl& url)
{
    NPT_CHECK_FATAL(SetURLBase(url));
    m_URLDescription = url;
    return NPT_SUCCESS;
}*/

/*----------------------------------------------------------------------
|   PLT_DeviceData::GetDescriptionUrl
+---------------------------------------------------------------------*/
NPT_String
PLT_DeviceData::GetDescriptionUrl(const char* ip_address)
{
    NPT_HttpUrl url = m_URLDescription;

    // replace host with ip address specified
    if (ip_address) url.SetHost(ip_address);
    return url.ToString();
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::SetURLBase
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::SetURLBase(NPT_HttpUrl& url) 
{
    // only http scheme supported
    m_URLBase.SetScheme(url.GetScheme());

    // update port if any
    if (url.GetPort() != NPT_URL_INVALID_PORT) m_URLBase.SetPort(url.GetPort());

    // update host if any
    if (!url.GetHost().IsEmpty()) m_URLBase.SetHost(url.GetHost());

    // update path
    NPT_String path = url.GetPath();

    // remove trailing file according to RFC 2396
    if (!path.EndsWith("/")) {
        int index = path.ReverseFind('/');
        if (index < 0) return NPT_FAILURE;
        path.SetLength(index+1);
    }
    m_URLBase.SetPath(path, true);

    return NPT_SUCCESS;
}    

/*----------------------------------------------------------------------
|   PLT_DeviceData::GetURLBase
+---------------------------------------------------------------------*/
NPT_HttpUrl
PLT_DeviceData::GetURLBase()
{
    return m_URLBase;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::NormalizeURL
+---------------------------------------------------------------------*/
NPT_HttpUrl
PLT_DeviceData::NormalizeURL(const NPT_String& url)
{
    if (url.StartsWith("http://")) return NPT_HttpUrl(url);

    NPT_HttpUrl norm_url = m_URLBase;
    if (url.StartsWith("/")) {
        norm_url.ParsePathPlus(url);
    } else {
        norm_url.ParsePathPlus(norm_url.GetPath() + url);
    }

    return norm_url;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::GetIconUrl
+---------------------------------------------------------------------*/
NPT_String
PLT_DeviceData::GetIconUrl(const char* mimetype, 
                           NPT_Int32   maxsize, 
                           NPT_Int32   maxdepth)
{
    PLT_DeviceIcon icon;

    for (NPT_Cardinal i=0; i<m_Icons.GetItemCount(); i++) {
        if ((mimetype && m_Icons[i].m_MimeType != mimetype) ||
            (maxsize  && m_Icons[i].m_Width > maxsize)      ||
            (maxsize  && m_Icons[i].m_Height > maxsize)     ||
            (maxdepth && m_Icons[i].m_Depth > maxdepth))
            continue;

        // pick the biggest and better resolution we can
        if (icon.m_Width  >= m_Icons[i].m_Width  ||
            icon.m_Height >= m_Icons[i].m_Height ||
            icon.m_Depth  >= m_Icons[i].m_Depth  ||
            m_Icons[i].m_UrlPath.IsEmpty())
            continue;

        icon = m_Icons[i];
    }

    if (icon.m_UrlPath == "") return "";

    return NormalizeURL(icon.m_UrlPath).ToString();
}
/*----------------------------------------------------------------------
|   PLT_DeviceData::SetLeaseTime
+---------------------------------------------------------------------*/
NPT_Result   
PLT_DeviceData::SetLeaseTime(NPT_TimeInterval lease_time, NPT_TimeStamp lease_time_last_update /* = 0 */) 
{
    // Enforce 10 seconds min lease time
    m_LeaseTime = (lease_time.ToSeconds()>=10)?lease_time:*PLT_Constants::GetInstance().GetDefaultDeviceLease();
    
    // get current time as last update time if none passed
    if ((double)lease_time_last_update == 0.) {
        NPT_System::GetCurrentTimeStamp(lease_time_last_update);
    }
    m_LeaseTimeLastUpdate = lease_time_last_update;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::GetLeaseTimeLastUpdate
+---------------------------------------------------------------------*/
NPT_TimeStamp 
PLT_DeviceData::GetLeaseTimeLastUpdate() 
{
    return m_LeaseTimeLastUpdate;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::operator const char*()
+---------------------------------------------------------------------*/
PLT_DeviceData::operator const char*()
{
    NPT_StringOutputStreamReference stream(new NPT_StringOutputStream);
    stream->WriteString("Device GUID: ");
    stream->WriteString((const char*)m_UUID);

    stream->WriteString("Device Type: ");
    stream->WriteString((const char*)m_DeviceType);

    stream->WriteString("Device Base Url: ");
    stream->WriteString((const char*)GetURLBase().ToString());

    stream->WriteString("Device Friendly Name: ");
    stream->WriteString((const char*)m_FriendlyName);

    m_Representation = stream->GetString();
    return m_Representation;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::AddEmbeddedDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::AddEmbeddedDevice(PLT_DeviceDataReference& device)
{
    device->m_ParentUUID = m_UUID;
    return m_EmbeddedDevices.Add(device);
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::RemoveEmbeddedDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::RemoveEmbeddedDevice(PLT_DeviceDataReference& device)
{
    for (NPT_Cardinal i=0;
         i<m_EmbeddedDevices.GetItemCount();
         i++) {
        if (m_EmbeddedDevices[i] == device) return m_EmbeddedDevices.Erase(i);
    }

    return NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::AddService
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::AddService(PLT_Service* service)
{
    if (service->GetServiceType() == "" ||
        service->GetServiceID()   == "" ||
        service->GetSCPDURL()     == "" ||
        service->GetControlURL()  == "" ||
        service->GetEventSubURL() == "") {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return m_Services.Add(service);
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::RemoveService
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::RemoveService(PLT_Service* service)
{
	for (NPT_Cardinal i=0;
         i<m_Services.GetItemCount();
         i++) {
        if (m_Services[i] == service) return m_Services.Erase(i);
    }

    return NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   PLT_GetDescriptionIterator class
+---------------------------------------------------------------------*/
template <class T>
class PLT_GetDescriptionIterator
{
public:
    PLT_GetDescriptionIterator<T>(NPT_XmlElementNode* parent) :
      m_Parent(parent) {}

      NPT_Result operator()(T& data) const {
          return data->GetDescription(m_Parent);
      }

private:
    NPT_XmlElementNode* m_Parent;
};

/*----------------------------------------------------------------------
|   PLT_DeviceData::GetDescription
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::GetDescription(NPT_XmlElementNode* root, NPT_XmlElementNode** device_out)
{
    NPT_XmlElementNode* device = new NPT_XmlElementNode("device");
    if (device_out) *device_out = device;

    NPT_CHECK_SEVERE(root->AddChild(device));

    // device properties
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "deviceType", m_DeviceType));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "friendlyName", m_FriendlyName));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "manufacturer", m_Manufacturer));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "manufacturerURL", m_ManufacturerURL));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "modelDescription", m_ModelDescription));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "modelName", m_ModelName));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "modelURL", m_ModelURL));
    if (!m_ModelNumber.IsEmpty()) NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "modelNumber", m_ModelNumber));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "serialNumber", m_SerialNumber));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "UDN", "uuid:" + m_UUID));
    
    if (!m_PresentationURL.IsEmpty()) {
        NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(device, "presentationURL", m_PresentationURL));
    }
    
    // Extra info not in UPnP specs
    NPT_CHECK(OnAddExtraInfo(device));

    // DLNA support
    if (!m_DlnaDoc.IsEmpty()) {
        NPT_XmlElementNode* dlnadoc = new NPT_XmlElementNode("dlna", "X_DLNADOC");
        NPT_CHECK_SEVERE(dlnadoc->SetNamespaceUri("dlna", "urn:schemas-dlna-org:device-1-0"));
        dlnadoc->AddText(m_DlnaDoc);
        device->AddChild(dlnadoc);
    }
    if (!m_DlnaCap.IsEmpty()) {
        NPT_XmlElementNode* dlnacap = new NPT_XmlElementNode("dlna", "X_DLNACAP");
        NPT_CHECK_SEVERE(dlnacap->SetNamespaceUri("dlna", "urn:schemas-dlna-org:device-1-0"));
        dlnacap->AddText(m_DlnaCap);
        device->AddChild(dlnacap);
    }

    // icons
    if (m_Icons.GetItemCount()) {
        NPT_XmlElementNode* icons = new NPT_XmlElementNode("iconList");
        NPT_CHECK_SEVERE(device->AddChild(icons));
        for (NPT_Cardinal i=0; i<m_Icons.GetItemCount(); i++) {
            NPT_XmlElementNode* icon = new NPT_XmlElementNode("icon");
            NPT_CHECK_SEVERE(icons->AddChild(icon));
            NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(icon, "mimetype", m_Icons[i].m_MimeType));
            NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(icon, "width", NPT_String::FromInteger(m_Icons[i].m_Width)));
            NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(icon, "height", NPT_String::FromInteger(m_Icons[i].m_Height)));
            NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(icon, "depth", NPT_String::FromInteger(m_Icons[i].m_Depth)));
            NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(icon, "url", m_Icons[i].m_UrlPath));
        }
    }

    // services
    NPT_XmlElementNode* services = new NPT_XmlElementNode("serviceList");
    NPT_CHECK_SEVERE(device->AddChild(services));
    NPT_CHECK_SEVERE(m_Services.ApplyUntil(PLT_GetDescriptionIterator<PLT_Service*>(services), 
                                           NPT_UntilResultNotEquals(NPT_SUCCESS)));

    // PS3 support
    if (!m_AggregationFlags.IsEmpty()) {
        NPT_XmlElementNode* aggr = new NPT_XmlElementNode("av", "aggregationFlags");
        NPT_CHECK_SEVERE(aggr->SetNamespaceUri("av", "urn:schemas-sonycom:av"));
        aggr->AddText(m_AggregationFlags);
        device->AddChild(aggr);
    }

    // embedded devices
    if (m_EmbeddedDevices.GetItemCount()) {
        NPT_XmlElementNode* deviceList = new NPT_XmlElementNode("deviceList");
        NPT_CHECK_SEVERE(device->AddChild(deviceList));

        NPT_CHECK_SEVERE(m_EmbeddedDevices.ApplyUntil(
            PLT_GetDescriptionIterator<PLT_DeviceDataReference>(deviceList), 
            NPT_UntilResultNotEquals(NPT_SUCCESS)));
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::GetDescription
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::GetDescription(NPT_String& desc)
{
    NPT_Result res;
    NPT_XmlElementNode* spec = NULL;
    NPT_XmlElementNode* root = new NPT_XmlElementNode("root");

    NPT_CHECK_LABEL_SEVERE(res = root->SetNamespaceUri("", "urn:schemas-upnp-org:device-1-0"), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = root->SetNamespaceUri("dlna", "urn:schemas-dlna-org:device-1-0"), cleanup);

    // add spec version
    spec = new NPT_XmlElementNode("specVersion");
    NPT_CHECK_LABEL_SEVERE(res = root->AddChild(spec), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::AddChildText(spec, "major", "1"), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::AddChildText(spec, "minor", "0"), cleanup);

    // get device xml
    NPT_CHECK_LABEL_SEVERE(res = GetDescription(root), cleanup);

    // serialize node
    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::Serialize(*root, desc, true, 2), cleanup);

cleanup:
    delete root;
    return res;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::SetDescription
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::SetDescription(PLT_DeviceDataReference&      root_device,
                               NPT_TimeInterval              leasetime,
                               NPT_HttpUrl                   description_url,
                               const char*                   description, 
                               const NPT_HttpRequestContext& context)
{
    NPT_XmlParser       parser;
    NPT_XmlNode*        tree = NULL;
    NPT_Result          res;
    NPT_XmlElementNode* root = NULL;
    NPT_String          URLBase;
    
    // create new device if none passed
    if (root_device.IsNull()) {
        root_device = new PLT_DeviceData(description_url, "", leasetime);
    }
    
    res = parser.Parse(description, tree);
    NPT_CHECK_LABEL_SEVERE(res, cleanup);

    root = tree->AsElementNode();
    if (!root || 
        root->GetTag() != "root" || 
        !root->GetNamespace() || 
        *root->GetNamespace() != "urn:schemas-upnp-org:device-1-0") {
        NPT_LOG_INFO_1("root namespace is invalid: %s", 
            (root&&root->GetNamespace())?root->GetNamespace()->GetChars():"null");
        NPT_CHECK_LABEL_SEVERE(NPT_FAILURE, cleanup);
    }

    // look for optional URLBase element
    if (NPT_SUCCEEDED(PLT_XmlHelper::GetChildText(root, "URLBase", URLBase))) {
        NPT_HttpUrl url(URLBase);
		// Some devices like Connect360 try to be funny - not so
        if (url.GetHost().ToLowercase() == "localhost" ||
            url.GetHost().ToLowercase() == "127.0.0.1") {
            url.SetHost(context.GetRemoteAddress().GetIpAddress().ToString());
        }
        root_device->SetURLBase(url);
    } else {
        // No URLBase, derive from description url
        root_device->SetURLBase(description_url);
    }

    // at least one root device child element is required
    NPT_XmlElementNode* device;
    if (!(device = PLT_XmlHelper::GetChild(root, "device"))) {
        NPT_CHECK_LABEL_SEVERE(NPT_FAILURE, cleanup);
    }

    res = SetDescriptionDevice(root_device, device, context);

cleanup:
    // delete the tree
    delete tree;
    return res;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::SetDescriptionDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::SetDescriptionDevice(PLT_DeviceDataReference&      device,
                                     NPT_XmlElementNode*           device_node, 
                                     const NPT_HttpRequestContext& context)
{
    NPT_Result res;
    
    device->m_LocalIfaceIp = context.GetLocalAddress().GetIpAddress();
    
    NPT_CHECK_SEVERE(PLT_XmlHelper::GetChildText(device_node, "deviceType", device->m_DeviceType));
    NPT_CHECK_SEVERE(PLT_XmlHelper::GetChildText(device_node, "UDN", device->m_UUID));

    // remove uuid: prefix
    if (device->m_UUID.StartsWith("uuid:")) {
        device->m_UUID = ((const char*)device->m_UUID)+5;
    }

    // optional attributes
    PLT_XmlHelper::GetChildText(device_node, "friendlyName", device->m_FriendlyName);
    PLT_XmlHelper::GetChildText(device_node, "manufacturer", device->m_Manufacturer);
    PLT_XmlHelper::GetChildText(device_node, "manufacturerURL", device->m_ManufacturerURL);
    PLT_XmlHelper::GetChildText(device_node, "modelDescription", device->m_ModelDescription);
    PLT_XmlHelper::GetChildText(device_node, "modelName", device->m_ModelName);
    PLT_XmlHelper::GetChildText(device_node, "modelURL", device->m_ModelURL);
    PLT_XmlHelper::GetChildText(device_node, "modelNumber", device->m_ModelNumber);
    PLT_XmlHelper::GetChildText(device_node, "serialNumber", device->m_SerialNumber);

    // enumerate icons
    NPT_XmlElementNode* iconList = PLT_XmlHelper::GetChild(device_node, "iconList");
    if (iconList) {
        NPT_Array<NPT_XmlElementNode*> icons;
        PLT_XmlHelper::GetChildren(iconList, icons, "icon");

        for (NPT_Cardinal k=0 ; k<icons.GetItemCount(); k++) {
            PLT_DeviceIcon icon;
            NPT_String integer, height, depth;

            PLT_XmlHelper::GetChildText(icons[k], "mimetype", icon.m_MimeType);
            PLT_XmlHelper::GetChildText(icons[k], "url", icon.m_UrlPath);

            if(NPT_SUCCEEDED(PLT_XmlHelper::GetChildText(icons[k], "width", integer)))
                NPT_ParseInteger32(integer, icon.m_Width);
            if(NPT_SUCCEEDED(PLT_XmlHelper::GetChildText(icons[k], "height", integer)))
                NPT_ParseInteger32(integer, icon.m_Height);
            if(NPT_SUCCEEDED(PLT_XmlHelper::GetChildText(icons[k], "depth", integer)))
                NPT_ParseInteger32(integer, icon.m_Depth);

            device->m_Icons.Add(icon);
        }
    }

    // enumerate device services
    NPT_XmlElementNode* serviceList = PLT_XmlHelper::GetChild(device_node, "serviceList");
    if (serviceList) {
        NPT_Array<NPT_XmlElementNode*> services;
        PLT_XmlHelper::GetChildren(serviceList, services, "service");
        for( int k = 0 ; k < (int)services.GetItemCount(); k++) {
            NPT_String type, id, url;
            PLT_XmlHelper::GetChildText(services[k], "serviceType", type);
            PLT_XmlHelper::GetChildText(services[k], "serviceId", id);    
            PLT_Service* service = new PLT_Service(device.AsPointer(), type, id, NULL);
            
            PLT_XmlHelper::GetChildText(services[k], "SCPDURL", url);
            service->SetSCPDURL(url);
            
            PLT_XmlHelper::GetChildText(services[k], "controlURL", url);
            service->SetControlURL(url);
            
            PLT_XmlHelper::GetChildText(services[k], "eventSubURL", url);
            service->SetEventSubURL(url);
            
            if (NPT_FAILED(res = device->AddService(service))) {
                delete service;
                return res;
            }
        }
    }

    // enumerate embedded devices
    NPT_XmlElementNode* deviceList = PLT_XmlHelper::GetChild(device_node, "deviceList");
    if (deviceList) {
        NPT_Array<NPT_XmlElementNode*> devices;
        PLT_XmlHelper::GetChildren(deviceList, devices, "device");
        for (int k = 0; k<(int)devices.GetItemCount(); k++) {    
            // create an embedded device with same url base and leasetime as parent
            PLT_DeviceDataReference embedded_device(new PLT_DeviceData(device->m_URLDescription, "", device->m_LeaseTime));
            NPT_CHECK_SEVERE(PLT_DeviceData::SetDescriptionDevice(embedded_device, devices[k], context));
            device->AddEmbeddedDevice(embedded_device);
        }
    }
    
    // TODO: Parse extra DLNA stuff

    return NPT_SUCCESS;
}


/*----------------------------------------------------------------------
|   PLT_DeviceData::FindEmbeddedDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::FindEmbeddedDevice(const char*              uuid, 
                                   PLT_DeviceDataReference& device)
{
    NPT_Result res = NPT_ContainerFind(m_EmbeddedDevices, 
        PLT_DeviceDataFinder(uuid), 
        device);
    if (NPT_SUCCEEDED(res)) return res;

    for (int i=0; i<(int)m_EmbeddedDevices.GetItemCount(); i++) {
        res = m_EmbeddedDevices[i]->FindEmbeddedDevice(
            uuid, 
            device);
        if (NPT_SUCCEEDED(res)) return res;
    }

    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::FindEmbeddedDeviceByType
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::FindEmbeddedDeviceByType(const char*              type, 
                                         PLT_DeviceDataReference& device)
{
    NPT_Result res = NPT_ContainerFind(m_EmbeddedDevices, 
        PLT_DeviceDataFinderByType(type), 
        device);
    if (NPT_SUCCEEDED(res)) return res;

    for (int i=0; i<(int)m_EmbeddedDevices.GetItemCount(); i++) {
        res = m_EmbeddedDevices[i]->FindEmbeddedDeviceByType(
            type, 
            device);
        if (NPT_SUCCEEDED(res)) return res;
    }

    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::FindServiceById
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::FindServiceById(const char* id, PLT_Service*& service)
{
    // do not try to find it within embedded devices, since different
    // embedded devices could have an identical service
    return NPT_ContainerFind(m_Services, 
        PLT_ServiceIDFinder(id),
        service);
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::FindServiceByType
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::FindServiceByType(const char* type, PLT_Service*& service)
{
    // do not try to find it within embedded devices, since different
    // embedded devices could have an identical service
    return NPT_ContainerFind(m_Services, 
        PLT_ServiceTypeFinder(type), 
        service);
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::FindServiceByName
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::FindServiceByName(const char* name, PLT_Service*& service)
{
    // do not try to find it within embedded devices, since different
    // embedded devices could have an identical service
    return NPT_ContainerFind(m_Services, 
        PLT_ServiceNameFinder(name), 
        service);
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::FindServiceBySCPDURL
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::FindServiceBySCPDURL(const char*   url,
                                     PLT_Service*& service, 
                                     bool          recursive /* = false */)
{
    NPT_Result res = NPT_ContainerFind(
        m_Services, 
        PLT_ServiceSCPDURLFinder(url), 
        service);
    if (NPT_SUCCEEDED(res)) return res;

    if (recursive) {
		for (int i=0; i<(int)m_EmbeddedDevices.GetItemCount(); i++) {
            res = m_EmbeddedDevices[i]->FindServiceBySCPDURL(
                url, 
                service,
                recursive);
            if (NPT_SUCCEEDED(res)) return res;
        }
    }

    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::FindServiceByControlURL
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::FindServiceByControlURL(const char*   url, 
                                        PLT_Service*& service, 
                                        bool          recursive /* = false */)
{
    NPT_Result res = NPT_ContainerFind(m_Services, 
        PLT_ServiceControlURLFinder(url), 
        service);
    if (NPT_SUCCEEDED(res)) return res;

    if (recursive) {
		for (int i=0; i<(int)m_EmbeddedDevices.GetItemCount(); i++) {
            res = m_EmbeddedDevices[i]->FindServiceByControlURL(
                url, 
                service,
                recursive);
            if (NPT_SUCCEEDED(res)) return res;
        }
    }

    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_DeviceData::FindServiceByEventSubURL
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceData::FindServiceByEventSubURL(const char*   url, 
                                         PLT_Service*& service, 
                                         bool          recursive /* = false */)
{       
    NPT_Result res = NPT_ContainerFind(m_Services, 
        PLT_ServiceEventSubURLFinder(url), 
        service);
    if (NPT_SUCCEEDED(res)) return res;

    if (recursive) {
        for (int i=0; i<(int)m_EmbeddedDevices.GetItemCount(); i++) {
            res = m_EmbeddedDevices[i]->FindServiceByEventSubURL(
                url, 
                service,
                recursive);
            if (NPT_SUCCEEDED(res)) return res;
        }
    }

    return NPT_FAILURE;
}
