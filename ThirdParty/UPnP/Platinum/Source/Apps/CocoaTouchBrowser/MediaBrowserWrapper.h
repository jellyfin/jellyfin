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
#import <Platinum/Platinum.h>

/*----------------------------------------------------------------------
|   DiscoveryWrapper
+---------------------------------------------------------------------*/
@interface DiscoveryWrapper : NSObject
{
    @public
    PLT_DeviceDataReference device;
    bool                    added;
}
@end

/*----------------------------------------------------------------------
|   BrowseResponseWrapper
+---------------------------------------------------------------------*/
@interface BrowseResponseWrapper : NSObject
{
    @public
    NPT_Result              res;
    PLT_DeviceDataReference device;
    PLT_BrowseInfo          *info;
    void                    *userdata;
}
@end

/*----------------------------------------------------------------------
|   MediaBrowserDelegate interface
+---------------------------------------------------------------------*/
@protocol MediaBrowserDelegate

-(void) handleDiscovery: (DiscoveryWrapper*) wrapper;
-(void) handleBrowseResponse: (BrowseResponseWrapper*) wrapper;

@end

/*----------------------------------------------------------------------
|   PLT_MediaBrowserWrapper class
+---------------------------------------------------------------------*/
class PLT_MediaBrowserWrapper : public PLT_MediaBrowser,
                                public PLT_MediaBrowserDelegate
{
public:
    PLT_MediaBrowserWrapper(PLT_CtrlPointReference& control_point);
    virtual ~PLT_MediaBrowserWrapper();
        
    // public methods
    virtual void SetWrapperDelegate(id delegate) { m_WrapperDelegate = delegate; }
    
    // PLT_MediaBrowserDelegate methods
    bool OnMSAdded(PLT_DeviceDataReference& device);
    void OnMSRemoved(PLT_DeviceDataReference& device);
    void OnBrowseResult(NPT_Result               res, 
                        PLT_DeviceDataReference& device, 
                        PLT_BrowseInfo*          info, 
                        void*                    userdata);
        
private:
    void NotifyAddedRemoved(PLT_DeviceDataReference& device,
                            bool                     added);
        
private:
    id m_WrapperDelegate;
};
