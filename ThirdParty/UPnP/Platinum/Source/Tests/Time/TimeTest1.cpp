/*****************************************************************
|
|   Platinum - Time Test
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
#include <stdlib.h>
#include <stdio.h>
#include "Neptune.h"
#include "Platinum.h"

/*----------------------------------------------------------------------
|       macros
+---------------------------------------------------------------------*/
#define SHOULD_SUCCEED(r)                                        \
    do {                                                         \
        if (NPT_FAILED(r)) {                                     \
            fprintf(stderr, "FAILED: line %d\n", __LINE__);      \
            NPT_ASSERT(0);                                       \
        }                                                        \
    } while(0)                                         

#define SHOULD_FAIL(r)                                           \
    do {                                                         \
        if (NPT_SUCCEEDED(r)) {                                  \
            fprintf(stderr, "should have failed line %d (%d)\n", \
                __LINE__, r);                                    \
            NPT_ASSERT(0);                                       \
        }                                                        \
    } while(0)                                  

#define SHOULD_EQUAL_I(a, b)                                     \
    do {                                                         \
        if ((a) != (b)) {                                        \
            fprintf(stderr, "got %d expected %d line %d\n",      \
                (int)a, (int)b, __LINE__);                       \
            NPT_ASSERT(0);                                       \
        }                                                        \
    } while(0)                                  

#define SHOULD_EQUAL_F(a, b)                                     \
    do {                                                         \
        if ((a) != (b)) {                                        \
            fprintf(stderr, "got %f, expected %f line %d\n",     \
                (float)a, (float)b, __LINE__);                   \
            NPT_ASSERT(0);                                       \
        }                                                        \
    } while(0)                                  

#define SHOULD_EQUAL_S(a, b)                                     \
    do {                                                         \
        if (!NPT_StringsEqual(a,b)) {                            \
            fprintf(stderr, "got %s, expected %s line %d\n",     \
                a, b, __LINE__);                                 \
            NPT_ASSERT(0);                                           \
        }                                                        \
    } while(0)     

/*----------------------------------------------------------------------
|   TestSuiteGetTime
+---------------------------------------------------------------------*/
static void
TestSuiteGetTime()
{
    NPT_TimeStamp now, now2;
    NPT_DateTime  today;

    /* get utc time */
    SHOULD_SUCCEED(NPT_System::GetCurrentTimeStamp(now));

    /* convert utc time to date */
    SHOULD_SUCCEED(today.FromTimeStamp(now));

    /* convert local time back to utc */
    SHOULD_SUCCEED(today.ToTimeStamp(now2));

    /* verify utc time has not change */
    SHOULD_EQUAL_I(now.ToSeconds(), now2.ToSeconds());
}

/*----------------------------------------------------------------------
|   TestSuiteSetDateTimeZone
+---------------------------------------------------------------------*/
static void
TestSuiteSetDateTimeZone()
{
    NPT_TimeStamp now, now2;
    NPT_DateTime today, today2;
    NPT_Int32    tz;

    /* get utc time */
    SHOULD_SUCCEED(NPT_System::GetCurrentTimeStamp(now));

    /* convert utc time to date */
    SHOULD_SUCCEED(today.FromTimeStamp(now));

    for (tz = -60*12; tz <= 60*12; tz+=30) {
        /* convert date to another timezone */
        today2 = today;
        SHOULD_SUCCEED(today2.ChangeTimeZone(tz));

        /* get timestamp from converted date */
        SHOULD_SUCCEED(today2.ToTimeStamp(now2));

        /* verify utc time has not change */
        SHOULD_EQUAL_I(now.ToSeconds(), now2.ToSeconds());
    }
}

/*----------------------------------------------------------------------
|   TestSuiteFormatTime
+---------------------------------------------------------------------*/
static void
TestSuiteFormatTime()
{
    NPT_DateTime  gmt_today, tz_today;
    NPT_TimeStamp now;
    NPT_String    output_s;

    /* current time */
    SHOULD_SUCCEED(NPT_System::GetCurrentTimeStamp(now));

    /* get the date */
    SHOULD_SUCCEED(gmt_today.FromTimeStamp(now));

    /* print out current local date and daylight savings settings */
    /* this should convert to GMT internally if dst is set */
    printf("GMT time for Today is: %s\n", gmt_today.ToString().GetChars());

    /* convert the date to GMT-8 */
    tz_today = gmt_today;
    SHOULD_SUCCEED(tz_today.ChangeTimeZone(-8*60));

    /* this should convert to GMT internally if dst is set */
    printf("(GMT-8) time for Today is: %s\n", tz_today.ToString(NPT_DateTime::FORMAT_RFC_1123).GetChars());
    printf("(GMT-8) time for Today is: %s\n", tz_today.ToString(NPT_DateTime::FORMAT_RFC_1036).GetChars());
    printf("(GMT-8) time for Today is: %s\n", tz_today.ToString(NPT_DateTime::FORMAT_ANSI).GetChars());
    printf("(GMT-8) time for Today is: %s\n", tz_today.ToString(NPT_DateTime::FORMAT_W3C).GetChars());

    /* print with RFC1123 */
    printf("GMT time for Today is: %s\n", gmt_today.ToString(NPT_DateTime::FORMAT_RFC_1123).GetChars());
    printf("GMT time for Today is: %s\n", gmt_today.ToString(NPT_DateTime::FORMAT_RFC_1036).GetChars());
    printf("GMT time for Today is: %s\n", gmt_today.ToString(NPT_DateTime::FORMAT_ANSI).GetChars());
    printf("GMT time for Today is: %s\n", gmt_today.ToString(NPT_DateTime::FORMAT_W3C).GetChars());
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    TestSuiteGetTime();
    TestSuiteSetDateTimeZone();
    TestSuiteFormatTime();
    return 0;
}
