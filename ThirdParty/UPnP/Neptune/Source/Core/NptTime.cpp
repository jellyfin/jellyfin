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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTime.h"
#include "NptUtils.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const char* const NPT_TIME_DAYS_SHORT[] = {"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"}; 
const char* const NPT_TIME_DAYS_LONG[]  = {"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"}; 
const char* const NPT_TIME_MONTHS[]     = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};

static const NPT_Int32 NPT_TIME_MONTH_DAY[]      = {-1, 30, 58, 89, 119, 150, 180, 211, 242, 272, 303, 333, 364 };
static const NPT_Int32 NPT_TIME_MONTH_DAY_LEAP[] = {-1, 30, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
static const NPT_Int32 NPT_TIME_ELAPSED_DAYS_AT_MONTH[13] = {
    0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
};

const NPT_Int32 NPT_SECONDS_PER_DAY  = (24L * 60L * 60L);
const NPT_Int32 NPT_SECONDS_PER_YEAR = (365L * NPT_SECONDS_PER_DAY);

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
#define NPT_TIME_YEAR_IS_LEAP(_y) ((((_y)%4 == 0) && ((_y)%100 != 0)) || ((_y)%400 == 0)) 
#define NPT_TIME_CHECK_BOUNDS(_var, _low, _high) do {                                       \
    if (((_var)<(_low)) || ((_var)>(_high))) {                                              \
        return NPT_ERROR_OUT_OF_RANGE;                                                      \
    }                                                                                       \
} while (0)

/*----------------------------------------------------------------------
|   NPT_TimeStamp::NPT_TimeStamp
+---------------------------------------------------------------------*/
NPT_TimeStamp::NPT_TimeStamp(const NPT_TimeStamp& timestamp)
{
    m_NanoSeconds = timestamp.m_NanoSeconds;
}

/*----------------------------------------------------------------------
|   NPT_TimeStamp::NPT_TimeStamp
+---------------------------------------------------------------------*/
NPT_TimeStamp::NPT_TimeStamp(double seconds)
{
    m_NanoSeconds = (NPT_Int64)(seconds * 1e9);
}

/*----------------------------------------------------------------------
|   NPT_TimeStamp::operator+=
+---------------------------------------------------------------------*/
NPT_TimeStamp&
NPT_TimeStamp::operator+=(const NPT_TimeStamp& t)
{
    m_NanoSeconds += t.m_NanoSeconds;
    return *this;
}

/*----------------------------------------------------------------------
|   NPT_TimeStamp::operator-=
+---------------------------------------------------------------------*/
NPT_TimeStamp&
NPT_TimeStamp::operator-=(const NPT_TimeStamp& t)
{
    m_NanoSeconds -= t.m_NanoSeconds;
    return *this;
}

/*----------------------------------------------------------------------
|   MatchString
+---------------------------------------------------------------------*/
static int
MatchString(const char* string, const char* const* list, unsigned int list_length)
{
    for (unsigned int i=0; i<list_length; i++) {
        if (NPT_StringsEqual(string, list[i])) return i;
    }
    
    return -1;
}

/*----------------------------------------------------------------------
|   ElapsedLeapYearsSince1900
+---------------------------------------------------------------------*/
static NPT_UInt32
ElapsedLeapYearsSince1900(NPT_UInt32 year)
{
    if (year < 1901) return 0;
    NPT_UInt32 years_since_1900 = year-1-1900; // not including the current year
    return  years_since_1900/4   - 
            years_since_1900/100 +
           (years_since_1900+300)/400;
}

/*----------------------------------------------------------------------
|   ElapsedDaysSince1900
+---------------------------------------------------------------------*/
static NPT_UInt32
ElapsedDaysSince1900(const NPT_DateTime& date)
{
    // compute the number of days elapsed in the year
    NPT_UInt32 day_count = NPT_TIME_ELAPSED_DAYS_AT_MONTH[date.m_Month-1] + date.m_Day - 1;

    // adjust for leap years after february
    if (NPT_TIME_YEAR_IS_LEAP(date.m_Year) && (date.m_Month > 2)) ++day_count;
    
    // compute the total number of elapsed days
    NPT_UInt32 leap_year_count = ElapsedLeapYearsSince1900(date.m_Year);
    day_count += (date.m_Year-1900)*365 + leap_year_count;
    
    return day_count;
}

