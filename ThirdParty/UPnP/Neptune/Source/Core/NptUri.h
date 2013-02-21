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
 ****************************************************************/

#ifndef _NPT_URI_H_
#define _NPT_URI_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptStrings.h"
#include "NptList.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const NPT_UInt16 NPT_URL_INVALID_PORT       = 0;
const NPT_UInt16 NPT_URL_DEFAULT_HTTP_PORT  = 80;
const NPT_UInt16 NPT_URL_DEFAULT_HTTPS_PORT = 443;

/*----------------------------------------------------------------------
|   NPT_Uri
+---------------------------------------------------------------------*/
class NPT_Uri {
public:
    // types
    typedef enum {
        SCHEME_ID_UNKNOWN,
        SCHEME_ID_HTTP,
        SCHEME_ID_HTTPS
    } SchemeId;

    // constants. use as a parameter to Encode()
    static const char* const PathCharsToEncode;
    static const char* const QueryCharsToEncode;
    static const char* const FragmentCharsToEncode;
    static const char* const UnsafeCharsToEncode;

    // class methods
    static NPT_String PercentEncode(const char* str, const char* chars, bool encode_percents=true);
    static NPT_String PercentDecode(const char* str);
    static SchemeId   ParseScheme(const NPT_String& scheme);

    // methods
    NPT_Uri() : m_SchemeId(SCHEME_ID_UNKNOWN) {}
    virtual ~NPT_Uri() {}
    const NPT_String& GetScheme() const {
        return m_Scheme;
    }
    void SetScheme(const char* scheme);
    NPT_Result SetSchemeFromUri(const char* uri);
    SchemeId GetSchemeId() const {
        return m_SchemeId;
    }

protected:
    // members
    NPT_String m_Scheme;
    SchemeId   m_SchemeId;
};

/*----------------------------------------------------------------------
|   NPT_UrlQuery
+---------------------------------------------------------------------*/
class NPT_UrlQuery
{
public:
    // class methods
    static NPT_String UrlEncode(const char* str, bool encode_percents=true);
    static NPT_String UrlDecode(const char* str);
    
    // types
    struct Field {
        Field(const char* name, const char* value, bool encoded);
        NPT_String m_Name;
        NPT_String m_Value;
    };

    // constructor
    NPT_UrlQuery() {}
    NPT_UrlQuery(const char* query);

    // accessors
    NPT_List<Field>& GetFields() { return m_Fields; }

    // methods
    NPT_Result  Parse(const char* query);
    NPT_Result  SetField(const char* name, const char* value, bool encoded=false);
    NPT_Result  AddField(const char* name, const char* value, bool encoded=false);
    const char* GetField(const char* name);
    NPT_String  ToString();

private:
    // members
    NPT_List<Field> m_Fields;
};

/*----------------------------------------------------------------------
|   NPT_Url
+---------------------------------------------------------------------*/
class NPT_Url : public NPT_Uri {
public:
    /**
     * Default constructor. This does not construct a valid URL, but an
     * uninitialized one that can later be initialized to a valid URL by
     * parsing or setting some of its fields.
     */
    NPT_Url();
    
    /**
     * Construct a URL by parsing an input string in its fully encoded form.
     * If an error occurs during parsing (such as an invalid syntax), the
     * URL will be in an invalid state (a call to IsValid() will return false).
     *
     * @param url The URL string in its encoded form
     * @param default_port The default port number, or 0 if not specified
     */
    NPT_Url(const char* url, NPT_UInt16  default_port = 0);
    
    /**
     * Construct a URL from its components. When constructing a URL from
     * components, the components are assumed to be passed in their non-encoded
     * form, and will thus be encoded automatically.
     *
     * @param scheme The URL scheme
     * @param port The port number
     * @param path The path
     * @param query The query, if any, or NULL
     * @param fragment The fragment, if any, or NULL
     */
    NPT_Url(const char* scheme,
            const char* host, 
            NPT_UInt16  port, 
            const char* path,
            const char* query = NULL,
            const char* fragment = NULL);

    /**
     * Parse a URL from its fully encoded form.
     *
     * @param url The URL string in its encoded form
     * @param default port The defautl port number, or 0 if not specified
     */
    NPT_Result Parse(const char* url, NPT_UInt16  default_port = 0);
    
