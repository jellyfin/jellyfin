/*****************************************************************
|
|   Platinum - Media Browser Wrapper
|
| Copyright (c) 2004-2008, Plutinosoft, LLC.
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
#import "MediaBrowserWrapper.h"

/*----------------------------------------------------------------------
|   DiscoveryWrapper implementation
+---------------------------------------------------------------------*/
@implementation DiscoveryWrapper
@end

/*----------------------------------------------------------------------
|   BrowseResponseWrapper implementation
+---------------------------------------------------------------------*/
@implementation BrowseResponseWrapper
@end

/*----------------------------------------------------------------------
|   PLT_MediaBrowserWrapper::PLT_MediaBrowserWrapper
+---------------------------------------------------------------------*/
PLT_MediaBrowserWrapper::PLT_MediaBrowserWrapper(PLT_CtrlPointReference& control_point) :
    PLT_MediaBrowser(control_point)
{
    PLT_MediaBrowser::SetDelegate(this);
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowserWrapper::~PLT_MediaBrowserWrapper
+---------------------------------------------------------------------*/
PLT_MediaBrowserWrapper::~PLT_MediaBrowserWrapper()
{
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowserWrapper::OnMSAdded
+---------------------------------------------------------------------*/
bool
PLT_MediaBrowserWrapper::OnMSAdded(PLT_DeviceDataReference& device)
{        
    NotifyAddedRemoved(device, true);
    return true;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowserWrapper::OnMSRemoved
+---------------------------------------------------------------------*/
void 
PLT_MediaBrowserWrapper::OnMSRemoved(PLT_DeviceDataReference& device)
{
    NotifyAddedRemoved(device, false);
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowserWrapper::NotifyAddedRemoved
+---------------------------------------------------------------------*/
void 
PLT_MediaBrowserWrapper::NotifyAddedRemoved(PLT_DeviceDataReference& device, 
                                            bool                     added)
{
    if ([m_WrapperDelegate respondsToSelector:@selector(handleDiscovery:)]) {
        DiscoveryWrapper* wrapper = [[DiscoveryWrapper alloc] init];
        wrapper->device = device;
        wrapper->added  = added;
                
        // trigger the handling of the message on the main thread
        [m_WrapperDelegate performSelectorOnMainThread: @selector(handleDiscovery:)
                                            withObject: wrapper
                                         waitUntilDone: YES];
        [wrapper release];
    }
}	

/*----------------------------------------------------------------------
|   PLT_MediaBrowserWrapper::OnBrowseResult
+---------------------------------------------------------------------*/
void 
PLT_MediaBrowserWrapper::OnBrowseResult(NPT_Result               res, 
                                        PLT_DeviceDataReference& device, 
                                        PLT_BrowseInfo*          info, 
                                        void*                    userdata)
{    
    if ([m_WrapperDelegate respondsToSelector:@selector(handleBrowseResponse:)]) {
        BrowseResponseWrapper* wrapper = [[BrowseResponseWrapper alloc] init];
        wrapper->res      = res;
        wrapper->device   = device;
        wrapper->info     = info;
        wrapper->userdata = userdata;
                
        // trigger the handling of the message on the main thread
        [m_WrapperDelegate performSelectorOnMainThread: @selector(handleBrowseResponse:)
                                            withObject: wrapper
                                         waitUntilDone: YES];
        [wrapper release];
    }
}

