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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltConstants.h"
#include "PltHttp.h"

static PLT_Constants Constants; 

/*----------------------------------------------------------------------
|   PLT_Constants::PLT_Constant
+---------------------------------------------------------------------*/
PLT_Constants::PLT_Constants()
{
    SetDefaultUserAgent(PLT_HTTP_DEFAULT_USER_AGENT);
    SetDefaultDeviceLease(NPT_TimeInterval(1800.));
    SetDefaultSubscribeLease(NPT_TimeInterval(1800.));
    SetAnnounceMulticastTimeToLive(4);
    SetSearchMulticastTimeToLive(4);
}

/*----------------------------------------------------------------------
|   PLT_Constants::GetInstance
+---------------------------------------------------------------------*/
PLT_Constants&    
PLT_Constants::GetInstance()
{
    return Constants;
}