    /**
     * Parse just the path plus optional query and fragment from a fully encoded form.
     *
     * @param path_plus The URL path plus optional query and fragment
     */
    NPT_Result ParsePathPlus(const char* path_plus);
    
    /**
     * Returns the host part of the URL, in its encoded form
     */
    const NPT_String& GetHost() const { return m_Host;     }
    
    /**
     * Returns the port number of the URL.
     */
    NPT_UInt16 GetPort() const { return m_Port;     }

    /**
     * Returns the path part of the URL, in its encoded form
     */
    const NPT_String& GetPath() const { return m_Path; }
    
    /**
     * Returns the path part of the URL, in its encoded or decoded form
     */
    NPT_String GetPath(bool decoded) const { return decoded?NPT_Uri::PercentDecode(m_Path):m_Path;}

    /**
     * Returns the query part of the URL, in its encoded form
     */
    const NPT_String& GetQuery() const { return m_Query; }
    
    /**
     * Returns the fragment part of the URL, in its encoded form
     */
    const NPT_String& GetFragment() const { return m_Fragment; }
    
    /**
     * Returns whether the URL is valid or not. Invalid URLs are uninitialized or
     * not fully initialized URLs.
     *
     * @return true if the URL is valid, false if it is not.
     */
    virtual bool IsValid() const;
    
    /**
     * Resets a URL to an uninitialized state.
     */
    void Reset();
    
    /**
     * Returns whether the URL has a query part or not.
     *
     * @return true if the URL has a query part, false if it does not.
     */
    bool HasQuery() const { return m_HasQuery; } 

    /**
     * Returns whether the URL has a fragment part or not.
     *
     * @return true if the URL has a fragment part, false if it does not.
     */
    bool HasFragment() const { return m_HasFragment; }

    /**
     * Sets the host part of the URL.
     *
     * @param host The host part of the URL
     */
    NPT_Result SetHost(const char* host);
    
    /**
     * Sets the port number of the URL.
     *
     * @param port The port number of the URL
     */
    NPT_Result SetPort(NPT_UInt16 port);
    
    /**
     * Sets the path part of the URL.
     *
     * @param path The path part of the URL
     * @param encoded Boolean flag indicating whether the path parameter is
     * already encoded or not. If it is not already encoded, it will be
     * automatically encoded.
     */
    NPT_Result SetPath(const char* path, bool encoded=false);
    
    /**
     * Sets the query part of the URL.
     * 
     * @param query The query part of the URL
     * @param encoded Boolean flag indicating whether the query parameter is
     * already encoded or not. If it is not already encoded, it will be
     * automatically encoded.
     */     
    NPT_Result SetQuery(const char* query, bool encoded=false);

    /**
     * Sets the fragment part of the URL.
     * 
     * @param query The fragment part of the URL
     * @param encoded Boolean flag indicating whether the fragment parameter is
     * already encoded or not. If it is not already encoded, it will be
     * automatically encoded.
     */     
    NPT_Result SetFragment(const char* fragment, bool encoded=false);

    /**
     * Return the string representation of the URL in a way that can be used in 
     * an HTTP request (i.e just the portion of the URL starting with the path)
     *
     * @param with_fragment Boolean flag specifiying whether the fragment part of 
     * the URL should be included in the returned string or not.
     */
    virtual NPT_String ToRequestString(bool with_fragment = false) const;

    /**
     * Return the string representation of the URL.
     *
     * @param default_port default port number for the scheme. If the port number of
     * the URL is not equal to the default port, then port number is explicitely 
     * included in the string representation of the URL. 
     * @param with_fragment Boolean flag specifiying whether the fragment part of 
     * the URL should be included in the returned string or not.
     */
    virtual NPT_String ToStringWithDefaultPort(NPT_UInt16 default_port, bool with_fragment = true) const;

    /**
     * Return the string representation of the URL.
     *
     * @param with_fragment Boolean flag specifiying whether the fragment part of 
     * the URL should be included in the returned string or not.
     */
    virtual NPT_String ToString(bool with_fragment = true) const;

protected:
    // members
    NPT_String m_Host;
    NPT_UInt16 m_Port;
    NPT_String m_Path;
    bool       m_HasQuery;
    NPT_String m_Query;
    bool       m_HasFragment;
    NPT_String m_Fragment;
};

#endif // _NPT_URI_H_
