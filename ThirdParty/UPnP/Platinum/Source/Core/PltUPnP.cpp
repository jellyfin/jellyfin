/*****************************************************************
|
|   Platinum - UPnP Engine
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
#include "Neptune.h"
#include "PltVersion.h"
#include "PltUPnP.h"
#include "PltDeviceHost.h"
#include "PltCtrlPoint.h"
#include "PltSsdp.h"

NPT_SET_LOCAL_LOGGER("platinum.core.upnp")

/*----------------------------------------------------------------------
|   PLT_UPnP_CtrlPointStartIterator class
+---------------------------------------------------------------------*/
class PLT_UPnP_CtrlPointStartIterator
{
public:
    PLT_UPnP_CtrlPointStartIterator(PLT_SsdpListenTask* listen_task) :
        m_ListenTask(listen_task)  {}
    virtual ~PLT_UPnP_CtrlPointStartIterator() {}

    NPT_Result operator()(PLT_CtrlPointReference& ctrl_point) const {
        NPT_CHECK_SEVERE(ctrl_point->Start(m_ListenTask));
        return NPT_SUCCESS;
    }

private:
    PLT_SsdpListenTask* m_ListenTask;
};

/*----------------------------------------------------------------------
|   PLT_UPnP_CtrlPointStopIterator class
+---------------------------------------------------------------------*/
class PLT_UPnP_CtrlPointStopIterator
{
public:
    PLT_UPnP_CtrlPointStopIterator(PLT_SsdpListenTask* listen_task) :
        m_ListenTask(listen_task)  {}
    virtual ~PLT_UPnP_CtrlPointStopIterator() {}

    NPT_Result operator()(PLT_CtrlPointReference& ctrl_point) const {
        return ctrl_point->Stop(m_ListenTask);
    }


private:
    PLT_SsdpListenTask* m_ListenTask;
};

/*----------------------------------------------------------------------
|   PLT_UPnP_DeviceStartIterator class
+---------------------------------------------------------------------*/
class PLT_UPnP_DeviceStartIterator
{
public:
    PLT_UPnP_DeviceStartIterator(PLT_SsdpListenTask* listen_task) :
        m_ListenTask(listen_task)  {}
    virtual ~PLT_UPnP_DeviceStartIterator() {}

    NPT_Result operator()(PLT_DeviceHostReference& device_host) const {
        NPT_CHECK_SEVERE(device_host->Start(m_ListenTask));
        return NPT_SUCCESS;
    }

private:
    PLT_SsdpListenTask* m_ListenTask;
};

/*----------------------------------------------------------------------
|   PLT_UPnP_DeviceStopIterator class
+---------------------------------------------------------------------*/
class PLT_UPnP_DeviceStopIterator
{
public:
    PLT_UPnP_DeviceStopIterator(PLT_SsdpListenTask* listen_task) :
        m_ListenTask(listen_task)  {}
    virtual ~PLT_UPnP_DeviceStopIterator() {}

    NPT_Result operator()(PLT_DeviceHostReference& device_host) const {
        return device_host->Stop(m_ListenTask);
    }


private:
    PLT_SsdpListenTask* m_ListenTask;
};

/*----------------------------------------------------------------------
|   PLT_UPnP::PLT_UPnP
+---------------------------------------------------------------------*/
PLT_UPnP::PLT_UPnP() :
    m_Started(false),
    m_SsdpListenTask(NULL),
	m_IgnoreLocalUUIDs(true)
{
}
    
/*----------------------------------------------------------------------
|   PLT_UPnP::~PLT_UPnP
+---------------------------------------------------------------------*/
PLT_UPnP::~PLT_UPnP()
{
    Stop();

    m_CtrlPoints.Clear();
    m_Devices.Clear();
}

