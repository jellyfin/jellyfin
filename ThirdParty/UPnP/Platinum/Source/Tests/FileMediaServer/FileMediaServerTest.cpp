/*****************************************************************
|
|   Platinum - Test UPnP A/V MediaServer
|
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
#include "PltUPnP.h"
#include "PltFileMediaServer.h"

#include <stdlib.h>

NPT_SET_LOCAL_LOGGER("platinum.media.server.file.test")

/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
struct Options {
    const char* path;
    const char* friendly_name;
    NPT_UInt32  port;
} Options;

/*----------------------------------------------------------------------
|   PrintUsageAndExit
+---------------------------------------------------------------------*/
static void
PrintUsageAndExit()
{
    fprintf(stderr, "usage: FileMediaServerTest [-f <friendly_name>] [-p <port>] [-b] <path>\n");
    fprintf(stderr, "-f : optional upnp device friendly name\n");
    fprintf(stderr, "-p : optional http port\n");
    fprintf(stderr, "<path> : local path to serve\n");
    exit(1);
}

/*----------------------------------------------------------------------
|   ParseCommandLine
+---------------------------------------------------------------------*/
static void
ParseCommandLine(char** args)
{
    const char* arg;

    /* default values */
    Options.path     = NULL;
    Options.friendly_name = NULL;
    Options.port = 0;

    while ((arg = *args++)) {
        if (!strcmp(arg, "-f")) {
            Options.friendly_name = *args++;
        } else if (!strcmp(arg, "-p")) {
            if (NPT_FAILED(NPT_ParseInteger32(*args++, Options.port))) {
                fprintf(stderr, "ERROR: invalid argument\n");
                PrintUsageAndExit();
            }
        } else if (Options.path == NULL) {
            Options.path = arg;
        } else {
            fprintf(stderr, "ERROR: too many arguments\n");
            PrintUsageAndExit();
        }
    }

    /* check args */
    if (Options.path == NULL) {
        fprintf(stderr, "ERROR: path missing\n");
        PrintUsageAndExit();
    }
}

/*----------------------------------------------------------------------
|   main
+---------------------------------------------------------------------*/
int
main(int /* argc */, char** argv)
{
    // setup Neptune logging
    NPT_LogManager::GetDefault().Configure("plist:.level=INFO;.handlers=ConsoleHandler;.ConsoleHandler.colors=off;.ConsoleHandler.filter=42");

    /* parse command line */
    ParseCommandLine(argv+1);

	/* for DLNA faster testing */
	//PLT_Constants::GetInstance().m_DefaultDeviceLease = 30.;
    
    PLT_UPnP upnp;
    PLT_DeviceHostReference device(
        new PLT_FileMediaServer(
            Options.path, 
            Options.friendly_name?Options.friendly_name:"Platinum UPnP Media Server",
            false,
            "SAMEDEVICEGUID", // NULL for random ID
            (NPT_UInt16)Options.port)
            );

    NPT_List<NPT_IpAddress> list;
    NPT_CHECK_SEVERE(PLT_UPnPMessageHelper::GetIPAddresses(list));
    NPT_String ip = list.GetFirstItem()->ToString();

    device->m_ModelDescription = "Platinum File Media Server";
    device->m_ModelURL = "http://www.plutinosoft.com/";
    device->m_ModelNumber = "1.0";
    device->m_ModelName = "Platinum File Media Server";
    device->m_Manufacturer = "Plutinosoft";
    device->m_ManufacturerURL = "http://www.plutinosoft.com/";

    upnp.AddDevice(device);
    NPT_String uuid = device->GetUUID();

    NPT_CHECK_SEVERE(upnp.Start());
    NPT_LOG_INFO("Press 'q' to quit.");

    char buf[256];
    while (gets(buf)) {
        if (*buf == 'q')
            break;
    }

    upnp.Stop();

    return 0;
}
