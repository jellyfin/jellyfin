/*****************************************************************
|
|   Platinum - UPnP Constants
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
 UPnP Constants
 */

#ifndef _PLT_UPNP_CONSTANTS_H_
#define _PLT_UPNP_CONSTANTS_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|   PLT_Constants
+---------------------------------------------------------------------*/
/**
 The PLT_Constants class provides a way to globally set or get certain 
 UPnP constants.
 */
class PLT_Constants
{
public:
    // class methods
    static PLT_Constants& GetInstance();
    
    PLT_Constants();
    ~PLT_Constants() {};
    
    void SetDefaultDeviceLease(NPT_TimeInterval lease) { m_DefaultDeviceLease = new NPT_TimeInterval(lease); }
    NPT_Reference<NPT_TimeInterval> GetDefaultDeviceLease() { return m_DefaultDeviceLease; }
  
    void SetDefaultSubscribeLease(NPT_TimeInterval lease) { m_DefaultSubscribeLease = new NPT_TimeInterval(lease); }
    NPT_Reference<NPT_TimeInterval> GetDefaultSubscribeLease() { return m_DefaultSubscribeLease; }
    
    void SetDefaultUserAgent(const char* agent) { m_DefaultUserAgent = new NPT_String(agent); }
    NPT_Reference<NPT_String> GetDefaultUserAgent() { return m_DefaultUserAgent; }
    
    void SetSearchMulticastTimeToLive(NPT_UInt32 ttl) { m_SearchMulticastTimeToLive = ttl; }
    NPT_UInt32 GetSearchMulticastTimeToLive() { return m_SearchMulticastTimeToLive; }

    void SetAnnounceMulticastTimeToLive(NPT_UInt32 ttl) { m_AnnounceMulticastTimeToLive = ttl; }
    NPT_UInt32 GetAnnounceMulticastTimeToLive() { return m_AnnounceMulticastTimeToLive; }

private:
    // members
    NPT_Reference<NPT_TimeInterval> m_DefaultDeviceLease;
    NPT_Reference<NPT_TimeInterval> m_DefaultSubscribeLease;
    NPT_Reference<NPT_String>       m_DefaultUserAgent;
    NPT_UInt32                      m_SearchMulticastTimeToLive;
    NPT_UInt32                      m_AnnounceMulticastTimeToLive;
};

#endif /* _PLT_UPNP_CONSTANTS_H_ */
