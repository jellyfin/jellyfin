/*****************************************************************
|
|   Platinum - Leaks
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
#include "PltLeaks.h"

#if defined(WIN32)
#include <crtdbg.h>
#include <stdio.h>
#include "string.h"

/*----------------------------------------------------------------------
|   PLT_Leak_AllocHook
+---------------------------------------------------------------------*/
int PLT_Leak_AllocHook(int                  alloc_type, 
                       void*                user_data, 
                       size_t               size, 
                       int                  block_type, 
                       long                 request_number, 
                       const unsigned char* filename, 
                       int                  line_number)
{
    (void)alloc_type;
    (void)user_data;
    (void)size;
    (void)block_type;
    (void)request_number;
    (void)line_number;
    (void)filename;
   /*
    * if (request_number == 34556)
    *   return 2;
    *
    */
    return 1;
}

/*----------------------------------------------------------------------
|   PLT_Leak_Enable
+---------------------------------------------------------------------*/
void
PLT_Leak_Enable(void) 
{
#if defined(_DEBUG)
    /*
    * If you want VC to dump file name and line number of leaking resource
    * use #define _CRTDBG_MAP_ALLOC in suspected file (project)
    * and #include "crtdbg.h" in suspected file
    */
_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF    |
               _CRTDBG_CHECK_ALWAYS_DF |
               _CRTDBG_LEAK_CHECK_DF);

_CrtSetAllocHook(PLT_Leak_AllocHook );

#endif
}
#else
/*----------------------------------------------------------------------
|   PLT_Leak_Enable
+---------------------------------------------------------------------*/
void
PLT_Leak_Enable(void) 
{
}
#endif
