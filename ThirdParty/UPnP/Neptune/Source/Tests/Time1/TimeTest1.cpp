/*****************************************************************
|
|      Time Test Program 1
|
|      (c) 2005-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <stdlib.h>
#include <stdio.h>
#include "Neptune.h"
#include "NptResults.h"

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

#define SHOULD_EQUAL(a, b)                                       \
    do {                                                         \
        if ((a) != (b)) {                                        \
            fprintf(stderr, "not equal, line %d\n", __LINE__);   \
            NPT_ASSERT(0);                                       \
        }                                                        \
    } while(0)                                  

#define SHOULD_EQUAL_I(a, b)                                     \
    do {                                                         \
        if ((a) != (b)) {                                        \
            fprintf(stderr, "got %d, expected %d line %d\n",     \
                a, b, __LINE__);                                 \
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
            NPT_ASSERT(0);                                       \
        }                                                        \
    } while(0)     

/*----------------------------------------------------------------------
|   TestMisc
+---------------------------------------------------------------------*/
static void
TestMisc()
{
    NPT_DateTime  date;
    NPT_TimeStamp ts;
    NPT_String    s;
    
    NPT_System::GetCurrentTimeStamp(ts);
    SHOULD_SUCCEED(date.FromTimeStamp(ts, false));
    s = date.ToString(NPT_DateTime::FORMAT_W3C);
    NPT_Console::OutputF("%s\n", s.GetChars());
    s = date.ToString(NPT_DateTime::FORMAT_ANSI);
    NPT_Console::OutputF("%s\n", s.GetChars());
    s = date.ToString(NPT_DateTime::FORMAT_RFC_1036);
    NPT_Console::OutputF("%s\n", s.GetChars());
    s = date.ToString(NPT_DateTime::FORMAT_RFC_1123);
    NPT_Console::OutputF("%s\n", s.GetChars());
    SHOULD_SUCCEED(date.FromTimeStamp(ts, true));
    s = date.ToString(NPT_DateTime::FORMAT_W3C);
    NPT_Console::OutputF("%s\n", s.GetChars());
    s = date.ToString(NPT_DateTime::FORMAT_ANSI);
    NPT_Console::OutputF("%s\n", s.GetChars());
    s = date.ToString(NPT_DateTime::FORMAT_RFC_1036);
    NPT_Console::OutputF("%s\n", s.GetChars());
    s = date.ToString(NPT_DateTime::FORMAT_RFC_1123);
    NPT_Console::OutputF("%s\n", s.GetChars());

    ts = 0.0;
    SHOULD_SUCCEED(date.FromTimeStamp(ts, false));
    s = date.ToString(NPT_DateTime::FORMAT_W3C);
    SHOULD_EQUAL_S(s.GetChars(), "1970-01-01T00:00:00Z");
    s = date.ToString(NPT_DateTime::FORMAT_ANSI);
    SHOULD_EQUAL_S(s.GetChars(), "Thu Jan  1 00:00:00 1970");
    s = date.ToString(NPT_DateTime::FORMAT_RFC_1036);
    SHOULD_EQUAL_S(s.GetChars(), "Thursday, 01-Jan-70 00:00:00 GMT");
    s = date.ToString(NPT_DateTime::FORMAT_RFC_1123);
    SHOULD_EQUAL_S(s.GetChars(), "Thu, 01 Jan 1970 00:00:00 GMT");
    
    ts.SetSeconds(0xFFFFFFFF);
    SHOULD_SUCCEED(date.FromTimeStamp(ts, false));
    s = date.ToString(NPT_DateTime::FORMAT_W3C, false);
    SHOULD_EQUAL_S(s.GetChars(), "2106-02-07T06:28:15Z");

    NPT_TimeStamp now;
    NPT_System::GetCurrentTimeStamp(now);
    NPT_DateTime now_local(now, true);
    NPT_DateTime now_utc(now, false);
    SHOULD_EQUAL_I(now_utc.m_TimeZone, 0);
    NPT_TimeStamp ts1, ts2;
    now_local.ToTimeStamp(ts1);
    now_utc.ToTimeStamp(ts2);
    SHOULD_EQUAL_I((int)ts1.ToSeconds(), (int)ts2.ToSeconds());
    
    ts.SetSeconds(0);
    NPT_DateTime d1(ts);
    ts.SetSeconds(ts.ToSeconds()-3600);
    NPT_DateTime d2(ts);
    d1.ToTimeStamp(ts1);
    d2.ToTimeStamp(ts2);
    SHOULD_EQUAL_I((int)ts1.ToSeconds(), (int)ts2.ToSeconds()+3600);
}

