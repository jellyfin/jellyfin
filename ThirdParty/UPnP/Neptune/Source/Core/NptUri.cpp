/*****************************************************************
|
|   Neptune - URI
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
 ***************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptUri.h"
#include "NptUtils.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|   NPT_Uri::ParseScheme
+---------------------------------------------------------------------*/
NPT_Uri::SchemeId
NPT_Uri::ParseScheme(const NPT_String& scheme)
{
    if (scheme == "http") {
        return SCHEME_ID_HTTP;
    } else if (scheme == "https") {
        return SCHEME_ID_HTTPS;
    } else {
        return SCHEME_ID_UNKNOWN;
    }
}

/*----------------------------------------------------------------------
|   NPT_Uri::SetScheme
+---------------------------------------------------------------------*/
void
NPT_Uri::SetScheme(const char* scheme)
{
    m_Scheme = scheme;
    m_Scheme.MakeLowercase();
    m_SchemeId = ParseScheme(m_Scheme);
}

/*----------------------------------------------------------------------
|   NPT_Uri::SetSchemeFromUri
+---------------------------------------------------------------------*/
NPT_Result
NPT_Uri::SetSchemeFromUri(const char* uri)
{
    const char* start = uri;
    char c;
    while ((c =*uri++)) {
        if (c == ':') {
            m_Scheme.Assign(start, (NPT_Size)(uri-start-1));
            m_Scheme.MakeLowercase();
            m_SchemeId = ParseScheme(m_Scheme);
            return NPT_SUCCESS;
        } else if ((c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   (c >= '0' && c <= '9') ||
                   (c == '+')             ||
                   (c == '.')             ||
                   (c == '-')) {
            continue;
        } else {
            break;
        }
    }
    return NPT_ERROR_INVALID_SYNTAX;
}

/*----------------------------------------------------------------------
Appendix A.  Collected ABNF for URI

   URI           = scheme ":" hier-part [ "?" query ] [ "#" fragment ]

   hier-part     = "//" authority path-abempty
                 / path-absolute
                 / path-rootless
                 / path-empty

   URI-reference = URI / relative-ref

   absolute-URI  = scheme ":" hier-part [ "?" query ]

   relative-ref  = relative-part [ "?" query ] [ "#" fragment ]

   relative-part = "//" authority path-abempty
                 / path-absolute
                 / path-noscheme
                 / path-empty

   scheme        = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." )

   authority     = [ userinfo "@" ] host [ ":" port ]
   userinfo      = *( unreserved / pct-encoded / sub-delims / ":" )
   host          = IP-literal / IPv4address / reg-name
   port          = *DIGIT

   IP-literal    = "[" ( IPv6address / IPvFuture  ) "]"

   IPvFuture     = "v" 1*HEXDIG "." 1*( unreserved / sub-delims / ":" )

   IPv6address   =                            6( h16 ":" ) ls32
                 /                       "::" 5( h16 ":" ) ls32
                 / [               h16 ] "::" 4( h16 ":" ) ls32
                 / [ *1( h16 ":" ) h16 ] "::" 3( h16 ":" ) ls32
                 / [ *2( h16 ":" ) h16 ] "::" 2( h16 ":" ) ls32
                 / [ *3( h16 ":" ) h16 ] "::"    h16 ":"   ls32
                 / [ *4( h16 ":" ) h16 ] "::"              ls32
                 / [ *5( h16 ":" ) h16 ] "::"              h16
                 / [ *6( h16 ":" ) h16 ] "::"

   h16           = 1*4HEXDIG
   ls32          = ( h16 ":" h16 ) / IPv4address
   IPv4address   = dec-octet "." dec-octet "." dec-octet "." dec-octet
   dec-octet     = DIGIT                 ; 0-9
                 / %x31-39 DIGIT         ; 10-99
                 / "1" 2DIGIT            ; 100-199
                 / "2" %x30-34 DIGIT     ; 200-249
                 / "25" %x30-35          ; 250-255

   reg-name      = *( unreserved / pct-encoded / sub-delims )

   path          = path-abempty    ; begins with "/" or is empty
                 / path-absolute   ; begins with "/" but not "//"
                 / path-noscheme   ; begins with a non-colon segment
                 / path-rootless   ; begins with a segment
                 / path-empty      ; zero characters

   path-abempty  = *( "/" segment )
   path-absolute = "/" [ segment-nz *( "/" segment ) ]
   path-noscheme = segment-nz-nc *( "/" segment )
   path-rootless = segment-nz *( "/" segment )
   path-empty    = 0<pchar>

   segment       = *pchar
   segment-nz    = 1*pchar
   segment-nz-nc = 1*( unreserved / pct-encoded / sub-delims / "@" )
                 ; non-zero-length segment without any colon ":"

   pchar         = unreserved / pct-encoded / sub-delims / ":" / "@"

   query         = *( pchar / "/" / "?" )

   fragment      = *( pchar / "/" / "?" )

   pct-encoded   = "%" HEXDIG HEXDIG

   unreserved    = ALPHA / DIGIT / "-" / "." / "_" / "~"
   reserved      = gen-delims / sub-delims
   gen-delims    = ":" / "/" / "?" / "#" / "[" / "]" / "@"
   sub-delims    = "!" / "$" / "&" / "'" / "(" / ")"
                 / "*" / "+" / "," / ";" / "="

---------------------------------------------------------------------*/
                 
#define NPT_URI_ALWAYS_ENCODE " !\"<>\\^`{|}"

/*----------------------------------------------------------------------
|   NPT_Uri::PathCharsToEncode
+---------------------------------------------------------------------*/
const char* const
NPT_Uri::PathCharsToEncode = NPT_URI_ALWAYS_ENCODE "?#[]";
//NPT_Uri::PathCharsToEncode = NPT_URI_ALWAYS_ENCODE "?#[]:/";

/*----------------------------------------------------------------------
|   NPT_Uri::QueryCharsToEncode
+---------------------------------------------------------------------*/
const char* const
NPT_Uri::QueryCharsToEncode = NPT_URI_ALWAYS_ENCODE "#[]";

/*----------------------------------------------------------------------
|   NPT_Uri::FragmentCharsToEncode
+---------------------------------------------------------------------*/
const char* const
NPT_Uri::FragmentCharsToEncode = NPT_URI_ALWAYS_ENCODE "[]";

/*----------------------------------------------------------------------
|   NPT_Uri::UnsafeCharsToEncode
+---------------------------------------------------------------------*/
const char* const
NPT_Uri::UnsafeCharsToEncode = NPT_URI_ALWAYS_ENCODE; // and ' ?

/*----------------------------------------------------------------------
|   NPT_Uri::PercentEncode
+---------------------------------------------------------------------*/
NPT_String
NPT_Uri::PercentEncode(const char* str, const char* chars, bool encode_percents)
{
    NPT_String encoded;

    // check args
    if (str == NULL) return encoded;

    // reserve at least the size of the current uri
    encoded.Reserve(NPT_StringLength(str));

    // process each character
    char escaped[3];
    escaped[0] = '%';
    while (unsigned char c = *str++) {
        bool encode = false;
        if (encode_percents && c == '%') {
            encode = true;
        } else if (c < ' ' || c > '~') {
            encode = true;
        } else {
            const char* match = chars;
            while (*match) {
                if (c == *match) {
                    encode = true;
                    break;
                }
                ++match;
            }
        }
        if (encode) {
            // encode
            NPT_ByteToHex(c, &escaped[1], true);
            encoded.Append(escaped, 3);
        } else {
            // no encoding required
            encoded += c;
        }
    }
    
    return encoded;
}

/*----------------------------------------------------------------------
|   NPT_Uri::PercentDecode
+---------------------------------------------------------------------*/
NPT_String
NPT_Uri::PercentDecode(const char* str)
{
    NPT_String decoded;

    // check args
    if (str == NULL) return decoded;

    // reserve at least the size of the current uri
    decoded.Reserve(NPT_StringLength(str));

    // process each character
    while (unsigned char c = *str++) {
        if (c == '%') {
            // needs to be unescaped
            unsigned char unescaped;
            if (NPT_SUCCEEDED(NPT_HexToByte(str, unescaped))) {
                decoded += unescaped;
                str += 2;
            } else {
                // not a valid escape sequence, just keep the %
                decoded += c;
            }
        } else {
            // no unescaping required
            decoded += c;
        }
    }

    return decoded;
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::NPT_UrlQuery
+---------------------------------------------------------------------*/ 
NPT_UrlQuery::NPT_UrlQuery(const char* query)
{
    Parse(query);
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::UrlEncode
+---------------------------------------------------------------------*/ 
NPT_String
NPT_UrlQuery::UrlEncode(const char* str, bool encode_percents)
{
    NPT_String encoded = NPT_Uri::PercentEncode(
        str, 
        ";/?:@&=+$,"    /* reserved as defined in RFC 2396 */
        "\"#<>\\^`{|}", /* other unsafe chars              */
        encode_percents);
    encoded.Replace(' ','+');
    
    return encoded;
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::UrlDecode
+---------------------------------------------------------------------*/
NPT_String
NPT_UrlQuery::UrlDecode(const char* str)
{
    NPT_String decoded = NPT_Uri::PercentDecode(str);
    decoded.Replace('+', ' ');
    return decoded;
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::Field::Field
+---------------------------------------------------------------------*/
NPT_UrlQuery::Field::Field(const char* name, const char* value, bool encoded) 
{
    if (encoded) {
        m_Name = name;
        m_Value = value;
    } else {
        m_Name = UrlEncode(name);
        m_Value = UrlEncode(value);
    }
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::ToString
+---------------------------------------------------------------------*/
NPT_String 
NPT_UrlQuery::ToString()
{
    NPT_String encoded;
    bool       separator = false;
    for (NPT_List<Field>::Iterator it = m_Fields.GetFirstItem();
         it;
         ++it) {
         Field& field = *it;
         if (separator) encoded += "&";
         separator = true;
         encoded += field.m_Name;
         encoded += "=";
         encoded += field.m_Value;
    }

    return encoded;
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::Parse
+---------------------------------------------------------------------*/
NPT_Result 
NPT_UrlQuery::Parse(const char* query)
{
    const char* cursor = query;
    NPT_String  name;
    NPT_String  value;
    bool        in_name = true;
    do {
        if (*cursor == '\0' || *cursor == '&') {
            if (!name.IsEmpty()) {
                AddField(name, value, true);   
            }
            name.SetLength(0);
            value.SetLength(0);
            in_name = true;
        } else if (*cursor == '=' && in_name) {
            in_name = false;
        } else {
            if (in_name) {
                name += *cursor;
            } else {
                value += *cursor;
            }
        }
    } while (*cursor++);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::AddField
+---------------------------------------------------------------------*/
NPT_Result 
NPT_UrlQuery::AddField(const char* name, const char* value, bool encoded)
{
    return m_Fields.Add(Field(name, value, encoded));
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::SetField
+---------------------------------------------------------------------*/
NPT_Result
NPT_UrlQuery::SetField(const char* name, const char* value, bool encoded)
{
    NPT_String ename;
    if (encoded) {
        ename = name;
    } else {
        ename = UrlEncode(name);
    }
    for (NPT_List<Field>::Iterator it = m_Fields.GetFirstItem();
         it;
         ++it) {
         Field& field = *it;
         if (field.m_Name == ename) {
            if (encoded) {
                field.m_Value = value;
            } else {
                field.m_Value = UrlEncode(value);
            }
            return NPT_SUCCESS;
        }
    }

    // field not found, add it
    return AddField(name, value, encoded);
}

/*----------------------------------------------------------------------
|   NPT_UrlQuery::GetField
+---------------------------------------------------------------------*/
const char* 
NPT_UrlQuery::GetField(const char* name)
{
    NPT_String ename = UrlEncode(name);
    for (NPT_List<Field>::Iterator it = m_Fields.GetFirstItem();
         it;
         ++it) {
         Field& field = *it;
         if (field.m_Name == ename) return field.m_Value;
    }

    // field not found
    return NULL;
}

/*----------------------------------------------------------------------
|   types
+---------------------------------------------------------------------*/
typedef enum {
    NPT_URL_PARSER_STATE_START,
    NPT_URL_PARSER_STATE_SCHEME,
    NPT_URL_PARSER_STATE_LEADING_SLASH,
    NPT_URL_PARSER_STATE_HOST,
    NPT_URL_PARSER_STATE_PORT,
    NPT_URL_PARSER_STATE_PATH,
    NPT_URL_PARSER_STATE_QUERY
} NPT_UrlParserState;

/*----------------------------------------------------------------------
|   NPT_Url::NPT_Url
+---------------------------------------------------------------------*/
NPT_Url::NPT_Url() : 
    m_Port(NPT_URL_INVALID_PORT),
    m_Path("/"),
    m_HasQuery(false),
    m_HasFragment(false)
{
}

/*----------------------------------------------------------------------
|   NPT_Url::NPT_Url
+---------------------------------------------------------------------*/
NPT_Url::NPT_Url(const char* url, NPT_UInt16 default_port) : 
    m_Port(NPT_URL_INVALID_PORT),
    m_HasQuery(false),
    m_HasFragment(false)
{
    // try to parse
    if (NPT_FAILED(Parse(url, default_port))) {
        Reset();
    }
}

/*----------------------------------------------------------------------
|   NPT_Url::Parse
+---------------------------------------------------------------------*/
NPT_Result
NPT_Url::Parse(const char* url, NPT_UInt16 default_port)
{
    // check parameters
    if (url == NULL) return NPT_ERROR_INVALID_PARAMETERS;

    // set the uri scheme
    NPT_Result result = SetSchemeFromUri(url);
    if (NPT_FAILED(result)) return result;
    
    // set the default port
    if (default_port) {
        m_Port = default_port;
    } else {
        switch (m_SchemeId) {
            case SCHEME_ID_HTTP:  m_Port = NPT_URL_DEFAULT_HTTP_PORT;  break;
            case SCHEME_ID_HTTPS: m_Port = NPT_URL_DEFAULT_HTTPS_PORT; break;
            default:                                                   break;
        }
    }
    
    // move to the scheme-specific part
    url += m_Scheme.GetLength()+1;

    // intialize the parser
    NPT_UrlParserState state = NPT_URL_PARSER_STATE_START;
    const char* mark = url;

    // parse the URL
    char c;
    do  {
        c = *url++;
        switch (state) {
          case NPT_URL_PARSER_STATE_START:
            if (c == '/') {
                state = NPT_URL_PARSER_STATE_LEADING_SLASH;
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case NPT_URL_PARSER_STATE_LEADING_SLASH:
            if (c == '/') {
                state = NPT_URL_PARSER_STATE_HOST;
                mark = url;
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case NPT_URL_PARSER_STATE_HOST:
            if (c == ':' || c == '/' || c == '\0' || c == '?' || c == '#') {
                m_Host.Assign(mark, (NPT_Size)(url-1-mark));
                if (c == ':') {
                    mark = url;
                    m_Port = 0;
                    state = NPT_URL_PARSER_STATE_PORT;
                } else {
                    mark = url-1;
                    state = NPT_URL_PARSER_STATE_PATH;
                }
            }
            break;

          case NPT_URL_PARSER_STATE_PORT:
            if (c >= '0' && c <= '9') {
                unsigned int val = m_Port*10+(c-'0');
                if (val > 65535) {
                    m_Port = NPT_URL_INVALID_PORT;
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                m_Port = val;
            } else if (c == '/' || c == '\0') {
                mark = url-1;
                state = NPT_URL_PARSER_STATE_PATH;
            } else {
                // invalid character
                m_Port = NPT_URL_INVALID_PORT;
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case NPT_URL_PARSER_STATE_PATH:
            if (*mark) {
                return ParsePathPlus(mark);
            }
            break;

          default:
            break;
        }
    } while (c);

    // if we get here, the path is implicit
    m_Path = "/";
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Url::NPT_Url
+---------------------------------------------------------------------*/
NPT_Url::NPT_Url(const char* scheme,
                 const char* host, 
                 NPT_UInt16  port, 
                 const char* path,
                 const char* query,
                 const char* fragment) :
    m_Host(host),
    m_Port(port),
    m_Path(path),
    m_HasQuery(query != NULL),
    m_Query(query),
    m_HasFragment(fragment != NULL),
    m_Fragment(fragment)
{
    SetScheme(scheme);
}    

/*----------------------------------------------------------------------
|   NPT_Url::Reset
+---------------------------------------------------------------------*/
void
NPT_Url::Reset()
{
    m_Host.SetLength(0);
    m_Port = 0;
    m_Path.SetLength(0);
    m_HasQuery = false;
    m_Query.SetLength(0);
    m_HasFragment = false;
    m_Fragment.SetLength(0);
}

/*----------------------------------------------------------------------
|   NPT_Url::IsValid
+---------------------------------------------------------------------*/
bool
NPT_Url::IsValid() const
{
    switch (m_SchemeId) {
        case SCHEME_ID_HTTP:
        case SCHEME_ID_HTTPS:
            return m_Port != NPT_URL_INVALID_PORT && !m_Host.IsEmpty();
            break;
            
        default:
            return !m_Scheme.IsEmpty();
    }
}

/*----------------------------------------------------------------------
|   NPT_Url::SetHost
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Url::SetHost(const char* host)
{
    const char* port = host;
    while (*port && *port != ':') port++;
    if (*port) {
        m_Host.Assign(host, (NPT_Size)(port-host));
        unsigned int port_number;
        if (NPT_SUCCEEDED(NPT_ParseInteger(port+1, port_number, false))) {
            m_Port = (short)port_number;
        }
    } else {
        m_Host = host;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Url::SetHost
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Url::SetPort(NPT_UInt16 port)
{
    m_Port = port;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Url::SetPath
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Url::SetPath(const char* path, bool encoded)
{
    if (encoded) {
        m_Path = path;
    } else {
        m_Path = PercentEncode(path, PathCharsToEncode);    
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Url::ParsePathPlus
+---------------------------------------------------------------------*/
NPT_Result
NPT_Url::ParsePathPlus(const char* path_plus)
{
    // check parameters
    if (path_plus == NULL) return NPT_ERROR_INVALID_PARAMETERS;

    // reset any existing values
    m_Path.SetLength(0);
    m_Query.SetLength(0);
    m_Fragment.SetLength(0);
    m_HasQuery = false;
    m_HasFragment = false;

#ifdef _WIN32
    // Skip the leading '/' if there is an absolute path starting with
    // a drive letter on Windows.
    if (path_plus[0] == '/' && 
        ((path_plus[1] >= 'a' && path_plus[1] <= 'z') ||
         (path_plus[1] >= 'A' && path_plus[1] <= 'Z')) &&
        path_plus[2] == ':') 
    {
        ++path_plus;
    }
#endif

    // intialize the parser
    NPT_UrlParserState state = NPT_URL_PARSER_STATE_PATH;
    const char* mark = path_plus;

    // parse the path+
    char c;
    do  {
        c = *path_plus++;
        switch (state) {
          case NPT_URL_PARSER_STATE_PATH:
            if (c == '\0' || c == '?' || c == '#') {
                if (path_plus-1 > mark) {
                    m_Path.Append(mark, (NPT_Size)(path_plus-1-mark));
                }
                if (c == '?') {
                    m_HasQuery = true;
                    state = NPT_URL_PARSER_STATE_QUERY;
                    mark = path_plus;
                } else if (c == '#') {
                    m_HasFragment = true;
                    m_Fragment = path_plus;
                    return NPT_SUCCESS;
                }
            }
            break;

          case NPT_URL_PARSER_STATE_QUERY:
            if (c == '\0' || c == '#') {
                m_Query.Assign(mark, (NPT_Size)(path_plus-1-mark));
                if (c == '#') {
                    m_HasFragment = true;
                    m_Fragment = path_plus;
                }
                return NPT_SUCCESS;
            }
            break;

          default: 
            break;
        }
    } while (c);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Url::SetQuery
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Url::SetQuery(const char* query, bool encoded)
{
    if (encoded) {
        m_Query = query;
    } else {
        m_Query = PercentEncode(query, QueryCharsToEncode);
    }
    m_HasQuery = query!=NULL && NPT_StringLength(query)>0;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Url::SetFragment
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Url::SetFragment(const char* fragment, bool encoded)
{
    if (encoded) {
        m_Fragment = fragment;
    } else {
        m_Fragment = PercentEncode(fragment, FragmentCharsToEncode);
    }
    m_HasFragment = fragment!=NULL;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Url::ToRequestString
+---------------------------------------------------------------------*/
NPT_String
NPT_Url::ToRequestString(bool with_fragment) const
{
    NPT_String result;
    NPT_Size   length = m_Path.GetLength()+1;
    if (m_HasQuery)    length += 1+m_Query.GetLength();
    if (with_fragment) length += 1+m_Fragment.GetLength();
    result.Reserve(length);

    if (m_Path.IsEmpty()) {
        result += "/";
    } else {
        result += m_Path;
    }
    if (m_HasQuery) {
        result += "?";
        result += m_Query;
    }
    if (with_fragment && m_HasFragment) {
        result += "#";
        result += m_Fragment;
    }
    return result;
}

/*----------------------------------------------------------------------
|   NPT_Url::ToStringWithDefaultPort
+---------------------------------------------------------------------*/
NPT_String
NPT_Url::ToStringWithDefaultPort(NPT_UInt16 default_port, bool with_fragment) const
{
    NPT_String result;
    NPT_String request = ToRequestString(with_fragment);
    NPT_Size   length = m_Scheme.GetLength()+3+m_Host.GetLength()+6+request.GetLength();

    result.Reserve(length);
    result += m_Scheme;
    result += "://";
    result += m_Host;
    if (m_Port != default_port) {
        NPT_String port = NPT_String::FromInteger(m_Port);
        result += ":";
        result += port;
    }
    result += request;
    return result;
}

/*----------------------------------------------------------------------
|   NPT_Url::ToString
+---------------------------------------------------------------------*/
NPT_String
NPT_Url::ToString(bool with_fragment) const
{
    return ToStringWithDefaultPort(0, with_fragment);
}