/*----------------------------------------------------------------------
|   PLT_UPnP::Start()
+---------------------------------------------------------------------*/
NPT_Result
PLT_UPnP::Start()
{
    NPT_LOG_INFO("Starting UPnP...");

    NPT_AutoLock lock(m_Lock);

    if (m_Started == true) NPT_CHECK_SEVERE(NPT_ERROR_INVALID_STATE);
    
    NPT_List<NPT_IpAddress> ips;
    PLT_UPnPMessageHelper::GetIPAddresses(ips);
    
    /* Create multicast socket and bind on 1900. If other apps didn't
       play nicely by setting the REUSE_ADDR flag, this could fail */
    NPT_UdpMulticastSocket* socket = new NPT_UdpMulticastSocket();
    NPT_CHECK_SEVERE(socket->Bind(NPT_SocketAddress(NPT_IpAddress::Any, 1900), true));
    
    /* Join multicast group for every ip we found */
    NPT_CHECK_SEVERE(ips.ApplyUntil(PLT_SsdpInitMulticastIterator(socket),
                                    NPT_UntilResultNotEquals(NPT_SUCCESS)));

    /* create the ssdp listener */
    m_SsdpListenTask = new PLT_SsdpListenTask(socket);
    NPT_CHECK_SEVERE(m_TaskManager.StartTask(m_SsdpListenTask));

    /* start devices & ctrlpoints */
    // TODO: Starting devices and ctrlpoints could fail?
    m_CtrlPoints.Apply(PLT_UPnP_CtrlPointStartIterator(m_SsdpListenTask));
    m_Devices.Apply(PLT_UPnP_DeviceStartIterator(m_SsdpListenTask));

    m_Started = true;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_UPnP::Stop
+---------------------------------------------------------------------*/
NPT_Result
PLT_UPnP::Stop()
{
    NPT_AutoLock lock(m_Lock);

    if (m_Started == false) return NPT_ERROR_INVALID_STATE;

    NPT_LOG_INFO("Stopping UPnP...");

    // Stop ctrlpoints and devices first
    m_CtrlPoints.Apply(PLT_UPnP_CtrlPointStopIterator(m_SsdpListenTask));
    m_Devices.Apply(PLT_UPnP_DeviceStopIterator(m_SsdpListenTask));

    // stop remaining tasks
    m_TaskManager.StopAllTasks();
    m_SsdpListenTask = NULL;

    m_Started = false;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_UPnP::AddDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_UPnP::AddDevice(PLT_DeviceHostReference& device)
{
    NPT_AutoLock lock(m_Lock);

    // tell all our controllers to ignore this device
	if (m_IgnoreLocalUUIDs) {
		for (NPT_List<PLT_CtrlPointReference>::Iterator iter = 
                 m_CtrlPoints.GetFirstItem(); 
             iter; 
             iter++) {
		    (*iter)->IgnoreUUID(device->GetUUID());
		}
	}

    if (m_Started) {
        NPT_LOG_INFO("Starting Device...");
        NPT_CHECK_SEVERE(device->Start(m_SsdpListenTask));
    }

    m_Devices.Add(device);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_UPnP::RemoveDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_UPnP::RemoveDevice(PLT_DeviceHostReference& device)
{
    NPT_AutoLock lock(m_Lock);

    if (m_Started) {
        device->Stop(m_SsdpListenTask);
    }

    return m_Devices.Remove(device);
}

/*----------------------------------------------------------------------
|   PLT_UPnP::AddCtrlPoint
+---------------------------------------------------------------------*/
NPT_Result
PLT_UPnP::AddCtrlPoint(PLT_CtrlPointReference& ctrl_point)
{
    NPT_AutoLock lock(m_Lock);

    // tell the control point to ignore our own running devices
	if (m_IgnoreLocalUUIDs) {
		for (NPT_List<PLT_DeviceHostReference>::Iterator iter = 
                 m_Devices.GetFirstItem(); 
             iter; 
             iter++) {
			ctrl_point->IgnoreUUID((*iter)->GetUUID());
		}
	}

    if (m_Started) {
        NPT_LOG_INFO("Starting Ctrlpoint...");
        NPT_CHECK_SEVERE(ctrl_point->Start(m_SsdpListenTask));
    }

    m_CtrlPoints.Add(ctrl_point);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_UPnP::RemoveCtrlPoint
+---------------------------------------------------------------------*/
NPT_Result
PLT_UPnP::RemoveCtrlPoint(PLT_CtrlPointReference& ctrl_point)
{
    NPT_AutoLock lock(m_Lock);

    if (m_Started) {
        ctrl_point->Stop(m_SsdpListenTask);
    }

    return m_CtrlPoints.Remove(ctrl_point);
}