/*----------------------------------------------------------------------
|   NPT_DateTime::NPT_DateTime
+---------------------------------------------------------------------*/
NPT_DateTime::NPT_DateTime() :
    m_Year(1970),
    m_Month(1),
    m_Day(1),
    m_Hours(0),
    m_Minutes(0),
    m_Seconds(0),
    m_NanoSeconds(0),
    m_TimeZone(0)
{
}

/*----------------------------------------------------------------------
|   NPT_DateTime::NPT_DateTime
+---------------------------------------------------------------------*/
NPT_DateTime::NPT_DateTime(const NPT_TimeStamp& timestamp, bool local)
{
    FromTimeStamp(timestamp, local);
}

/*----------------------------------------------------------------------
|   NPT_DateTime::ChangeTimeZone
+---------------------------------------------------------------------*/
NPT_Result
NPT_DateTime::ChangeTimeZone(NPT_Int32 timezone)
{
    if (timezone < -12*60 || timezone > 12*60) {
        return NPT_ERROR_OUT_OF_RANGE;
    }
    NPT_TimeStamp ts;
    NPT_Result result = ToTimeStamp(ts);
    if (NPT_FAILED(result)) return result;
    ts.SetNanos(ts.ToNanos()+(NPT_Int64)timezone*(NPT_Int64)60*(NPT_Int64)1000000000);

    result = FromTimeStamp(ts);
    m_TimeZone = timezone;
    return result;
}