/*----------------------------------------------------------------------
|   TestDateFromTimeStringW3C
+---------------------------------------------------------------------*/
static void
TestDateFromTimeStringW3C()
{
    NPT_DateTime date;

    /* Valid date */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01:10.003Z", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 3000000);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date, 2 characters milliseconds */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01:10.02Z", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 20000000);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date, 1 character milliseconds */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01:10.9Z", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 900000000);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date, no seconds, Z */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01Z", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 0);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date, no seconds, timezone */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01+03:00", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 0);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 180);

    /* Valid date, no millimseconds */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01:10Z", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date with microseconds, 'Z' */
    SHOULD_SUCCEED(date.FromString("2005-09-06T17:16:10.003498Z", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2005);
    SHOULD_EQUAL_I(date.m_Month        , 9);
    SHOULD_EQUAL_I(date.m_Day          , 6);
    SHOULD_EQUAL_I(date.m_Hours        , 17);
    SHOULD_EQUAL_I(date.m_Minutes      , 16);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 3498000);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date, no milliseconds, with timezone offset */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01:10+03:00", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 180);

    /* Valid date, no milliseconds, with negative m_TimeZone offset */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01:10-05:00", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , -300);

    /* Valid date, with milliseconds, with positive m_TimeZone offset */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01:10.200+03:00", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 200000000);
    SHOULD_EQUAL_I(date.m_TimeZone     , 180);

    /* Valid date, with milliseconds, with negative m_TimeZone offset */
    SHOULD_SUCCEED(date.FromString("2006-04-14T12:01:10.030-05:00", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 30000000);
    SHOULD_EQUAL_I(date.m_TimeZone     , -300);

    /* Valid date with microseconds and negative m_TimeZone offset */
    SHOULD_SUCCEED(date.FromString("2005-09-06T17:16:10.001822-05:00", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2005);
    SHOULD_EQUAL_I(date.m_Month        , 9);
    SHOULD_EQUAL_I(date.m_Day          , 6);
    SHOULD_EQUAL_I(date.m_Hours        , 17);
    SHOULD_EQUAL_I(date.m_Minutes      , 16);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 1822000);
    SHOULD_EQUAL_I(date.m_TimeZone     , -300);
    
    /* Valid date with microseconds and positive m_TimeZone offset */
    SHOULD_SUCCEED(date.FromString("2005-09-06T17:16:10.001822+05:00", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2005);
    SHOULD_EQUAL_I(date.m_Month        , 9);
    SHOULD_EQUAL_I(date.m_Day          , 6);
    SHOULD_EQUAL_I(date.m_Hours        , 17);
    SHOULD_EQUAL_I(date.m_Minutes      , 16);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 1822000);
    SHOULD_EQUAL_I(date.m_TimeZone     , 300);
    
    /* Valid date with no time and m_TimeZone offset */
    SHOULD_SUCCEED(date.FromString("2006-10-05", NPT_DateTime::FORMAT_W3C));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 10);
    SHOULD_EQUAL_I(date.m_Day          , 5);
    
    /* Invalid date with 3 digit year */
    SHOULD_FAIL(date.FromString("206-04-14T12:01:10.003Z", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with 5 digit year */
    SHOULD_FAIL(date.FromString("20076-04-14T12:01:10.003Z", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with 5 digit year */
    SHOULD_FAIL(date.FromString("20076-04-14T12:01:10.003Z", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with garbage in the end */
    SHOULD_FAIL(date.FromString("2006-04-14T12:01:10.003+69:696", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad month */
    SHOULD_FAIL(date.FromString("2006-010-14T12:01:10.003", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad month, right overall length */
    SHOULD_FAIL(date.FromString("2063-0--14T12:01:10.003", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad year-month separator */
    SHOULD_FAIL(date.FromString("2063Y08-14T12:01:10.003", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad time separator */
    SHOULD_FAIL(date.FromString("2063-08-14t12:01:10.003", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad hour */
    SHOULD_FAIL(date.FromString("2063-08-14T012:01:10.003", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad GMT indicator */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10.003z", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad GMT indicator */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10.003g", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with millisecond separator but no digits */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10.", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with millisecond separator but no digits */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10.Z", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with millisecond separator but no digits */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10.+10:38", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad m_TimeZone offset */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10+10:338", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad m_TimeZone offset */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10+001:38", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad m_TimeZone offset */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10+10:33Z", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad m_TimeZone offset */
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10.08+10:33Z", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with bad m_TimeZone offset with m_Seconds*/
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10.08+10:33:30", NPT_DateTime::FORMAT_W3C));

    /* Invalid date with m_TimeZone offset too big*/
    SHOULD_FAIL(date.FromString("2063-08-14T12:01:10.08+14:33", NPT_DateTime::FORMAT_W3C));
}

/*----------------------------------------------------------------------
|   TestDateFromTimeStringANSI
+---------------------------------------------------------------------*/
static void
TestDateFromTimeStringANSI()
{
    NPT_DateTime date;

    /* Valid date */
    SHOULD_SUCCEED(date.FromString("Fri Apr 14 12:01:10 2006", NPT_DateTime::FORMAT_ANSI));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date with space in the days */
    SHOULD_SUCCEED(date.FromString("Fri Apr  7 12:01:10 2006", NPT_DateTime::FORMAT_ANSI));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 7);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Wrong weekday */
    SHOULD_FAIL(date.FromString("Wed Apr 14 12:01:10 2006", NPT_DateTime::FORMAT_ANSI));

    /* Wrong year length */
    SHOULD_FAIL(date.FromString("Mon Apr 14 12:01:10 95", NPT_DateTime::FORMAT_ANSI));
}

/*----------------------------------------------------------------------
|   TestDateFromTimeStringRFC_1036
+---------------------------------------------------------------------*/
static void
TestDateFromTimeStringRFC_1036()
{
    NPT_DateTime date;

    /* Valid date */
    SHOULD_SUCCEED(date.FromString("Friday, 14-Apr-2006 12:01:10 UT", NPT_DateTime::FORMAT_RFC_1036));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date with timezone */
    SHOULD_SUCCEED(date.FromString("Friday, 14-Apr-95 12:01:10 GMT", NPT_DateTime::FORMAT_RFC_1036));
    SHOULD_EQUAL_I(date.m_Year         , 1995);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date with timezone */
    SHOULD_SUCCEED(date.FromString("Friday, 14-Apr-95 12:01:10 -0800", NPT_DateTime::FORMAT_RFC_1036));
    SHOULD_EQUAL_I(date.m_Year         , 1995);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , -8*60);

    /* Wrong day name */
    SHOULD_FAIL(date.FromString("Fri, 14-Apr-95 12:01:10 -0800", NPT_DateTime::FORMAT_RFC_1036));

    /* Wrong weekday */
    SHOULD_FAIL(date.FromString("Wednesday, 14-Apr-95 12:01:10 GMT", NPT_DateTime::FORMAT_RFC_1036));

    /* Wrong year length */
    SHOULD_FAIL(date.FromString("Monday, 14-Apr-1995 12:01:10 GMT", NPT_DateTime::FORMAT_RFC_1036));
}

/*----------------------------------------------------------------------
|   TestDateFromTimeStringRFC_1123
+---------------------------------------------------------------------*/
static void
TestDateFromTimeStringRFC_1123()
{
    NPT_DateTime date;

    /* Valid date */
    SHOULD_SUCCEED(date.FromString("Fri, 14 Apr 2006 12:01:10 UT", NPT_DateTime::FORMAT_RFC_1123));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date with timezone*/
    SHOULD_SUCCEED(date.FromString("Fri, 14 Apr 2006 12:01:10 GMT", NPT_DateTime::FORMAT_RFC_1123));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Valid date with timezone*/
    SHOULD_SUCCEED(date.FromString("Fri, 14 Apr 2006 12:01:10 +0800", NPT_DateTime::FORMAT_RFC_1123));
    SHOULD_EQUAL_I(date.m_Year         , 2006);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 8*60);

    /* Valid date, short year */
    SHOULD_SUCCEED(date.FromString("Fri, 14 Apr 95 12:01:10 GMT", NPT_DateTime::FORMAT_RFC_1123));
    SHOULD_EQUAL_I(date.m_Year         , 1995);
    SHOULD_EQUAL_I(date.m_Month        , 4);
    SHOULD_EQUAL_I(date.m_Day          , 14);
    SHOULD_EQUAL_I(date.m_Hours        , 12);
    SHOULD_EQUAL_I(date.m_Minutes      , 1);
    SHOULD_EQUAL_I(date.m_Seconds      , 10);
    SHOULD_EQUAL_I(date.m_NanoSeconds  , 0);
    SHOULD_EQUAL_I(date.m_TimeZone     , 0);

    /* Wrong day name */
    SHOULD_FAIL(date.FromString("Friday, 14 Apr 95 12:01:10 GMT", NPT_DateTime::FORMAT_RFC_1123));

    /* Wrong weekday */
    SHOULD_FAIL(date.FromString("Wed, 14 Apr 2006 12:01:10 GMT", NPT_DateTime::FORMAT_RFC_1123));

    /* Wrong year length */
    SHOULD_FAIL(date.FromString("Mon, 14 Apr 95 12:01:10 GMT", NPT_DateTime::FORMAT_RFC_1123));
}

/*----------------------------------------------------------------------
|   TestRandom
+---------------------------------------------------------------------*/
static void
TestRandom()
{
    for (unsigned int i=0; i<10000; i++) {
        NPT_TimeStamp ts((double)NPT_System::GetRandomInteger());
        NPT_TimeStamp ts2;
        NPT_DateTime date;
        SHOULD_SUCCEED(date.FromTimeStamp(ts, false));
        SHOULD_SUCCEED(date.ToTimeStamp(ts2));
        NPT_String ds;
        NPT_DateTime ndate;
        ds = date.ToString(NPT_DateTime::FORMAT_ANSI);
        ndate.FromString(ds);
        //SHOULD_EQUAL(date, ndate);
        SHOULD_SUCCEED(ndate.ToTimeStamp(ts2));
        SHOULD_EQUAL_F((double)ts2.ToSeconds(), (double)ts.ToSeconds());
        
        ds = date.ToString(NPT_DateTime::FORMAT_W3C);
        ndate.FromString(ds);
        //SHOULD_EQUAL(date, ndate);
        SHOULD_SUCCEED(ndate.ToTimeStamp(ts2));
        SHOULD_EQUAL_F((double)ts2.ToSeconds(), (double)ts.ToSeconds());

        ds = date.ToString(NPT_DateTime::FORMAT_RFC_1123);
        ndate.FromString(ds);
        //SHOULD_EQUAL(date, ndate);
        SHOULD_SUCCEED(ndate.ToTimeStamp(ts2));
        SHOULD_EQUAL_F((double)ts2.ToSeconds(), (double)ts.ToSeconds());

        ds = date.ToString(NPT_DateTime::FORMAT_RFC_1036);
        ndate.FromString(ds);
        //SHOULD_EQUAL(date, ndate);
        SHOULD_SUCCEED(ndate.ToTimeStamp(ts2));
        SHOULD_EQUAL_F((double)ts2.ToSeconds(), (double)ts.ToSeconds());
    }
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    TestMisc();
    TestDateFromTimeStringW3C();
    TestDateFromTimeStringANSI();
    TestDateFromTimeStringRFC_1036();
    TestDateFromTimeStringRFC_1123();
    TestRandom();
    return 0;
}
