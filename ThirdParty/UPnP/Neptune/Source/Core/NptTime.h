/*****************************************************************
|
|   Neptune - Time
|
| Copyright (c) 2002-2008, Axiomatic Systems, LLC.
| All rights reserved.
|
| Redistribution and use in source and binary forms, with or without
| modification, are permitted provided that the following conditions are met:
|     * Redistributions of source code must retain the above copyright
|       notice, this list of conditions and the following disclaimer.
|     * Redistributions in binary form must reproduce the above copyright
|       notice, this list of conditions and the following disclaimer in the
|       documentation and/or other materials provided with the distribution.
|     * Neither the name of Axiomatic Systems nor the
|       names of its contributors may be used to endorse or promote products
|       derived from this software without specific prior written permission.
|
| THIS SOFTWARE IS PROVIDED BY AXIOMATIC SYSTEMS ''AS IS'' AND ANY
| EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
| WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
| DISCLAIMED. IN NO EVENT SHALL AXIOMATIC SYSTEMS BE LIABLE FOR ANY
| DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
| (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
| ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
| (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
| SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
|
 ****************************************************************/

#ifndef _NPT_TIME_H_
#define _NPT_TIME_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptStrings.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
#define NPT_DATETIME_YEAR_MIN 1901
#define NPT_DATETIME_YEAR_MAX 2262

/*----------------------------------------------------------------------
|   NPT_TimeStamp
+---------------------------------------------------------------------*/
class NPT_TimeStamp
{
 public:
    // methods
    NPT_TimeStamp(const NPT_TimeStamp& timestamp);
    NPT_TimeStamp() : m_NanoSeconds(0) {}
    NPT_TimeStamp(NPT_Int64 nanoseconds) : m_NanoSeconds(nanoseconds) {}
    NPT_TimeStamp(double seconds);
    NPT_TimeStamp& operator+=(const NPT_TimeStamp& time_stamp);
    NPT_TimeStamp& operator-=(const NPT_TimeStamp& time_stamp);
    bool operator==(const NPT_TimeStamp& t) const { return m_NanoSeconds == t.m_NanoSeconds; }
    bool operator!=(const NPT_TimeStamp& t) const { return m_NanoSeconds != t.m_NanoSeconds; }
    bool operator> (const NPT_TimeStamp& t) const { return m_NanoSeconds >  t.m_NanoSeconds; }
    bool operator< (const NPT_TimeStamp& t) const { return m_NanoSeconds <  t.m_NanoSeconds; }
    bool operator>=(const NPT_TimeStamp& t) const { return m_NanoSeconds >= t.m_NanoSeconds; }
    bool operator<=(const NPT_TimeStamp& t) const { return m_NanoSeconds <= t.m_NanoSeconds; }

    // accessors
    void SetNanos(NPT_Int64 nanoseconds) { m_NanoSeconds = nanoseconds;          }
    void SetMicros(NPT_Int64 micros)     { m_NanoSeconds = micros  * 1000;       }
    void SetMillis(NPT_Int64 millis)     { m_NanoSeconds = millis  * 1000000;    }
    void SetSeconds(NPT_Int64 seconds)   { m_NanoSeconds = seconds * 1000000000; }
        
    // conversion
    operator double() const               { return (double)m_NanoSeconds/1E9; }
    void FromNanos(NPT_Int64 nanoseconds) { m_NanoSeconds = nanoseconds;      }
    NPT_Int64 ToNanos() const             { return m_NanoSeconds;             }
    NPT_Int64 ToMicros() const            { return m_NanoSeconds/1000;        }
    NPT_Int64 ToMillis() const            { return m_NanoSeconds/1000000;     }
    NPT_Int64 ToSeconds() const           { return m_NanoSeconds/1000000000;  }
    
private:
    // members
    NPT_Int64 m_NanoSeconds;
};

/*----------------------------------------------------------------------
|   operator+
+---------------------------------------------------------------------*/
inline 
NPT_TimeStamp 
operator+(const NPT_TimeStamp& t1, const NPT_TimeStamp& t2) 
{
    NPT_TimeStamp t = t1;
    return t += t2;
}

/*----------------------------------------------------------------------
|   operator-
+---------------------------------------------------------------------*/
inline 
NPT_TimeStamp 
operator-(const NPT_TimeStamp& t1, const NPT_TimeStamp& t2) 
{
    NPT_TimeStamp t = t1;
    return t -= t2;
}

/*----------------------------------------------------------------------
|   NPT_TimeInterval
+---------------------------------------------------------------------*/
typedef NPT_TimeStamp NPT_TimeInterval;

/*----------------------------------------------------------------------
|   NPT_DateTime
+---------------------------------------------------------------------*/
class NPT_DateTime {
public:
    // types
    enum Format {
        FORMAT_ANSI,
        FORMAT_W3C,
        FORMAT_RFC_1123,  // RFC 822 updated by RFC 1123
        FORMAT_RFC_1036   // RFC 850 updated by RFC 1036
    };
    
    enum FormatFlags {
        FLAG_EMIT_FRACTION      = 1,
        FLAG_EXTENDED_PRECISION = 2
    };
    
    // class methods
    NPT_Int32 GetLocalTimeZone();
    
    // constructors
    NPT_DateTime();
    NPT_DateTime(const NPT_TimeStamp& timestamp, bool local=false);
    
    // methods
    NPT_Result ChangeTimeZone(NPT_Int32 timezone);
    NPT_Result FromTimeStamp(const NPT_TimeStamp& timestamp, bool local=false);
    NPT_Result ToTimeStamp(NPT_TimeStamp& timestamp) const;
    NPT_Result FromString(const char* date, Format format = FORMAT_ANSI);
    NPT_String ToString(Format format = FORMAT_ANSI, NPT_Flags flags=0) const;
    
    // members
    NPT_Int32 m_Year;        // year
    NPT_Int32 m_Month;       // month of the year (1-12)
    NPT_Int32 m_Day;         // day of the month (1-31)
    NPT_Int32 m_Hours;       // hours (0-23)
    NPT_Int32 m_Minutes;     // minutes (0-59)
    NPT_Int32 m_Seconds;     // seconds (0-59)
    NPT_Int32 m_NanoSeconds; // nanoseconds (0-999999999)
    NPT_Int32 m_TimeZone;    // minutes offset from GMT
};

#endif // _NPT_TIME_H_