/*----------------------------------------------------------------------
|   NPT_DateTime::FromTimeStamp
+---------------------------------------------------------------------*/
NPT_Result
NPT_DateTime::FromTimeStamp(const NPT_TimeStamp& ts, bool local)
{
    // number of seconds from the epoch (positive or negative)
    NPT_Int64 seconds = ts.ToSeconds();
    
    // check the range (we only allow up to 31 bits of negative range for seconds
    // in order to have the same lower bound as the 32-bit gmtime() function)
    if (seconds < 0 && (NPT_Int32)seconds != seconds) return NPT_ERROR_OUT_OF_RANGE;
    
    // adjust for the timezone if necessary
    NPT_Int32 timezone = 0;
    if (local) {
        timezone = GetLocalTimeZone();
        seconds += timezone*60;
    }
    
    // adjust to the number of seconds since 1900
    seconds += (NPT_Int64)NPT_SECONDS_PER_YEAR*70 + 
               (NPT_Int64)(17*NPT_SECONDS_PER_DAY); // 17 leap year between 1900 and 1970
        
    // compute the years since 1900, not adjusting for leap years
    NPT_UInt32 years_since_1900 = (NPT_UInt32)(seconds/NPT_SECONDS_PER_YEAR);
    
    // compute the number of seconds elapsed in the current year
    seconds -= (NPT_Int64)years_since_1900 * NPT_SECONDS_PER_YEAR;

    // adjust for leap years
    bool is_leap_year = false;
    NPT_UInt32 leap_years_since_1900 = ElapsedLeapYearsSince1900(years_since_1900+1900);
    if (seconds < (leap_years_since_1900 * NPT_SECONDS_PER_DAY)) {
        // not enough seconds in the current year to compensate, move one year back
        seconds += NPT_SECONDS_PER_YEAR;
        seconds -= leap_years_since_1900 * NPT_SECONDS_PER_DAY;
        --years_since_1900;
        if (NPT_TIME_YEAR_IS_LEAP(years_since_1900+1900) ) {
            seconds += NPT_SECONDS_PER_DAY;
            is_leap_year = true;
        }
    } else {
        seconds -= leap_years_since_1900 * NPT_SECONDS_PER_DAY;
        if (NPT_TIME_YEAR_IS_LEAP(years_since_1900+1900) ) {
            is_leap_year = true;
        }
    }

    // now we know the year
    m_Year = years_since_1900+1900;

    // compute the number of days since January 1 (0 - 365)
    NPT_UInt32 day_of_the_year = (NPT_UInt32)(seconds/NPT_SECONDS_PER_DAY);

    // compute the number of seconds in the current day
    seconds -= day_of_the_year * NPT_SECONDS_PER_DAY;

    // compute the number of months since January (0 - 11) and the day of month (1 - 31) */
    const NPT_Int32* month_day = is_leap_year?NPT_TIME_MONTH_DAY_LEAP:NPT_TIME_MONTH_DAY;
    NPT_UInt32 month;
    for (month = 1; month_day[month] < (NPT_Int32)day_of_the_year ; month++) {}

    // now we know the month and day
    m_Month = month;
    m_Day   = day_of_the_year - month_day[month-1];

    // compute the number of hours since midnight (0 - 23), minutes after the hour
    // (0 - 59), seconds after the minute (0 - 59) and nanoseconds
    m_Hours   = (NPT_Int32)seconds/3600;
    seconds  -= m_Hours * 3600L;
    m_Minutes = (NPT_Int32)seconds / 60;
    m_Seconds = (NPT_Int32)seconds - m_Minutes * 60;
    m_NanoSeconds = (NPT_Int32)(ts.ToNanos()%1000000000);
    if (local) {
        m_TimeZone = timezone;
    } else {
        m_TimeZone = 0;
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   CheckDate
+---------------------------------------------------------------------*/
static NPT_Result
CheckDate(const NPT_DateTime& date)
{
    NPT_TIME_CHECK_BOUNDS(date.m_Year, NPT_DATETIME_YEAR_MIN, NPT_DATETIME_YEAR_MAX);
    NPT_TIME_CHECK_BOUNDS(date.m_Month,       1, 12);
    NPT_TIME_CHECK_BOUNDS(date.m_Day,         1, 31);
    NPT_TIME_CHECK_BOUNDS(date.m_Hours,       0, 23);
    NPT_TIME_CHECK_BOUNDS(date.m_Minutes,     0, 59);
    NPT_TIME_CHECK_BOUNDS(date.m_Seconds,     0, 59);
    NPT_TIME_CHECK_BOUNDS(date.m_NanoSeconds, 0, 999999999);
    NPT_TIME_CHECK_BOUNDS(date.m_TimeZone,   -12*60, 12*60);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_DateTime::ToTimeStamp
+---------------------------------------------------------------------*/
NPT_Result
NPT_DateTime::ToTimeStamp(NPT_TimeStamp& timestamp) const
{
    // default value
    timestamp.SetNanos(0);
    
    // check bounds
    NPT_Result result = CheckDate(*this);
    if (NPT_FAILED(result)) return result;

    // compute the number of days elapsed since 1900
    NPT_UInt32 days = ElapsedDaysSince1900(*this);

    // compute the number of nanoseconds
    NPT_Int64 seconds = (NPT_Int64)days      * (24*60*60) + 
                        (NPT_Int64)m_Hours   * (60*60) +
                        (NPT_Int64)m_Minutes * (60) + 
                        (NPT_Int64)m_Seconds;
    seconds -= (NPT_Int64)m_TimeZone*60;

    // adjust to the number of seconds since 1900
    seconds -= (NPT_Int64)NPT_SECONDS_PER_YEAR*70 + 
        (NPT_Int64)(17*NPT_SECONDS_PER_DAY); // 17 leap year between 1900 and 1970

    timestamp.FromNanos(seconds * 1000000000 + m_NanoSeconds);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   AppendNumber
+---------------------------------------------------------------------*/
static void
AppendNumber(NPT_String& output, NPT_UInt32 number, unsigned int digit_count)
{
    NPT_Size new_length = output.GetLength()+digit_count;
    output.SetLength(new_length);
    char* dest = output.UseChars()+new_length;
    while (digit_count--) {
        *--dest = '0'+(number%10);
        number /= 10;
    }
}

/*----------------------------------------------------------------------
|   NPT_DateTime::ToString
+---------------------------------------------------------------------*/
NPT_String
NPT_DateTime::ToString(Format format, NPT_Flags flags) const
{
    NPT_String result;
    
    if (NPT_FAILED(CheckDate(*this))) return result;
    
    switch (format) {
        case FORMAT_W3C:
            AppendNumber(result, m_Year, 4);
            result += '-';
            AppendNumber(result, m_Month, 2);
            result += '-';
            AppendNumber(result, m_Day, 2);
            result += 'T';
            AppendNumber(result, m_Hours, 2);
            result += ':';
            AppendNumber(result, m_Minutes, 2);
            result += ':';
            AppendNumber(result, m_Seconds, 2);
            if (flags & FLAG_EMIT_FRACTION) {
                result += '.';
                if (flags & FLAG_EXTENDED_PRECISION) {
                    // nanoseconds precision
                    AppendNumber(result, m_NanoSeconds, 9);
                } else {
                    // only miliseconds precision
                    AppendNumber(result, m_NanoSeconds/1000000, 3);
                }
            }
            if (m_TimeZone) {
                NPT_UInt32 tz;
                if (m_TimeZone > 0) {
                    result += '+';
                    tz = m_TimeZone;
                } else {
                    result += '-';
                    tz = -m_TimeZone;
                }
                AppendNumber(result, tz/60, 2);
                result += ':';
                AppendNumber(result, tz%60, 2);
            } else {
                result += 'Z';
            }
            break;
            
        case FORMAT_ANSI: {
            // compute the number of days elapsed since 1900
            NPT_UInt32 days = ElapsedDaysSince1900(*this);
            
            // format the result
            result.SetLength(24);
            NPT_FormatString(result.UseChars(), result.GetLength()+1, 
                             "%.3s %.3s%3d %.2d:%.2d:%.2d %d",
                             NPT_TIME_DAYS_SHORT[(days+1)%7],
                             NPT_TIME_MONTHS[m_Month-1],
                             m_Day,
                             m_Hours,
                             m_Minutes,
                             m_Seconds,
                             m_Year);
            break;
        }
            
        case FORMAT_RFC_1036:
        case FORMAT_RFC_1123: {
            // compute the number of days elapsed since 1900
            NPT_UInt32 days = ElapsedDaysSince1900(*this);

            if (format == FORMAT_RFC_1036) {
                result += NPT_TIME_DAYS_LONG[(days+1)%7];
                result += ", ";
                AppendNumber(result, m_Day, 2);
                result += '-';
                result += NPT_TIME_MONTHS[m_Month-1];
                result += '-';
                AppendNumber(result, m_Year%100, 2);
            } else {
                result += NPT_TIME_DAYS_SHORT[(days+1)%7];
                result += ", ";
                AppendNumber(result, m_Day, 2);
                result += ' ';
                result += NPT_TIME_MONTHS[m_Month-1];
                result += ' ';
                AppendNumber(result, m_Year, 4);
            }
            result += ' ';
            AppendNumber(result, m_Hours, 2);
            result += ':';
            AppendNumber(result, m_Minutes, 2);
            result += ':';
            AppendNumber(result, m_Seconds, 2);
            if (m_TimeZone) {
                if (m_TimeZone > 0) {
                    result += " +";
                    AppendNumber(result, m_TimeZone/60, 2);
                    AppendNumber(result, m_TimeZone%60, 2);
                } else {
                    result += " -";
                    AppendNumber(result, -m_TimeZone/60, 2);
                    AppendNumber(result, -m_TimeZone%60, 2);
                }
            } else {
                result += " GMT";
            }
            break;
        }
    }

    return result;
}

/*----------------------------------------------------------------------
|   NPT_DateTime::FromString
+--------------------------------------------------------------------*/
NPT_Result
NPT_DateTime::FromString(const char* date, Format format)
{
    if (date == NULL || date[0] == '\0') return NPT_ERROR_INVALID_PARAMETERS;
    
    // create a local copy to work with
    NPT_String workspace(date);
    char* input = workspace.UseChars();
    NPT_Size input_size = workspace.GetLength();
    
    switch (format) {
      case FORMAT_W3C: {
        if (input_size < 17 && input_size != 10) return NPT_ERROR_INVALID_SYNTAX;

        // check separators
        if (input[4] != '-' || 
            input[7] != '-') {
            return NPT_ERROR_INVALID_SYNTAX;
        }
         
        // replace separators with terminators
        input[4] = input[7] = '\0';
        
        bool no_seconds = true;
        if (input_size > 10) {
            if (input[10] != 'T' || 
                input[13] != ':') {
                return NPT_ERROR_INVALID_SYNTAX;
            }
           input[10] = input[13] = '\0';
            if (input[16] == ':') {
                input[16] = '\0';
                no_seconds = false;
                if (input_size < 20) return NPT_ERROR_INVALID_SYNTAX;
            } else {
                m_Seconds = 0;
            }
        }
          
    
        // parse CCYY-MM-DD fields
        if (NPT_FAILED(NPT_ParseInteger(input,    m_Year,    false)) ||
            NPT_FAILED(NPT_ParseInteger(input+5,  m_Month,   false)) ||
            NPT_FAILED(NPT_ParseInteger(input+8,  m_Day,     false))) {
            return NPT_ERROR_INVALID_SYNTAX;
        }

        // parse remaining fields if any
        if (input_size > 10) {
            // parse the timezone part
            if (input[input_size-1] == 'Z') {
                m_TimeZone = 0;
                input[input_size-1] = '\0';
            } else if (input[input_size-6] == '+' || input[input_size-6] == '-') {
                if (input[input_size-3] != ':') return NPT_ERROR_INVALID_SYNTAX;
                input[input_size-3] = '\0';
                unsigned int hh, mm;
                if (NPT_FAILED(NPT_ParseInteger(input+input_size-5, hh, false)) ||
                    NPT_FAILED(NPT_ParseInteger(input+input_size-2, mm, false))) {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                if (hh > 59 || mm > 59) return NPT_ERROR_INVALID_SYNTAX;
                m_TimeZone = hh*60+mm;
                if (input[input_size-6] == '-') m_TimeZone = -m_TimeZone;
                input[input_size-6] = '\0';
            }
            
            // parse fields
            if (NPT_FAILED(NPT_ParseInteger(input+11, m_Hours,   false)) ||
                NPT_FAILED(NPT_ParseInteger(input+14, m_Minutes, false))) {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            if (!no_seconds && input[19] == '.') {
                char fraction[10];
                fraction[9] = '\0';
                unsigned int fraction_size = NPT_StringLength(input+20);
                if (fraction_size == 0) return NPT_ERROR_INVALID_SYNTAX;
                for (unsigned int i=0; i<9; i++) {
                    if (i < fraction_size) {
                        fraction[i] = input[20+i];
                    } else {
                        fraction[i] = '0';
                    }
                }
                if (NPT_FAILED(NPT_ParseInteger(fraction, m_NanoSeconds, false))) {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                input[19] = '\0';
            } else {
                m_NanoSeconds = 0;
            }
            if (!no_seconds) {
                if (NPT_FAILED(NPT_ParseInteger(input+17, m_Seconds, false))) {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
            }
        }
        break;
      }
    
      case FORMAT_RFC_1036: 
      case FORMAT_RFC_1123: {
        if (input_size < 26) return NPT_ERROR_INVALID_SYNTAX;
        // look for the weekday and separtor
        const char* wday = input;
        while (*input && *input != ',') {
            ++input;
            --input_size;
        }
        if (*input == '\0' || *wday == ',') return NPT_ERROR_INVALID_SYNTAX;
        *input++ = '\0';
        --input_size;
        
        // look for the timezone
        char* timezone = input+input_size-1;
        unsigned int timezone_size = 0;
        while (input_size && *timezone != ' ') {
            --timezone;
            ++timezone_size;
            --input_size;
        }
        if (input_size == 0) return NPT_ERROR_INVALID_SYNTAX;
        *timezone++ = '\0';
        
        // check separators
        if (input_size < 20) return NPT_ERROR_INVALID_SYNTAX;
        unsigned int yl = input_size-18;
        if (yl != 2 && yl != 4) return NPT_ERROR_INVALID_SYNTAX;
        char sep;
        int wday_index;
        if (format == FORMAT_RFC_1036) {
            sep = '-';
            wday_index = MatchString(wday, NPT_TIME_DAYS_LONG, 7);
        } else {
            sep = ' ';
            wday_index = MatchString(wday, NPT_TIME_DAYS_SHORT, 7);
        }
        if (input[0]     != ' ' || 
            input[3]     != sep || 
            input[7]     != sep ||
            input[8+yl]  != ' ' ||
            input[11+yl] != ':' ||
            input[14+yl] != ':') {
            return NPT_ERROR_INVALID_SYNTAX;
        }
        input[3] = input[7] = input[8+yl] = input[11+yl] = input[14+yl] = '\0';            

        // parse fields
        m_Month = 1+MatchString(input+4, NPT_TIME_MONTHS, 12);
        if (NPT_FAILED(NPT_ParseInteger(input+1,     m_Day,     false)) ||
            NPT_FAILED(NPT_ParseInteger(input+8,     m_Year,    false)) ||
            NPT_FAILED(NPT_ParseInteger(input+9+yl,  m_Hours,   false)) ||
            NPT_FAILED(NPT_ParseInteger(input+12+yl, m_Minutes, false)) ||
            NPT_FAILED(NPT_ParseInteger(input+15+yl, m_Seconds, false))) {
            return NPT_ERROR_INVALID_SYNTAX;
        }
        
        // adjust short year lengths
        if (yl == 2) m_Year += 1900;
        
        // parse the timezone
        if (NPT_StringsEqual(timezone, "GMT") ||
            NPT_StringsEqual(timezone, "UT")  ||
            NPT_StringsEqual(timezone, "Z")) {
            m_TimeZone = 0;
        } else if (NPT_StringsEqual(timezone, "EDT")) {
            m_TimeZone = -4*60;
        } else if (NPT_StringsEqual(timezone, "EST") ||
                   NPT_StringsEqual(timezone, "CDT")) {
            m_TimeZone = -5*60;
        } else if (NPT_StringsEqual(timezone, "CST") ||
                   NPT_StringsEqual(timezone, "MDT")) {
            m_TimeZone = -6*60;
        } else if (NPT_StringsEqual(timezone, "MST") ||
                   NPT_StringsEqual(timezone, "PDT")) {
            m_TimeZone = -7*60;
        } else if (NPT_StringsEqual(timezone, "PST")) {
            m_TimeZone = -8*60;
        } else if (timezone_size == 1) {
            if (timezone[0] >= 'A' && timezone[0] <= 'I') {
                m_TimeZone = -60*(1+timezone[0]-'A');
            } else if (timezone[0] >= 'K' && timezone[0] <= 'M') {
                m_TimeZone = -60*(timezone[0]-'A');            
            } else if (timezone[0] >= 'N' && timezone[0] <= 'Y') {
                m_TimeZone = 60*(1+timezone[0]-'N');
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
        } else if (timezone_size == 5) {
            int sign;
            if (timezone[0] == '-') {
                sign = -1;
            } else if (timezone[0] == '+') {
                sign = 1;
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            NPT_UInt32 tz;
            if (NPT_FAILED(NPT_ParseInteger(timezone+1, tz, false))) {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            unsigned int hh = (tz/100);
            unsigned int mm = (tz%100);
            if (hh > 59 || mm > 59) return NPT_ERROR_INVALID_SYNTAX;
            m_TimeZone = sign*(hh*60+mm);
        } else {
            return NPT_ERROR_INVALID_SYNTAX;
        }
        
        // compute the number of days elapsed since 1900
        NPT_UInt32 days = ElapsedDaysSince1900(*this);
        if ((int)((days+1)%7) != wday_index) {
            return NPT_ERROR_INVALID_PARAMETERS;
        }
        
        m_NanoSeconds = 0;

        break;
      }
    
      case FORMAT_ANSI: {
        if (input_size != 24) return NPT_ERROR_INVALID_SYNTAX;

        // check separators
        if (input[3]  != ' ' || 
            input[7]  != ' ' || 
            input[10] != ' ' || 
            input[13] != ':' || 
            input[16] != ':' ||
            input[19] != ' ') {
            return NPT_ERROR_INVALID_SYNTAX;
        }
        input[3] = input[7] = input[10] = input[13] = input[16] = input[19] = '\0';
        if (input[8] == ' ') input[8] = '0';
                
        m_Month = 1+MatchString(input+4, NPT_TIME_MONTHS, 12);
        if (NPT_FAILED(NPT_ParseInteger(input+8,  m_Day,     false)) ||
            NPT_FAILED(NPT_ParseInteger(input+11, m_Hours,   false)) ||
            NPT_FAILED(NPT_ParseInteger(input+14, m_Minutes, false)) ||
            NPT_FAILED(NPT_ParseInteger(input+17, m_Seconds, false)) ||
            NPT_FAILED(NPT_ParseInteger(input+20, m_Year,    false))) {
            return NPT_ERROR_INVALID_SYNTAX;
        }

        // compute the number of days elapsed since 1900
        NPT_UInt32 days = ElapsedDaysSince1900(*this);
        if ((int)((days+1)%7) != MatchString(input, NPT_TIME_DAYS_SHORT, 7)) {
            return NPT_ERROR_INVALID_PARAMETERS;
        }
        
        m_TimeZone    = 0;
        m_NanoSeconds = 0;
        break;
      }
    
      default:
        return NPT_ERROR_INVALID_PARAMETERS;
    }
    
    return CheckDate(*this);
}
