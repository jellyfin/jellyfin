/*****************************************************************
|
|   Neptune - Xml Support
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
#include "NptConfig.h"
#include "NptTypes.h"
#include "NptXml.h"
#include "NptUtils.h"
#include "NptMap.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   local compilation flags
+---------------------------------------------------------------------*/
//#define NPT_XML_PARSER_DEBUG
#ifdef NPT_XML_PARSER_DEBUG
#define NPT_XML_Debug_0(s) NPT_Debug(s)
#define NPT_XML_Debug_1(s,x0) NPT_Debug(s,x0)
#define NPT_XML_Debug_2(s,x0,x1) NPT_Debug(s,x0,x1)
#define NPT_XML_Debug_3(s,x0,x1,x2) NPT_Debug(s,x0,x1,x2)
#define NPT_XML_Debug_4(s,x0,x1,x2,x3) NPT_Debug(s,x0,x1,x2,x3)
#else
#define NPT_XML_Debug_0(s)
#define NPT_XML_Debug_1(s,x0)
#define NPT_XML_Debug_2(s,x0,x1)
#define NPT_XML_Debug_3(s,x0,x1,x2)
#define NPT_XML_Debug_4(s,x0,x1,x2,x3)
#endif

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
static const NPT_String 
NPT_XmlNamespaceUri_Xml("http://www.w3.org/XML/1998/namespace");

/*----------------------------------------------------------------------
|   NPT_XmlAttributeFinder
+---------------------------------------------------------------------*/
class NPT_XmlAttributeFinder
{
public:
    // if 'namespc' is NULL, we're looking for ANY namespace
    // if 'namespc' is '\0', we're looking for NO namespace
    // if 'namespc' is non-empty, look for that SPECIFIC namespace
    NPT_XmlAttributeFinder(const NPT_XmlElementNode& element, 
                           const char*               name, 
                           const char*               namespc) : 
      m_Element(element), m_Name(name), m_Namespace(namespc) {}

    bool operator()(const NPT_XmlAttribute* const & attribute) const {
        if (attribute->m_Name == m_Name) {
            if (m_Namespace) {
                const NPT_String& prefix = attribute->GetPrefix();
                if (m_Namespace[0] == '\0') {
                    // match if the attribute has NO namespace
                    return prefix.IsEmpty();
                } else {
                    // match if the attribute has the SPECIFIC namespace
                    // we're looking for
                    if (prefix.IsEmpty()) {
                        // attributes without a prefix don't have a namespace
                        return false;
                    } else {
                        const NPT_String* namespc = m_Element.GetNamespaceUri(prefix);
                        return namespc && *namespc == m_Namespace;
                    }
                }
            } else {
                // ANY namespace will match
                return true;
            }
        } else {
            return false;
        }
    }

private:
    const NPT_XmlElementNode& m_Element;
    const char*               m_Name;
    const char*               m_Namespace;
};

/*----------------------------------------------------------------------
|   NPT_XmlAttributeFinderWithPrefix
+---------------------------------------------------------------------*/
class NPT_XmlAttributeFinderWithPrefix
{
public:
    NPT_XmlAttributeFinderWithPrefix(const char* prefix, const char* name) : 
      m_Prefix(prefix?prefix:""), m_Name(name) {}

    bool operator()(const NPT_XmlAttribute* const & attribute) const {
        return attribute->m_Prefix == m_Prefix && attribute->m_Name == m_Name;
    }

private:
    const char* m_Prefix;
    const char* m_Name;
};

/*----------------------------------------------------------------------
|   NPT_XmlTagFinder
+---------------------------------------------------------------------*/
class NPT_XmlTagFinder
{
public:
    // if 'namespc' is NULL, we're looking for ANY namespace
    // if 'namespc' is '\0', we're looking for NO namespace
    // if 'namespc' is non-empty, look for that SPECIFIC namespace
    NPT_XmlTagFinder(const char* tag, const char* namespc) : 
      m_Tag(tag), m_Namespace(namespc) {}

    bool operator()(const NPT_XmlNode* const & node) const {
        const NPT_XmlElementNode* element = node->AsElementNode();
        if (element && element->m_Tag == m_Tag) {
            if (m_Namespace) {
                // look for a SPECIFIC namespace or NO namespace
                const NPT_String* namespc = element->GetNamespace();
                if (namespc) {
                    // the element has a namespace, match if it is equal to
                    // what we're looking for
                    return *namespc == m_Namespace;
                } else {
                    // the element does not have a namespace, match if we're
                    // looking for NO namespace
                    return m_Namespace[0] == '\0';
                }
            } else {
                // ANY namespace will match
                return true;
            }
        } else {
            return false;
        }
    }

private:
    const char* m_Tag;
    const char* m_Namespace;
};

/*----------------------------------------------------------------------
|   NPT_XmlTextFinder
+---------------------------------------------------------------------*/
class NPT_XmlTextFinder
{
public:
    bool operator()(const NPT_XmlNode* const & node) const {
        return node->AsTextNode() != NULL;
    }
};

/*----------------------------------------------------------------------
|   NPT_XmlNamespaceCollapser
+---------------------------------------------------------------------*/
class NPT_XmlNamespaceCollapser
{
public:
    NPT_XmlNamespaceCollapser(NPT_XmlElementNode* element) : 
      m_Root(element) {}

    void operator()(NPT_XmlNode*& node) const {
        NPT_XmlElementNode* element = node->AsElementNode();
        if (element == NULL) return;

        // collapse the namespace for this element
        CollapseNamespace(element, element->GetPrefix());

        // collapse the namespaces for the attributes
        NPT_List<NPT_XmlAttribute*>::Iterator item = element->GetAttributes().GetFirstItem();
        while (item) {
            NPT_XmlAttribute* attribute = *item;
            CollapseNamespace(element, attribute->GetPrefix());
            ++item;
        }

        // recurse to the children
        element->GetChildren().Apply(*this);
    }

private:
    // methods
    void CollapseNamespace(NPT_XmlElementNode* element, const NPT_String& prefix) const;

    // members
    NPT_XmlElementNode* m_Root;
};

/*----------------------------------------------------------------------
|   NPT_XmlNamespaceCollapser::CollapseNamespace
+---------------------------------------------------------------------*/
void
NPT_XmlNamespaceCollapser::CollapseNamespace(NPT_XmlElementNode* element, 
                                             const NPT_String&   prefix) const
{
    if (m_Root->m_NamespaceMap == NULL ||
        (m_Root->m_NamespaceMap->GetNamespaceUri(prefix) == NULL && prefix != "xml")) {
        // the root element does not have that prefix in the map
        const NPT_String* uri = element->GetNamespaceUri(prefix);
        if (uri) m_Root->SetNamespaceUri(prefix, uri->GetChars());
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlAttribute::NPT_XmlAttribute
+---------------------------------------------------------------------*/
NPT_XmlAttribute::NPT_XmlAttribute(const char* name, const char* value) :
    m_Value(value)
{
    const char* cursor = name;
    while (char c = *cursor++) {
        if (c == ':') {
            unsigned int prefix_length = (unsigned int)(cursor-name)-1;
            m_Prefix.Assign(name, prefix_length);
            name = cursor;
            break;
        }
    }
    m_Name = name;
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::NPT_XmlElementNode
+---------------------------------------------------------------------*/
NPT_XmlElementNode::NPT_XmlElementNode(const char* prefix, const char* tag) :
    NPT_XmlNode(ELEMENT),
    m_Prefix(prefix),
    m_Tag(tag),
    m_NamespaceMap(NULL),
    m_NamespaceParent(NULL)
{
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::NPT_XmlElementNode
+---------------------------------------------------------------------*/
NPT_XmlElementNode::NPT_XmlElementNode(const char* tag) :
    NPT_XmlNode(ELEMENT),
    m_NamespaceMap(NULL),
    m_NamespaceParent(NULL)
{
    const char* cursor = tag;
    while (char c = *cursor++) {
        if (c == ':') {
            unsigned int prefix_length = (unsigned int)(cursor-tag)-1;
            m_Prefix.Assign(tag, prefix_length);
            tag = cursor;
            break;
        }
    }
    m_Tag = tag;
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::~NPT_XmlElementNode
+---------------------------------------------------------------------*/
NPT_XmlElementNode::~NPT_XmlElementNode()
{
    m_Children.Apply(NPT_ObjectDeleter<NPT_XmlNode>());
    m_Attributes.Apply(NPT_ObjectDeleter<NPT_XmlAttribute>());
    delete m_NamespaceMap;
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::SetParent
+---------------------------------------------------------------------*/
void
NPT_XmlElementNode::SetParent(NPT_XmlNode* parent)
{
    // update our parent
    m_Parent = parent;

    // update out namespace linkage
    NPT_XmlElementNode* parent_element =
        parent?parent->AsElementNode():NULL;
    NPT_XmlElementNode* namespace_parent;
    if (parent_element) {
        namespace_parent = 
            parent_element->m_NamespaceMap ? 
            parent_element:
            parent_element->m_NamespaceParent;
    } else {
        namespace_parent = NULL;
    }
    if (namespace_parent != m_NamespaceParent) {
        m_NamespaceParent = namespace_parent;
        RelinkNamespaceMaps();
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::AddChild
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlElementNode::AddChild(NPT_XmlNode* child)
{
    if (child == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    child->SetParent(this);
    return m_Children.Add(child);
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::GetChild
+---------------------------------------------------------------------*/
NPT_XmlElementNode*
NPT_XmlElementNode::GetChild(const char* tag, const char* namespc, NPT_Ordinal n) const
{
    // remap the requested namespace to match the semantics of the finder
    // and allow for "" to also mean NO namespace
    if (namespc == NULL || namespc[0] == '\0') {
        namespc = ""; // for the finder, empty string means NO namespace
    } else if (namespc[0] == '*' && namespc[1] == '\0') {
        namespc = NULL; // for the finder, NULL means ANY namespace
    }

    // find the child
    NPT_List<NPT_XmlNode*>::Iterator item;
    item = m_Children.Find(NPT_XmlTagFinder(tag, namespc), n);
    return item?(*item)->AsElementNode():NULL;
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::AddAttribute
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlElementNode::AddAttribute(const char* name, 
                                 const char* value)
{
    if (name == NULL || value == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    return m_Attributes.Add(new NPT_XmlAttribute(name, value));
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::SetAttribute
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlElementNode::SetAttribute(const char* prefix,
                                 const char* name, 
                                 const char* value)
{
    if (name == NULL || value == NULL) return NPT_ERROR_INVALID_PARAMETERS;

    /* see if this attribute is already set */
    NPT_List<NPT_XmlAttribute*>::Iterator attribute;
    attribute = m_Attributes.Find(NPT_XmlAttributeFinderWithPrefix(prefix, name));
    if (attribute) {
        // an attribute with this name and prefix already exists, 
        // change its value
        (*attribute)->SetValue(value); 
        return NPT_SUCCESS;
    }
    return m_Attributes.Add(new NPT_XmlAttribute(prefix, name, value));
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::SetAttribute
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlElementNode::SetAttribute(const char* name, const char* value)
{
    return SetAttribute(NULL, name, value);
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::GetAttribute
+---------------------------------------------------------------------*/
const NPT_String*
NPT_XmlElementNode::GetAttribute(const char* name, const char* namespc) const
{
    // remap the requested namespace to match the semantics of the finder
    // and allow for "" to also mean NO namespace
    if (namespc == NULL || namespc[0] == '\0') {
        namespc = ""; // for the finder, empty string means NO namespace
    } else if (namespc[0] == '*' && namespc[1] == '\0') {
        namespc = NULL; // for the finder, NULL means ANY namespace
    }

    // find the attribute
    NPT_List<NPT_XmlAttribute*>::Iterator attribute;
    attribute = m_Attributes.Find(NPT_XmlAttributeFinder(*this, name, namespc));
    if (attribute) { 
        return &(*attribute)->GetValue();
    } else {
        return NULL;
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::AddText
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlElementNode::AddText(const char* text)
{
    return AddChild(new NPT_XmlTextNode(NPT_XmlTextNode::CHARACTER_DATA, text));
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::GetText
+---------------------------------------------------------------------*/
const NPT_String*
NPT_XmlElementNode::GetText(NPT_Ordinal n) const
{
    NPT_List<NPT_XmlNode*>::Iterator node;
    node = m_Children.Find(NPT_XmlTextFinder(), n);
    return node?&(*node)->AsTextNode()->GetString():NULL;
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::MakeStandalone
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlElementNode::MakeStandalone()
{
    NPT_XmlNamespaceCollapser collapser(this);
    NPT_XmlNode* node_pointer = this;
    collapser(node_pointer);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::RelinkNamespaceMaps
+---------------------------------------------------------------------*/
void
NPT_XmlElementNode::RelinkNamespaceMaps()
{
    // update our children so that they can inherit the right
    // namespace map
    NPT_List<NPT_XmlNode*>::Iterator item = m_Children.GetFirstItem();
    while (item) {
        NPT_XmlElementNode* element = (*item)->AsElementNode();
        if (element) {
            if (m_NamespaceMap) {
                // we have a map, so our children point to us
                element->SetNamespaceParent(this);
            } else {
                // we don't have a map, so our children point to
                // where we also point
                element->SetNamespaceParent(m_NamespaceParent);
            }
        }
        ++item;
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::SetNamespaceParent
+---------------------------------------------------------------------*/
void
NPT_XmlElementNode::SetNamespaceParent(NPT_XmlElementNode* parent)
{
    m_NamespaceParent = parent;
    RelinkNamespaceMaps();
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::SetNamespaceUri
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlElementNode::SetNamespaceUri(const char* prefix, const char* uri)
{
    // ensure that we have a namespace map
    if (m_NamespaceMap == NULL) {
        m_NamespaceMap = new NPT_XmlNamespaceMap();
        RelinkNamespaceMaps();
    }

    return m_NamespaceMap->SetNamespaceUri(prefix, uri);
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::GetNamespaceUri
+---------------------------------------------------------------------*/
const NPT_String*
NPT_XmlElementNode::GetNamespaceUri(const char* prefix) const
{
    if (m_NamespaceMap) {
        // look in our namespace map first
        const NPT_String* namespc = m_NamespaceMap->GetNamespaceUri(prefix);
        if (namespc) {
            if (namespc->IsEmpty()) {
                return NULL;
            } else {
                return namespc;
            }
        }
    } 

    // look into our parent's namespace map
    if (m_NamespaceParent) {
        return m_NamespaceParent->GetNamespaceUri(prefix);
    } else {
        // check if this is a well-known namespace
        if (prefix[0] == 'x' && 
            prefix[1] == 'm' && 
            prefix[2] == 'l' && 
            prefix[3] == '\0') {
            return &NPT_XmlNamespaceUri_Xml;
        }

        // not found
        return NULL;
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::GetNamespace
+---------------------------------------------------------------------*/
const NPT_String*
NPT_XmlElementNode::GetNamespace() const
{
    return GetNamespaceUri(m_Prefix);
}

/*----------------------------------------------------------------------
|   NPT_XmlElementNode::GetNamespacePrefix
+---------------------------------------------------------------------*/
const NPT_String*
NPT_XmlElementNode::GetNamespacePrefix(const char* uri) const
{
    NPT_XmlNamespaceMap* namespace_map = 
        m_NamespaceMap? 
        m_NamespaceMap:
        (m_NamespaceParent?
         m_NamespaceParent->m_NamespaceMap:
         NULL);

    if (namespace_map) {
        return namespace_map->GetNamespacePrefix(uri);
    } else {
        return NULL;
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlTextNode::NPT_XmlTextNode
+---------------------------------------------------------------------*/
NPT_XmlTextNode::NPT_XmlTextNode(TokenType token_type, const char* text) :
    NPT_XmlNode(TEXT),
    m_TokenType(token_type),
    m_Text(text)
{
}

/*----------------------------------------------------------------------
|   NPT_XmlAccumulator
+---------------------------------------------------------------------*/
class NPT_XmlAccumulator {
public:
             NPT_XmlAccumulator();
            ~NPT_XmlAccumulator();
    void     Append(char c);
    void     Append(const char* s);
    void     AppendUTF8(unsigned int c);
    void     Reset() { m_Valid = 0; }
    const char*          GetString();
    NPT_Size             GetSize() const   { return m_Valid;  }
    const unsigned char* GetBuffer() const { return m_Buffer; }

private:
    // methods
    void Allocate(NPT_Size size);

    // members
    unsigned char* m_Buffer;
    NPT_Size       m_Allocated;
    NPT_Size       m_Valid;
};

/*----------------------------------------------------------------------
|   NPT_XmlAccumulator::NPT_XmlAccumulator
+---------------------------------------------------------------------*/
NPT_XmlAccumulator::NPT_XmlAccumulator() :
    m_Buffer(NULL),
    m_Allocated(0),
    m_Valid(0)
{
}

/*----------------------------------------------------------------------
|   NPT_XmlAccumulator::~NPT_XmlAccumulator
+---------------------------------------------------------------------*/
NPT_XmlAccumulator::~NPT_XmlAccumulator()
{
    delete[] m_Buffer;
}

/*----------------------------------------------------------------------
|   NPT_XmlAccumulator::Allocate
+---------------------------------------------------------------------*/
void
NPT_XmlAccumulator::Allocate(NPT_Size size)
{
    // check if we have enough
    if (m_Allocated >= size) return;

    // compute new size
    do {
        m_Allocated = m_Allocated ? m_Allocated * 2 : 32;
    } while (m_Allocated < size);

    // reallocate
    unsigned char* new_buffer = new unsigned char[m_Allocated];
    NPT_CopyMemory(new_buffer, m_Buffer, m_Valid);
    delete[] m_Buffer;
    m_Buffer = new_buffer;
}

/*----------------------------------------------------------------------
|   NPT_XmlAccumulator::Append
+---------------------------------------------------------------------*/
inline void
NPT_XmlAccumulator::Append(char c)
{
    NPT_Size needed = m_Valid+1;
    if (needed > m_Allocated) Allocate(needed);
    m_Buffer[m_Valid++] = c;
}

/*----------------------------------------------------------------------
|   NPT_XmlAccumulator::Append
+---------------------------------------------------------------------*/
void
NPT_XmlAccumulator::Append(const char* s)
{
    char c;
    while ((c = *s++)) Append(c);
}

/*----------------------------------------------------------------------
|   NPT_XmlAccumulator::AppendUTF8
+---------------------------------------------------------------------*/
inline void
NPT_XmlAccumulator::AppendUTF8(unsigned int c)
{
    NPT_Size needed = m_Valid+4; // allocate 4 more chars
    if (needed > m_Allocated) Allocate(needed);

    if (c <= 0x7F) {
        // 000000-00007F -> 1 char = 0xxxxxxx
        m_Buffer[m_Valid++] = (char)c;
    } else if (c <= 0x7FF) {
        // 000080-0007FF -> 2 chars = 110zzzzx 10xxxxxx
        m_Buffer[m_Valid++] = 0xC0|(c>>6  );
        m_Buffer[m_Valid++] = 0x80|(c&0x3F);
    } else if (c <= 0xFFFF) {
        // 000800-00FFFF -> 3 chars = 1110zzzz 10zxxxxx 10xxxxxx
        m_Buffer[m_Valid++] = 0xE0| (c>>12      );
        m_Buffer[m_Valid++] = 0x80|((c&0xFC0)>>6);
        m_Buffer[m_Valid++] = 0x80| (c&0x3F     );
    } else if (c <= 0x10FFFF) {
        // 010000-10FFFF -> 4 chars = 11110zzz 10zzxxxx 10xxxxxx 10xxxxxx
        m_Buffer[m_Valid++] = 0xF0| (c>>18         );
        m_Buffer[m_Valid++] = 0x80|((c&0x3F000)>>12);
        m_Buffer[m_Valid++] = 0x80|((c&0xFC0  )>> 6);
        m_Buffer[m_Valid++] = 0x80| (c&0x3F        );
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlAccumulator::GetString
+---------------------------------------------------------------------*/
inline const char*
NPT_XmlAccumulator::GetString()
{
    // ensure that the buffer is NULL terminated 
    Allocate(m_Valid+1);
    m_Buffer[m_Valid] = '\0';
    return (const char*)m_Buffer;
}

/*----------------------------------------------------------------------
|   NPT_XmlNamespaceMap::~NPT_XmlNamespaceMap
+---------------------------------------------------------------------*/
NPT_XmlNamespaceMap::~NPT_XmlNamespaceMap()
{
    m_Entries.Apply(NPT_ObjectDeleter<Entry>());
}

/*----------------------------------------------------------------------
|   NPT_XmlNamespaceMap::SetNamespaceUri
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlNamespaceMap::SetNamespaceUri(const char* prefix, const char* uri)
{
    NPT_List<Entry*>::Iterator item = m_Entries.GetFirstItem();
    while (item) {
        if ((*item)->m_Prefix == prefix) {
            // the prefix is already in the map, update the value
            (*item)->m_Uri = uri;
            return NPT_SUCCESS;
        }
        ++item;
    }

    // the prefix is not in the map, add it
    return m_Entries.Add(new Entry(prefix, uri));
}

/*----------------------------------------------------------------------
|   NPT_XmlNamespaceMap::GetNamespaceUri
+---------------------------------------------------------------------*/
const NPT_String*
NPT_XmlNamespaceMap::GetNamespaceUri(const char* prefix)
{
    NPT_List<Entry*>::Iterator item = m_Entries.GetFirstItem();
    while (item) {
        if ((*item)->m_Prefix == prefix) {
            // match
            return &(*item)->m_Uri;
        }
        ++item;
    }

    // the prefix is not in the map
    return NULL;
}

/*----------------------------------------------------------------------
|   NPT_XmlNamespaceMap::GetNamespacePrefix
+---------------------------------------------------------------------*/
const NPT_String*
NPT_XmlNamespaceMap::GetNamespacePrefix(const char* uri)
{
    NPT_List<Entry*>::Iterator item = m_Entries.GetFirstItem();
    while (item) {
        if ((*item)->m_Uri == uri) {
            // match
            return &(*item)->m_Prefix;
        }
        ++item;
    }

    // the uri is not in the map
    return NULL;
}

/*----------------------------------------------------------------------
|   character map
|
| flags:
| 1  --> any char
| 2  --> whitespace
| 4  --> name
| 8  --> content
| 16 --> value
+---------------------------------------------------------------------*/
#define NPT_XML_USE_CHAR_MAP
#if defined(NPT_XML_USE_CHAR_MAP)
// NOTE: this table is generated by the ruby script 'XmlCharMap.rb'
static const unsigned char NPT_XmlCharMap[256] = {
                   0, //   0 0x00 
                   0, //   1 0x01 
                   0, //   2 0x02 
                   0, //   3 0x03 
                   0, //   4 0x04 
                   0, //   5 0x05 
                   0, //   6 0x06 
                   0, //   7 0x07 
                   0, //   8 0x08 
            1|2|8|16, //   9 0x09 
            1|2|8|16, //  10 0x0a 
                   0, //  11 0x0b 
                   0, //  12 0x0c 
            1|2|8|16, //  13 0x0d 
                   0, //  14 0x0e 
                   0, //  15 0x0f 
                   0, //  16 0x10 
                   0, //  17 0x11 
                   0, //  18 0x12 
                   0, //  19 0x13 
                   0, //  20 0x14 
                   0, //  21 0x15 
                   0, //  22 0x16 
                   0, //  23 0x17 
                   0, //  24 0x18 
                   0, //  25 0x19 
                   0, //  26 0x1a 
                   0, //  27 0x1b 
                   0, //  28 0x1c 
                   0, //  29 0x1d 
                   0, //  30 0x1e 
                   0, //  31 0x1f 
            1|2|8|16, //  32 0x20 ' '
              1|8|16, //  33 0x21 '!'
              1|8|16, //  34 0x22 '"'
              1|8|16, //  35 0x23 '#'
              1|8|16, //  36 0x24 '$'
              1|8|16, //  37 0x25 '%'
                   1, //  38 0x26 '&'
              1|8|16, //  39 0x27 '''
              1|8|16, //  40 0x28 '('
              1|8|16, //  41 0x29 ')'
              1|8|16, //  42 0x2a '*'
              1|8|16, //  43 0x2b '+'
              1|8|16, //  44 0x2c ','
            1|4|8|16, //  45 0x2d '-'
            1|4|8|16, //  46 0x2e '.'
              1|8|16, //  47 0x2f '/'
            1|4|8|16, //  48 0x30 '0'
            1|4|8|16, //  49 0x31 '1'
            1|4|8|16, //  50 0x32 '2'
            1|4|8|16, //  51 0x33 '3'
            1|4|8|16, //  52 0x34 '4'
            1|4|8|16, //  53 0x35 '5'
            1|4|8|16, //  54 0x36 '6'
            1|4|8|16, //  55 0x37 '7'
            1|4|8|16, //  56 0x38 '8'
            1|4|8|16, //  57 0x39 '9'
            1|4|8|16, //  58 0x3a ':'
              1|8|16, //  59 0x3b ';'
                   1, //  60 0x3c '<'
              1|8|16, //  61 0x3d '='
              1|8|16, //  62 0x3e '>'
              1|8|16, //  63 0x3f '?'
              1|8|16, //  64 0x40 '@'
            1|4|8|16, //  65 0x41 'A'
            1|4|8|16, //  66 0x42 'B'
            1|4|8|16, //  67 0x43 'C'
            1|4|8|16, //  68 0x44 'D'
            1|4|8|16, //  69 0x45 'E'
            1|4|8|16, //  70 0x46 'F'
            1|4|8|16, //  71 0x47 'G'
            1|4|8|16, //  72 0x48 'H'
            1|4|8|16, //  73 0x49 'I'
            1|4|8|16, //  74 0x4a 'J'
            1|4|8|16, //  75 0x4b 'K'
            1|4|8|16, //  76 0x4c 'L'
            1|4|8|16, //  77 0x4d 'M'
            1|4|8|16, //  78 0x4e 'N'
            1|4|8|16, //  79 0x4f 'O'
            1|4|8|16, //  80 0x50 'P'
            1|4|8|16, //  81 0x51 'Q'
            1|4|8|16, //  82 0x52 'R'
            1|4|8|16, //  83 0x53 'S'
            1|4|8|16, //  84 0x54 'T'
            1|4|8|16, //  85 0x55 'U'
            1|4|8|16, //  86 0x56 'V'
            1|4|8|16, //  87 0x57 'W'
            1|4|8|16, //  88 0x58 'X'
            1|4|8|16, //  89 0x59 'Y'
            1|4|8|16, //  90 0x5a 'Z'
              1|8|16, //  91 0x5b '['
              1|8|16, //  92 0x5c '\'
              1|8|16, //  93 0x5d ']'
              1|8|16, //  94 0x5e '^'
            1|4|8|16, //  95 0x5f '_'
              1|8|16, //  96 0x60 '`'
            1|4|8|16, //  97 0x61 'a'
            1|4|8|16, //  98 0x62 'b'
            1|4|8|16, //  99 0x63 'c'
            1|4|8|16, // 100 0x64 'd'
            1|4|8|16, // 101 0x65 'e'
            1|4|8|16, // 102 0x66 'f'
            1|4|8|16, // 103 0x67 'g'
            1|4|8|16, // 104 0x68 'h'
            1|4|8|16, // 105 0x69 'i'
            1|4|8|16, // 106 0x6a 'j'
            1|4|8|16, // 107 0x6b 'k'
            1|4|8|16, // 108 0x6c 'l'
            1|4|8|16, // 109 0x6d 'm'
            1|4|8|16, // 110 0x6e 'n'
            1|4|8|16, // 111 0x6f 'o'
            1|4|8|16, // 112 0x70 'p'
            1|4|8|16, // 113 0x71 'q'
            1|4|8|16, // 114 0x72 'r'
            1|4|8|16, // 115 0x73 's'
            1|4|8|16, // 116 0x74 't'
            1|4|8|16, // 117 0x75 'u'
            1|4|8|16, // 118 0x76 'v'
            1|4|8|16, // 119 0x77 'w'
            1|4|8|16, // 120 0x78 'x'
            1|4|8|16, // 121 0x79 'y'
            1|4|8|16, // 122 0x7a 'z'
              1|8|16, // 123 0x7b '{'
              1|8|16, // 124 0x7c '|'
              1|8|16, // 125 0x7d '}'
              1|8|16, // 126 0x7e '~'
              1|8|16, // 127 0x7f 
              1|8|16, // 128 0x80 
              1|8|16, // 129 0x81 
              1|8|16, // 130 0x82 
              1|8|16, // 131 0x83 
              1|8|16, // 132 0x84 
              1|8|16, // 133 0x85 
              1|8|16, // 134 0x86 
              1|8|16, // 135 0x87 
              1|8|16, // 136 0x88 
              1|8|16, // 137 0x89 
              1|8|16, // 138 0x8a 
              1|8|16, // 139 0x8b 
              1|8|16, // 140 0x8c 
              1|8|16, // 141 0x8d 
              1|8|16, // 142 0x8e 
              1|8|16, // 143 0x8f 
              1|8|16, // 144 0x90 
              1|8|16, // 145 0x91 
              1|8|16, // 146 0x92 
              1|8|16, // 147 0x93 
              1|8|16, // 148 0x94 
              1|8|16, // 149 0x95 
              1|8|16, // 150 0x96 
              1|8|16, // 151 0x97 
              1|8|16, // 152 0x98 
              1|8|16, // 153 0x99 
              1|8|16, // 154 0x9a 
              1|8|16, // 155 0x9b 
              1|8|16, // 156 0x9c 
              1|8|16, // 157 0x9d 
              1|8|16, // 158 0x9e 
              1|8|16, // 159 0x9f 
              1|8|16, // 160 0xa0 
              1|8|16, // 161 0xa1 
              1|8|16, // 162 0xa2 
              1|8|16, // 163 0xa3 
              1|8|16, // 164 0xa4 
              1|8|16, // 165 0xa5 
              1|8|16, // 166 0xa6 
              1|8|16, // 167 0xa7 
              1|8|16, // 168 0xa8 
              1|8|16, // 169 0xa9 
              1|8|16, // 170 0xaa 
              1|8|16, // 171 0xab 
              1|8|16, // 172 0xac 
              1|8|16, // 173 0xad 
              1|8|16, // 174 0xae 
              1|8|16, // 175 0xaf 
              1|8|16, // 176 0xb0 
              1|8|16, // 177 0xb1 
              1|8|16, // 178 0xb2 
              1|8|16, // 179 0xb3 
              1|8|16, // 180 0xb4 
              1|8|16, // 181 0xb5 
              1|8|16, // 182 0xb6 
              1|8|16, // 183 0xb7 
              1|8|16, // 184 0xb8 
              1|8|16, // 185 0xb9 
              1|8|16, // 186 0xba 
              1|8|16, // 187 0xbb 
              1|8|16, // 188 0xbc 
              1|8|16, // 189 0xbd 
              1|8|16, // 190 0xbe 
              1|8|16, // 191 0xbf 
            1|4|8|16, // 192 0xc0 
            1|4|8|16, // 193 0xc1 
            1|4|8|16, // 194 0xc2 
            1|4|8|16, // 195 0xc3 
            1|4|8|16, // 196 0xc4 
            1|4|8|16, // 197 0xc5 
            1|4|8|16, // 198 0xc6 
            1|4|8|16, // 199 0xc7 
            1|4|8|16, // 200 0xc8 
            1|4|8|16, // 201 0xc9 
            1|4|8|16, // 202 0xca 
            1|4|8|16, // 203 0xcb 
            1|4|8|16, // 204 0xcc 
            1|4|8|16, // 205 0xcd 
            1|4|8|16, // 206 0xce 
            1|4|8|16, // 207 0xcf 
            1|4|8|16, // 208 0xd0 
            1|4|8|16, // 209 0xd1 
            1|4|8|16, // 210 0xd2 
            1|4|8|16, // 211 0xd3 
            1|4|8|16, // 212 0xd4 
            1|4|8|16, // 213 0xd5 
            1|4|8|16, // 214 0xd6 
              1|8|16, // 215 0xd7 
            1|4|8|16, // 216 0xd8 
            1|4|8|16, // 217 0xd9 
            1|4|8|16, // 218 0xda 
            1|4|8|16, // 219 0xdb 
            1|4|8|16, // 220 0xdc 
            1|4|8|16, // 221 0xdd 
            1|4|8|16, // 222 0xde 
            1|4|8|16, // 223 0xdf 
            1|4|8|16, // 224 0xe0 
            1|4|8|16, // 225 0xe1 
            1|4|8|16, // 226 0xe2 
            1|4|8|16, // 227 0xe3 
            1|4|8|16, // 228 0xe4 
            1|4|8|16, // 229 0xe5 
            1|4|8|16, // 230 0xe6 
            1|4|8|16, // 231 0xe7 
            1|4|8|16, // 232 0xe8 
            1|4|8|16, // 233 0xe9 
            1|4|8|16, // 234 0xea 
            1|4|8|16, // 235 0xeb 
            1|4|8|16, // 236 0xec 
            1|4|8|16, // 237 0xed 
            1|4|8|16, // 238 0xee 
            1|4|8|16, // 239 0xef 
            1|4|8|16, // 240 0xf0 
            1|4|8|16, // 241 0xf1 
            1|4|8|16, // 242 0xf2 
            1|4|8|16, // 243 0xf3 
            1|4|8|16, // 244 0xf4 
            1|4|8|16, // 245 0xf5 
            1|4|8|16, // 246 0xf6 
              1|8|16, // 247 0xf7 
            1|4|8|16, // 248 0xf8 
            1|4|8|16, // 249 0xf9 
            1|4|8|16, // 250 0xfa 
            1|4|8|16, // 251 0xfb 
            1|4|8|16, // 252 0xfc 
            1|4|8|16, // 253 0xfd 
            1|4|8|16, // 254 0xfe 
            1|4|8|16  // 255 0xff 
};
#endif // defined(NPT_XML_USE_CHAR_MAP)

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
#if defined (NPT_XML_USE_CHAR_MAP)
#define NPT_XML_CHAR_IS_ANY_CHAR(c)        (NPT_XmlCharMap[c] & 1)
#define NPT_XML_CHAR_IS_WHITESPACE(c)      (NPT_XmlCharMap[c] & 2)
#define NPT_XML_CHAR_IS_NAME_CHAR(c)       (NPT_XmlCharMap[c] & 4)
#define NPT_XML_CHAR_IS_ENTITY_REF_CHAR(c) (NPT_XML_CHAR_IS_NAME_CHAR((c)) || ((c) == '#'))
#define NPT_XML_CHAR_IS_CONTENT_CHAR(c)    (NPT_XmlCharMap[c] & 8)
#define NPT_XML_CHAR_IS_VALUE_CHAR(c)      (NPT_XmlCharMap[c] & 16)
#else
#define NPT_XML_CHAR_IS_WHITESPACE(c) \
((c) == ' ' || (c) == '\t' || (c) == 0x0D || (c) == 0x0A)

#define NPT_XML_CHAR_IS_ANY_CHAR(c) \
(NPT_XML_CHAR_IS_WHITESPACE((c)) || ((c) >= 0x20))

#define NPT_XML_CHAR_IS_DIGIT(c) \
((c) >= '0' && (c) <= '9')

#define NPT_XML_CHAR_IS_LETTER(c) \
(((c) >= 'A' && (c) <= 'Z') || ((c) >= 'a' && (c) <= 'z') || ((c) >= 0xC0 && (c) <= 0xD6) || ((c) >= 0xD8 && (c) <= 0xF6) || ((c) >= 0xF8))

#define NPT_XML_CHAR_IS_NAME_CHAR(c) \
(NPT_XML_CHAR_IS_DIGIT((c)) || NPT_XML_CHAR_IS_LETTER((c)) || (c) == '.' || (c) == '-' || (c) == '_' || (c) == ':')

#define NPT_XML_CHAR_IS_ENTITY_REF_CHAR(c) \
(NPT_XML_CHAR_IS_NAME_CHAR((c)) || ((c) == '#'))

#define NPT_XML_CHAR_IS_CONTENT_CHAR(c) \
(NPT_XML_CHAR_IS_ANY_CHAR((c)) && ((c) != '&') && ((c) != '<'))

#define NPT_XML_CHAR_IS_VALUE_CHAR(c) \
(NPT_XML_CHAR_IS_ANY_CHAR((c)) && ((c) != '&') && ((c) != '<'))

#endif // defined(NPT_XML_USE_CHAR_MAP)

/*----------------------------------------------------------------------
|   NPT_XmlStringIsWhitespace
+---------------------------------------------------------------------*/
static bool
NPT_XmlStringIsWhitespace(const char* s, NPT_Size size)
{
    for (NPT_Size x=0; x<size; x++) {
        if (!NPT_XML_CHAR_IS_WHITESPACE((int)s[x])) {
            return false;
        }
    }

    return true;
}

/*----------------------------------------------------------------------
|   NPT_XmlProcessor class
+---------------------------------------------------------------------*/
class NPT_XmlProcessor {
public:
    // constructor and destructor
   NPT_XmlProcessor(NPT_XmlParser* parser);

    // methods
    NPT_Result ProcessBuffer(const char* buffer, NPT_Size size);
    void       Reset();
    
private:
    // types
    typedef enum {
        CONTEXT_NONE,
        CONTEXT_OPEN_TAG,
        CONTEXT_CLOSE_TAG,
        CONTEXT_ATTRIBUTE,
        CONTEXT_VALUE_SINGLE_QUOTE,
        CONTEXT_VALUE_DOUBLE_QUOTE
    } Context;

    typedef enum {
        STATE_IN_INIT,
        STATE_IN_BOM_EF,
        STATE_IN_BOM_BB,
        STATE_IN_WHITESPACE,
        STATE_IN_NAME,
        STATE_IN_NAME_SPECIAL,
        STATE_IN_VALUE_START,
        STATE_IN_VALUE,
        STATE_IN_TAG_START,
        STATE_IN_EMPTY_TAG_END,
        STATE_IN_CONTENT,
        STATE_IN_PROCESSING_INSTRUCTION_START,
        STATE_IN_PROCESSING_INSTRUCTION,
        STATE_IN_PROCESSING_INSTRUCTION_END,
        STATE_IN_COMMENT,
        STATE_IN_COMMENT_END_1,
        STATE_IN_COMMENT_END_2,
        STATE_IN_DTD,
        STATE_IN_DTD_MARKUP_DECL,
        STATE_IN_DTD_MARKUP_DECL_END,
        STATE_IN_CDATA,
        STATE_IN_CDATA_END_1,
        STATE_IN_CDATA_END_2,
        STATE_IN_SPECIAL,
        STATE_IN_ENTITY_REF
    } State;

    // members
    NPT_XmlParser*     m_Parser;
    State              m_State;
    Context            m_Context;
    bool               m_SkipNewline;
    NPT_XmlAccumulator m_Name;
    NPT_XmlAccumulator m_Value;
    NPT_XmlAccumulator m_Text;
    NPT_XmlAccumulator m_Entity;

    // methods
#ifdef NPT_XML_PARSER_DEBUG
    const char* StateName(State state) {
        switch (state) {
          case STATE_IN_INIT: return "IN_INIT";
          case STATE_IN_BOM_EF: return "IN_BOM_EF";
          case STATE_IN_BOM_BB: return "IN_BOM_BB";
          case STATE_IN_WHITESPACE: return "IN_WHITESPACE";
          case STATE_IN_NAME: return "IN_NAME";
          case STATE_IN_NAME_SPECIAL: return "IN_NAME_SPECIAL";
          case STATE_IN_VALUE_START: return "IN_VALUE_START";
          case STATE_IN_VALUE: return "IN_VALUE";
          case STATE_IN_TAG_START: return "IN_TAG_START";
          case STATE_IN_EMPTY_TAG_END: return "IN_EMPTY_TAG_END";
          case STATE_IN_CONTENT: return "IN_CONTENT";
          case STATE_IN_PROCESSING_INSTRUCTION_START: return "IN_PROCESSING_INSTRUCTION_START";
          case STATE_IN_PROCESSING_INSTRUCTION: return "IN_PROCESSING_INSTRUCTION";
          case STATE_IN_PROCESSING_INSTRUCTION_END: return "IN_PROCESSING_INSTRUCTION_END";
          case STATE_IN_COMMENT: return "IN_COMMENT";
          case STATE_IN_COMMENT_END_1: return "IN_COMMENT_END_1";
          case STATE_IN_COMMENT_END_2: return "IN_COMMENT_END_2";
          case STATE_IN_DTD: return "IN_DTD";
          case STATE_IN_DTD_MARKUP_DECL: return "IN_DTD_MARKUP_DECL";
          case STATE_IN_DTD_MARKUP_DECL_END: return "IN_DTD_MARKUP_DECL_END";
          case STATE_IN_CDATA: return "IN_CDATA";
          case STATE_IN_CDATA_END_1: return "IN_CDATA_END_1";
          case STATE_IN_CDATA_END_2: return "IN_CDATA_END_2";
          case STATE_IN_SPECIAL: return "IN_SPECIAL";
          case STATE_IN_ENTITY_REF: return "IN_ENTITY_REF";
        }
        return "UNKNOWN";
    }

    const char* ContextName(Context context) {
        switch (context) {
          case CONTEXT_NONE: return "NONE";
          case CONTEXT_OPEN_TAG: return "OPEN_TAG";
          case CONTEXT_CLOSE_TAG: return "CLOSE_TAG";
          case CONTEXT_ATTRIBUTE: return "ATTRIBUTE";
          case CONTEXT_VALUE_SINGLE_QUOTE: return "VALUE_SINGLE_QUOTE";
          case CONTEXT_VALUE_DOUBLE_QUOTE: return "VALUE_DOUBLE_QUOTE";
        }
        return "UNKNOWN";
    }
#endif /* NPT_XML_PARSER_DEBUG */

    inline void SetState(State state) {
        NPT_XML_Debug_3("\nstate transition: %s to %s [ctx=%s]\n", 
                        StateName(m_State), 
                        StateName(state),
                        ContextName(m_Context));
        m_State = state;
    }

    inline void SetState(State state, Context context) {
        NPT_XML_Debug_4("\nstate transition: %s [ctx=%s] to %s [ctx=%s]\n", 
                        StateName(m_State), 
                        ContextName(m_Context),
                        StateName(state), 
                        ContextName(context));
        m_State = state;
        m_Context = context;
    }

    NPT_Result ResolveEntity(NPT_XmlAccumulator& source,
                             NPT_XmlAccumulator& destination);
    NPT_Result FlushPendingText();
};

/*----------------------------------------------------------------------
|   NPT_XmlProcessor::NPT_XmlProcessor
+---------------------------------------------------------------------*/
NPT_XmlProcessor::NPT_XmlProcessor(NPT_XmlParser* parser) :
    m_Parser(parser),
    m_State(STATE_IN_INIT),
    m_Context(CONTEXT_NONE),
    m_SkipNewline(false)
{
}

/*----------------------------------------------------------------------
|   NPT_XmlProcessor::Reset
+---------------------------------------------------------------------*/
void
NPT_XmlProcessor::Reset()
{
    m_State       = STATE_IN_INIT;
    m_Context     = CONTEXT_NONE;
    m_SkipNewline = false;
}

/*----------------------------------------------------------------------
|   NPT_XmlProcessor::ResolveEntity
+---------------------------------------------------------------------*/
NPT_Result 
NPT_XmlProcessor::ResolveEntity(NPT_XmlAccumulator& source,
                                NPT_XmlAccumulator& destination)
{
    const char* entity = (const char*)source.GetString();
    
    if (NPT_StringsEqual(entity, "lt")) {
        destination.Append('<');
    } else if (NPT_StringsEqual(entity, "gt")) {
        destination.Append('>');
    } else if (NPT_StringsEqual(entity, "amp")) {
        destination.Append('&');
    } else if (NPT_StringsEqual(entity, "quot")) {
        destination.Append('"');
    } else if (NPT_StringsEqual(entity, "apos")) {
        destination.Append('\'');
    } else if (entity[0] == '#') {
        int i=1;
        int base = 10;
        if (entity[1] == 'x') {
            i++;
            base = 16;
        }
        int parsed = 0;
        while (char c = entity[i++]) {
            int digit = -1;
            if (c>='0' && c<='9') {
                digit = c-'0';
            } else if (base == 16) {
                if (c >= 'a' && c <= 'f') {
                    digit = 10+c-'a';
                } else if (c >= 'A' && c <= 'F') {
                    digit = 10+c-'A';
                }
            }
            if (digit == -1) {
                // invalid char, leave the entity unparsed
                destination.Append(source.GetString());
                return NPT_ERROR_INVALID_SYNTAX;
            }
            parsed = base*parsed+digit;
        }
        destination.AppendUTF8(parsed);
    } else {
        // unknown entity, leave as-is
        destination.Append(source.GetString());
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlProcessor::FlushPendingText
+---------------------------------------------------------------------*/
NPT_Result 
NPT_XmlProcessor::FlushPendingText()
{
    if (m_Text.GetSize() > 0) {
        NPT_CHECK(m_Parser->OnCharacterData(m_Text.GetString(),
                                            m_Text.GetSize()));
        m_Text.Reset();
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlProcessor::ProcessBuffer
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlProcessor::ProcessBuffer(const char* buffer, NPT_Size size)
{
    unsigned char c;

    while (size-- && (c = *buffer++)) {
        NPT_XML_Debug_1("[%c]", (c == '\n' || c == '\r') ? '#' : c);

        // normalize line ends
        if (m_SkipNewline) {
            m_SkipNewline = false;
            if (c == '\n') continue;
        }
        if (c == '\r') {
            m_SkipNewline = true;
            c = '\n';
        }

        // process the character
        switch (m_State) {
          case STATE_IN_INIT:
            if (NPT_XML_CHAR_IS_WHITESPACE(c)) {
                SetState(STATE_IN_WHITESPACE);
                break;
            } else if (c == '<') {
                SetState(STATE_IN_TAG_START);
                break;
            } else if (c == 0xEF) {
                SetState(STATE_IN_BOM_EF);
                break;
            }
            return NPT_ERROR_INVALID_SYNTAX;
            
          case STATE_IN_BOM_EF:
            if (c == 0xBB) {
                SetState(STATE_IN_BOM_BB);
                break;
            }
            return NPT_ERROR_INVALID_SYNTAX;
            
          case STATE_IN_BOM_BB:
            if (c == 0xBF) {
                SetState(STATE_IN_WHITESPACE);
                break;
            }
            return NPT_ERROR_INVALID_SYNTAX;
                        
          case STATE_IN_WHITESPACE:
            if (NPT_XML_CHAR_IS_WHITESPACE(c)) break;
            switch (m_Context) {
              case CONTEXT_NONE:
                if (c == '<') {
                    SetState(STATE_IN_TAG_START);
                } else {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                break;
                
              case CONTEXT_ATTRIBUTE:
                if (c == '/') {
                    SetState(STATE_IN_EMPTY_TAG_END, CONTEXT_NONE);
                } else if (c == '>') {
                    SetState(STATE_IN_CONTENT, CONTEXT_NONE);
                } else if (NPT_XML_CHAR_IS_NAME_CHAR(c)) {
                    m_Name.Reset();
                    m_Name.Append(c);
                    SetState(STATE_IN_NAME);
                } else {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                break;

              case CONTEXT_CLOSE_TAG:
                if (c == '>') {
                    NPT_CHECK(FlushPendingText());
                    NPT_CHECK(m_Parser->OnEndElement(m_Name.GetString()));
                    SetState(STATE_IN_CONTENT, CONTEXT_NONE);
                } else {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                break;

              default:
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_NAME:
            if (NPT_XML_CHAR_IS_NAME_CHAR(c)) {
                m_Name.Append(c);
                break;
            }
            switch (m_Context) {
              case CONTEXT_ATTRIBUTE:
                if (c == '=') {
                    m_Value.Reset();
                    SetState(STATE_IN_VALUE_START);
                } else if (!NPT_XML_CHAR_IS_WHITESPACE(c)) {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                break;

              case CONTEXT_OPEN_TAG:
                if (c == '>' || c == '/' || NPT_XML_CHAR_IS_WHITESPACE(c)) {
                    NPT_CHECK(FlushPendingText());
                    NPT_CHECK(m_Parser->OnStartElement(m_Name.GetString()));
                    m_Name.Reset();
                    if (c == '>') {
                        SetState(STATE_IN_CONTENT, CONTEXT_NONE);
                    } else if (c == '/') {
                        SetState(STATE_IN_EMPTY_TAG_END);
                    } else {
                        SetState(STATE_IN_WHITESPACE, CONTEXT_ATTRIBUTE);
                    }
                } else {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                break;

              case CONTEXT_CLOSE_TAG:
                if (c == '>') {
                    NPT_CHECK(FlushPendingText());
                    NPT_CHECK(m_Parser->OnEndElement(m_Name.GetString()));
                    SetState(STATE_IN_CONTENT, CONTEXT_NONE);
                } else if (NPT_XML_CHAR_IS_WHITESPACE(c)) {
                    SetState(STATE_IN_WHITESPACE);
                } else {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                break;

              default:
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_NAME_SPECIAL:
            if (NPT_XML_CHAR_IS_NAME_CHAR(c) || (c == '[')) {
                m_Name.Append(c);

                const unsigned char* nb = m_Name.GetBuffer();
                if (m_Name.GetSize() == 2) {
                    if (nb[0] == '-' &&
                        nb[1] == '-') {
                        m_Name.Reset();
                        SetState(STATE_IN_COMMENT, CONTEXT_NONE);
                        break;
                    }
                } else if (m_Name.GetSize() == 7) {
                    if (nb[0] == '[' &&
                        nb[1] == 'C' &&
                        nb[2] == 'D' &&
                        nb[3] == 'A' &&
                        nb[4] == 'T' &&
                        nb[5] == 'A' &&
                        nb[6] == '[') {
                        m_Name.Reset();
                        SetState(STATE_IN_CDATA, CONTEXT_NONE);
                        break;
                    }
                }
                break;
            }
            if (NPT_XML_CHAR_IS_WHITESPACE(c)) {
                const char* special = m_Name.GetString();
                if (special && NPT_StringsEqual(special, "DOCTYPE")) {
                    SetState(STATE_IN_DTD, CONTEXT_NONE);
                } else {
                    SetState(STATE_IN_SPECIAL, CONTEXT_NONE);
                }
                m_Name.Reset();
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_VALUE_START:
            if (NPT_XML_CHAR_IS_WHITESPACE(c)) break;
            if (c == '"') {
                m_Value.Reset();
                SetState(STATE_IN_VALUE, CONTEXT_VALUE_DOUBLE_QUOTE);
            } else if (c == '\'') {
                m_Value.Reset();
                SetState(STATE_IN_VALUE, CONTEXT_VALUE_SINGLE_QUOTE);
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_VALUE:
            if ((c == '"'  && m_Context == CONTEXT_VALUE_DOUBLE_QUOTE) || 
                (c == '\'' && m_Context == CONTEXT_VALUE_SINGLE_QUOTE)) {
                NPT_CHECK(m_Parser->OnElementAttribute(m_Name.GetString(),
                                                       m_Value.GetString()));
                SetState(STATE_IN_WHITESPACE, CONTEXT_ATTRIBUTE);
            } else if (c == '&') {
                m_Entity.Reset();
                SetState(STATE_IN_ENTITY_REF);
            } else if (NPT_XML_CHAR_IS_WHITESPACE(c)) {
                m_Value.Append(' ');
            } else if (NPT_XML_CHAR_IS_VALUE_CHAR(c)) {
                m_Value.Append(c);
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_TAG_START:
            m_Name.Reset();
            if (c == '!') {
                SetState(STATE_IN_NAME_SPECIAL, CONTEXT_NONE);
            } else if (c == '?') {
                SetState(STATE_IN_PROCESSING_INSTRUCTION, CONTEXT_NONE);
            } else if (c == '/') {
                SetState(STATE_IN_NAME, CONTEXT_CLOSE_TAG);
            } else if (NPT_XML_CHAR_IS_NAME_CHAR(c)) {
                m_Name.Append(c);
                SetState(STATE_IN_NAME, CONTEXT_OPEN_TAG);
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_EMPTY_TAG_END:
            if (c == '>') {
                NPT_CHECK(FlushPendingText());
                NPT_CHECK(m_Parser->OnEndElement(NULL));
                SetState(STATE_IN_CONTENT, CONTEXT_NONE);
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_ENTITY_REF:
            switch (m_Context) {
              case CONTEXT_VALUE_SINGLE_QUOTE:
              case CONTEXT_VALUE_DOUBLE_QUOTE:
                if (c == ';') {
                    NPT_CHECK(ResolveEntity(m_Entity, m_Value));
                    SetState(STATE_IN_VALUE);
                } else if (NPT_XML_CHAR_IS_ENTITY_REF_CHAR(c)) {
                    m_Entity.Append(c);
                } else {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                break;

              case CONTEXT_NONE:
                if (c == ';') {
                    NPT_CHECK(ResolveEntity(m_Entity, m_Text));
                    SetState(STATE_IN_CONTENT);
                } else if (NPT_XML_CHAR_IS_ENTITY_REF_CHAR(c)) {
                    m_Entity.Append(c);
                } else {
                    return NPT_ERROR_INVALID_SYNTAX;
                }
                break;
                
              default:
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_COMMENT:
            if (c == '-') {
                SetState(STATE_IN_COMMENT_END_1);
            } else if (!NPT_XML_CHAR_IS_ANY_CHAR(c)) {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_COMMENT_END_1:
            if (c == '-') {
                SetState(STATE_IN_COMMENT_END_2);
            } else if (NPT_XML_CHAR_IS_ANY_CHAR(c)) {
                SetState(STATE_IN_COMMENT);
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_COMMENT_END_2:
            if (c == '>') {
                SetState(STATE_IN_CONTENT, CONTEXT_NONE);
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_CONTENT:
            if (c == '<') {
                SetState(STATE_IN_TAG_START, CONTEXT_NONE);
            } else if (c == '&') {
                m_Entity.Reset();
                SetState(STATE_IN_ENTITY_REF);
            } else {
                m_Text.Append(c);
            }
            break;

          case STATE_IN_PROCESSING_INSTRUCTION_START:
            break;

          case STATE_IN_PROCESSING_INSTRUCTION_END:
            if (c == '>') {
                SetState(STATE_IN_WHITESPACE, CONTEXT_NONE);
            } else {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_PROCESSING_INSTRUCTION:
            if (c == '?') {
                SetState(STATE_IN_PROCESSING_INSTRUCTION_END);
            }
            break;

          case STATE_IN_DTD:
            if (NPT_XML_CHAR_IS_WHITESPACE(c)) break;
            if (c == '[') {
                SetState(STATE_IN_DTD_MARKUP_DECL);
            } else if (c == '>') {
                SetState(STATE_IN_WHITESPACE, CONTEXT_NONE);
            }
            break;

          case STATE_IN_DTD_MARKUP_DECL:
            if (c == ']') {
                SetState(STATE_IN_DTD_MARKUP_DECL_END);
            }
            break;

          case STATE_IN_DTD_MARKUP_DECL_END:
            if (c == '>') {
                SetState(STATE_IN_WHITESPACE, CONTEXT_NONE);
            } else if (!NPT_XML_CHAR_IS_WHITESPACE(c)) {
                return NPT_ERROR_INVALID_SYNTAX;
            }
            break;

          case STATE_IN_CDATA:
            if (c == ']') {
                SetState(STATE_IN_CDATA_END_1);
            } else  {
                m_Text.Append(c);
            }
            break;

          case STATE_IN_CDATA_END_1:
            if (c == ']') {
                SetState(STATE_IN_CDATA_END_2);
            } else {
                m_Text.Append(']');
                m_Text.Append(c);
                SetState(STATE_IN_CDATA);
            }
            break;

          case STATE_IN_CDATA_END_2:
            if (c == '>') {
                SetState(STATE_IN_CONTENT, CONTEXT_NONE);
            } else {
                m_Text.Append("]]");
                m_Text.Append(c);
                SetState(STATE_IN_CDATA);
            }
            break;

          case STATE_IN_SPECIAL:
            if (c == '>') {
                SetState(STATE_IN_WHITESPACE, CONTEXT_NONE);
            }
            break;
        }
    }

    return NPT_SUCCESS;
}       

/*----------------------------------------------------------------------
|   NPT_XmlParser::NPT_XmlParser
+---------------------------------------------------------------------*/
NPT_XmlParser::NPT_XmlParser(bool keep_whitespace /* = false */) :
    m_Root(NULL),
    m_CurrentElement(NULL),
    m_KeepWhitespace(keep_whitespace)
{
    m_Processor = new NPT_XmlProcessor(this);
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::~NPT_XmlParser
+---------------------------------------------------------------------*/
NPT_XmlParser::~NPT_XmlParser()
{
    Reset();
    delete m_CurrentElement;
    delete m_Processor;
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::Reset
+---------------------------------------------------------------------*/
void
NPT_XmlParser::Reset()
{
    // delete anything that has been created 
    NPT_XmlNode* walker = m_CurrentElement; 
    while (walker && walker->GetParent()) { 
        walker = walker->GetParent(); 
    } 
    delete walker; 
    m_CurrentElement = NULL; 
    
    m_Processor->Reset();
    
    m_Root = NULL;
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::Parse
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlParser::Parse(NPT_InputStream& stream, 
                     NPT_Size&        size,
                     NPT_XmlNode*&    node,
                     bool             incremental /* = false */)
{       
    NPT_Result result;

    // start with a known state
    m_Root = NULL;
    node = NULL;
    if (!incremental) {
        Reset();
    }
    
    // use a buffer on the stack
    char buffer[1024];

    // read a buffer and parse it until the end of the stream
    NPT_Size max_bytes_to_read = size;
    size = 0;
    do {
        NPT_Size bytes_read;
        NPT_Size bytes_to_read = sizeof(buffer);
        if (max_bytes_to_read != 0 && 
            size+bytes_to_read > max_bytes_to_read) {
            bytes_to_read = max_bytes_to_read-size;
        }
        result = stream.Read(buffer, bytes_to_read, &bytes_read);
        if (NPT_SUCCEEDED(result)) {
            // update the counter
            size += bytes_read;

            // parse the buffer
            result = m_Processor->ProcessBuffer(buffer, bytes_read);
            if (NPT_FAILED(result)) break;
        } else {
            break;
        }
    } while(NPT_SUCCEEDED(result) && 
            (max_bytes_to_read == 0 || size < max_bytes_to_read));

    // return a tree if we have one 
    node = m_Root;
    if (incremental) {
        return result;
    } else {
        if (NPT_FAILED(result) && result != NPT_ERROR_EOS) {
            delete m_Root;
            m_Root = NULL;
            node = NULL;
            return result;
        } else {
            return m_Root?NPT_SUCCESS:NPT_ERROR_XML_NO_ROOT;     
        }
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::Parse
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlParser::Parse(NPT_InputStream& stream, 
                     NPT_XmlNode*&    node,
                     bool             incremental /* = false */)
{
    NPT_Size max_read = 0; // no limit
    return Parse(stream, max_read, node, incremental);
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::Parse
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlParser::Parse(const char*   xml, 
                     NPT_XmlNode*& node, 
                     bool          incremental /* = false */)
{       
    NPT_Size size = NPT_StringLength(xml);

    return Parse(xml, size, node, incremental);
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::Parse
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlParser::Parse(const char*   xml, 
                     NPT_Size      size, 
                     NPT_XmlNode*& node,
                     bool          incremental /* = false */)
{ 
    // start with a known state
    m_Root = NULL;
    node = NULL;
    if (!incremental) {
        Reset();
    }

    // parse the buffer
    NPT_Result result = m_Processor->ProcessBuffer(xml, size);
    
    // return a tree if we have one 
    node = m_Root;
    if (incremental) {
        return result;
    } else {
        if (NPT_FAILED(result)) {
            delete m_Root;
            m_Root = NULL;
            node = NULL;
            return result;
        } else {
            return m_Root?NPT_SUCCESS:NPT_ERROR_XML_NO_ROOT;     
        }
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::OnStartElement
+---------------------------------------------------------------------*/
NPT_Result 
NPT_XmlParser::OnStartElement(const char* name)
{
    NPT_XML_Debug_1("\nNPT_XmlParser::OnStartElement: %s\n", name);

    // we cannot start an element if we already have a root
    if (m_Root) {
        return NPT_ERROR_XML_MULTIPLE_ROOTS;
    }
    
    // create new node
    NPT_XmlElementNode* node = new NPT_XmlElementNode(name);

    // add node to tree
    if (m_CurrentElement) {
        // add the new node
        m_CurrentElement->AddChild(node);
    }
    m_CurrentElement = node;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::OnElementAttribute
+---------------------------------------------------------------------*/
NPT_Result 
NPT_XmlParser::OnElementAttribute(const char* name, const char* value)
{
    NPT_XML_Debug_2("\nNPT_XmlParser::OnElementAttribute: name=%s, value='%s'\n", 
                    name, value);

    if (m_CurrentElement == NULL) {
        return NPT_ERROR_INVALID_SYNTAX;
    }
                              
    // check if this is a namespace attribute
    if (name[0] == 'x' && 
        name[1] == 'm' && 
        name[2] == 'l' && 
        name[3] == 'n' &&
        name[4] == 's' &&
        (name[5] == '\0' || name[5] == ':')) {
        // namespace definition
        m_CurrentElement->SetNamespaceUri((name[5] == ':')?name+6:"", value);
    } else {
        m_CurrentElement->AddAttribute(name, value);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::OnEndElement
+---------------------------------------------------------------------*/
NPT_Result 
NPT_XmlParser::OnEndElement(const char* name)
{
    NPT_XML_Debug_1("\nNPT_XmlParser::OnEndElement: %s\n", name ? name : "NULL");

    if (m_CurrentElement == NULL) return NPT_ERROR_XML_TAG_MISMATCH;

    // check that the name matches (if there is a name)
    if (name) {
        const char*  prefix = name;
        unsigned int prefix_length = 0;
        const char*  tag    = name;
        const char*  cursor = name;
        while (char c = *cursor++) {
            if (c == ':') {
                prefix_length = (unsigned int)(cursor-name)-1;
                tag = cursor;
            }
        }
        // check that the name and prefix length match
        if (m_CurrentElement->GetTag() != tag ||
            m_CurrentElement->GetPrefix().GetLength() != prefix_length) {
            return NPT_ERROR_XML_TAG_MISMATCH;
        }

        // check the prefix
        const char* current_prefix = m_CurrentElement->GetPrefix().GetChars();
        for (unsigned int i=0; i<prefix_length; i++) {
            if (current_prefix[i] != prefix[i]) {
                return NPT_ERROR_XML_TAG_MISMATCH;
            }
        }
    }

    // pop up the stack
    NPT_XmlNode* parent = m_CurrentElement->GetParent();
    if (parent) {
        m_CurrentElement = parent->AsElementNode();
    } else {
        if (m_Root) {
            // this should never happen
            delete m_CurrentElement;
            m_CurrentElement = NULL;
            return NPT_ERROR_XML_MULTIPLE_ROOTS;
        } else {
            m_Root = m_CurrentElement;
            m_CurrentElement = NULL;
        }
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlParser::OnCharacterData
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlParser::OnCharacterData(const char* data, unsigned long size)
{ 
    NPT_XML_Debug_1("\nNPT_XmlParser::OnCharacterData: %s\n", data);
    
    // check that we have a current element
    if (m_CurrentElement == NULL) {
        // we do not allow non-whitespace outside an element content
        if (!NPT_XmlStringIsWhitespace(data, size)) {
            return NPT_ERROR_XML_INVALID_NESTING;
        }

        // ignore whitespace
        return NPT_SUCCESS;
    }

    // ignore whitespace if applicable
    if (m_KeepWhitespace || !NPT_XmlStringIsWhitespace(data, size)) {
        // add the text to the current element
        m_CurrentElement->AddText(data);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlAttributeWriter
+---------------------------------------------------------------------*/
class NPT_XmlAttributeWriter
{
public:
    NPT_XmlAttributeWriter(NPT_XmlSerializer& serializer) : m_Serializer(serializer) {}
    void operator()(NPT_XmlAttribute*& attribute) const {
        m_Serializer.Attribute(attribute->GetPrefix(),
                               attribute->GetName(),
                               attribute->GetValue());
    }

private:
    // members
    NPT_XmlSerializer& m_Serializer;
};

/*----------------------------------------------------------------------
|   NPT_XmlNodeWriter
+---------------------------------------------------------------------*/
class NPT_XmlNodeWriter
{
public:
    NPT_XmlNodeWriter(NPT_XmlSerializer& serializer) : 
        m_Serializer(serializer), m_AttributeWriter(serializer) {
        m_Serializer.StartDocument();
    }
    void operator()(NPT_XmlNode*& node) const {
        if (NPT_XmlElementNode* element = node->AsElementNode()) {
            const NPT_String& prefix = element->GetPrefix();
            const NPT_String& tag    = element->GetTag();
            m_Serializer.StartElement(prefix, tag);
            element->GetAttributes().Apply(m_AttributeWriter);

            // emit namespace attributes
            if (element->m_NamespaceMap) {
                NPT_List<NPT_XmlNamespaceMap::Entry*>::Iterator item = 
                    element->m_NamespaceMap->m_Entries.GetFirstItem();
                while (item) {
                    if ((*item)->m_Prefix.IsEmpty()) {
                        // default namespace
                        m_Serializer.Attribute(NULL, "xmlns", (*item)->m_Uri);
                    } else {
                        // namespace with prefix
                        m_Serializer.Attribute("xmlns", (*item)->m_Prefix, (*item)->m_Uri);
                    }
                    ++item;
                }
            }

            element->GetChildren().Apply(*this);
            m_Serializer.EndElement(prefix, tag);
        } else if (NPT_XmlTextNode* text = node->AsTextNode()) {
            m_Serializer.Text(text->GetString());
        }
    }

private:
    // members
    NPT_XmlSerializer&     m_Serializer;
    NPT_XmlAttributeWriter m_AttributeWriter;
};

/*----------------------------------------------------------------------
|   NPT_XmlNodeCanonicalWriter
+---------------------------------------------------------------------*/
class NPT_XmlNodeCanonicalWriter
{
public:
    // types
    struct MapChainLink {
        MapChainLink(MapChainLink* parent) : m_Parent(parent) {}
        MapChainLink*                   m_Parent;
        NPT_Map<NPT_String, NPT_String> m_RenderedNamespaces;
    };

    // constructor
    NPT_XmlNodeCanonicalWriter(NPT_XmlSerializer& serializer, 
                               MapChainLink*      map_chain = NULL) : 
        m_MapChain(map_chain),
        m_Serializer(serializer) {
        m_Serializer.StartDocument();
    }
    void operator()(NPT_XmlNode*& node) const;

private:
    // types
    struct SortedAttributeList {
        // types
        struct Entry {
            const NPT_String*       m_NamespaceUri;
            const NPT_XmlAttribute* m_Attribute;
        };

        // methods
        void Add(const NPT_String* namespace_uri, 
                 const NPT_XmlAttribute* attribute);
        void Emit(NPT_XmlSerializer& serializer);

        // members
        NPT_List<Entry> m_Entries;
    };

    struct SortedNamespaceList {
        // types
        struct Entry {
            const NPT_String* m_NamespacePrefix;
            const NPT_String* m_NamespaceUri;
        };

        // methods
        void Add(const NPT_String* prefix, const NPT_String* uri);
        void Emit(NPT_XmlSerializer& serializer);

        // members
        NPT_List<Entry> m_Entries;
    };

    // methods
    const NPT_String* GetNamespaceRenderedForPrefix(const NPT_String& prefix) const;

    // members
    MapChainLink*      m_MapChain;
    NPT_XmlSerializer& m_Serializer;
};

/*----------------------------------------------------------------------
|   NPT_XmlNodeCanonicalWriter::SortedAttributeList::Add
+---------------------------------------------------------------------*/
void
NPT_XmlNodeCanonicalWriter::SortedAttributeList::Add(
    const NPT_String*       namespace_uri,
    const NPT_XmlAttribute* attribute)
{
    // transform empty strings into NULL pointers
    if (namespace_uri && namespace_uri->IsEmpty()) namespace_uri = NULL;

    // find the namespace insertion position
    NPT_List<Entry>::Iterator entry = m_Entries.GetFirstItem();
    for (; entry; ++entry) {
        // decide if we insert now or move on
        const NPT_String* other_namespace_uri = entry->m_NamespaceUri;
        if (namespace_uri &&
            (other_namespace_uri == NULL || *namespace_uri > *other_namespace_uri)) {
            // this namespace uri is greater than the other, skip
            continue;
        } else if ((namespace_uri == NULL && other_namespace_uri == NULL) ||
                   (namespace_uri && other_namespace_uri && 
                   *namespace_uri == *other_namespace_uri)) {
            // namespace uris match, compare the names
            const NPT_XmlAttribute* other_attribute = entry->m_Attribute;
            if (attribute->GetName() > other_attribute->GetName()) continue;
        }
        break;
    }
    
    Entry new_entry = {namespace_uri, attribute};
    m_Entries.Insert(entry, new_entry);
}

/*----------------------------------------------------------------------
|   NPT_XmlNodeCanonicalWriter::SortedAttributeList::Emit
+---------------------------------------------------------------------*/
void
NPT_XmlNodeCanonicalWriter::SortedAttributeList::Emit(NPT_XmlSerializer& serializer)
{
    for (NPT_List<Entry>::Iterator i = m_Entries.GetFirstItem(); i; ++i) {
        serializer.Attribute(i->m_Attribute->GetPrefix(),
                             i->m_Attribute->GetName(),
                             i->m_Attribute->GetValue());
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlNodeCanonicalWriter::SortedNamespaceList::Add
+---------------------------------------------------------------------*/
void
NPT_XmlNodeCanonicalWriter::SortedNamespaceList::Add(const NPT_String* prefix,
                                                     const NPT_String* uri)
{
    // find the namespace insertion position
    NPT_List<Entry>::Iterator entry = m_Entries.GetFirstItem();
    if (prefix && !prefix->IsEmpty()) {
        for (; entry; ++entry) {
            // decide if we insert now or move on
            if (entry->m_NamespacePrefix && *prefix <= *entry->m_NamespacePrefix) {
                break;
            }
        }
    } else {
        prefix = NULL;
    }

    Entry new_entry = {prefix, uri};
    m_Entries.Insert(entry, new_entry);
}

/*----------------------------------------------------------------------
|   NPT_XmlNodeCanonicalWriter::SortedNamespaceList::Emit
+---------------------------------------------------------------------*/
void
NPT_XmlNodeCanonicalWriter::SortedNamespaceList::Emit(NPT_XmlSerializer& serializer)
{
    for (NPT_List<Entry>::Iterator i = m_Entries.GetFirstItem(); i; ++i) {
        const NPT_String* key   = i->m_NamespacePrefix;
        const NPT_String* value = i->m_NamespaceUri;
        if (key == NULL) {
            serializer.Attribute(NULL, "xmlns", *value);
        } else if (*key != "xml" || *value != NPT_XmlNamespaceUri_Xml) {
            serializer.Attribute("xmlns", *key, *value);
        }
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlNodeCanonicalWriter::GetNamespaceRenderedForPrefix
+---------------------------------------------------------------------*/
const NPT_String*
NPT_XmlNodeCanonicalWriter::GetNamespaceRenderedForPrefix(const NPT_String& prefix) const
{
    for (MapChainLink* link = m_MapChain;
         link;
         link = link->m_Parent) {
        NPT_String* uri;
        if (NPT_SUCCEEDED(link->m_RenderedNamespaces.Get(prefix, uri))) {
            return uri;
        }
    }

    return NULL;
}

/*----------------------------------------------------------------------
|   NPT_XmlNodeCanonicalWriter::operator()
+---------------------------------------------------------------------*/
void
NPT_XmlNodeCanonicalWriter::operator()(NPT_XmlNode*& node) const
{
    MapChainLink map_link(m_MapChain);

    if (NPT_XmlElementNode* element = node->AsElementNode()) {
        const NPT_String& prefix = element->GetPrefix();
        const NPT_String& tag    = element->GetTag();

        // process namespaces
        const NPT_String* namespace_uri = element->GetNamespace();
        const NPT_String* rendered = GetNamespaceRenderedForPrefix(prefix);
        if (namespace_uri && namespace_uri->IsEmpty()) namespace_uri = NULL;
        if (prefix.IsEmpty()) {
            // default namespace
            if (rendered == NULL) {
                // default namespace not rendered
                if (namespace_uri) {
                    map_link.m_RenderedNamespaces.Put("", *namespace_uri);
                }
            } else {
                // default namespace already rendered
                const char* compare;
                if (namespace_uri) {
                    compare = namespace_uri->GetChars();
                } else {
                    compare = "";
                }
                if (*rendered != compare) {
                    // the rendered default namespace had a different uri
                    map_link.m_RenderedNamespaces.Put("", compare);
                } 
            }
        } else {
            // explicit namespace
            // NOTE: namespace_uri should not be an empty string, but we test just
            // in case the XML document is not compliant
            if (namespace_uri && (rendered == NULL || *rendered != *namespace_uri)) {
                // namespace prefix not rendered or rendered with a different value
                map_link.m_RenderedNamespaces.Put(prefix, *namespace_uri);
            }
        }

        // process attributes
        SortedAttributeList prefixed_attributes;
        SortedAttributeList naked_attributes;
        for (NPT_List<NPT_XmlAttribute*>::Iterator attribute = element->GetAttributes().GetFirstItem();
             attribute;
             ++attribute) {
             const NPT_String& a_prefix = (*attribute)->GetPrefix();
             if (a_prefix.IsEmpty()) {
                 // naked attribute
                 naked_attributes.Add(NULL, *attribute);
             } else {
                // decide if we need to render this namespace declaration
                namespace_uri = element->GetNamespaceUri(a_prefix);
                if (namespace_uri) {
                    rendered = GetNamespaceRenderedForPrefix(a_prefix);;
                    if (rendered == NULL || *rendered != *namespace_uri) {
                        // namespace not rendered or rendered with a different value
                        map_link.m_RenderedNamespaces.Put(a_prefix, *namespace_uri);
                    }
                    prefixed_attributes.Add(namespace_uri, *attribute);
                }
             }
        }

        // start of element
        m_Serializer.StartElement(prefix, tag);
                    
        // namespace declarations
        if (map_link.m_RenderedNamespaces.GetEntryCount()) {
            SortedNamespaceList namespaces;
            NPT_List<NPT_Map<NPT_String, NPT_String>::Entry*>::Iterator entry = 
                map_link.m_RenderedNamespaces.GetEntries().GetFirstItem();
            while (entry) {
                const NPT_String& key   = (*entry)->GetKey();
                const NPT_String& value = (*entry)->GetValue();
                namespaces.Add(&key, &value);
                ++entry;
            }
            namespaces.Emit(m_Serializer);
        }

        // attributes
        naked_attributes.Emit(m_Serializer);
        prefixed_attributes.Emit(m_Serializer);

        // children
        MapChainLink* chain;
        if (map_link.m_RenderedNamespaces.GetEntryCount()) {
            chain = &map_link;
        } else {
            chain = m_MapChain;
        }
        element->GetChildren().Apply(NPT_XmlNodeCanonicalWriter(m_Serializer, chain));

        // end of element
        m_Serializer.EndElement(prefix, tag);
    } else if (NPT_XmlTextNode* text = node->AsTextNode()) {
        m_Serializer.Text(text->GetString());
    }
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::NPT_XmlSerializer
+---------------------------------------------------------------------*/
NPT_XmlSerializer::NPT_XmlSerializer(NPT_OutputStream* output,
                                     NPT_Cardinal      indentation,
                                     bool              shrink_empty_elements,
                                     bool              add_xml_decl) :
    m_Output(output),
    m_ElementPending(false),
    m_Depth(0),
    m_Indentation(indentation),
    m_ElementHasText(false),
    m_ShrinkEmptyElements(shrink_empty_elements),
    m_AddXmlDecl(add_xml_decl)
{
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::~NPT_XmlSerializer
+---------------------------------------------------------------------*/
NPT_XmlSerializer::~NPT_XmlSerializer()
{
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::StartDocument
+---------------------------------------------------------------------*/
NPT_Result 
NPT_XmlSerializer::StartDocument()
{
    if (!m_AddXmlDecl) return NPT_SUCCESS;

    return m_Output->WriteString("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n");
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::EndDocument
+---------------------------------------------------------------------*/
NPT_Result 
NPT_XmlSerializer::EndDocument()
{
    return m_ElementPending?NPT_ERROR_INVALID_STATE:NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::EscapeChar
+---------------------------------------------------------------------*/
void  
NPT_XmlSerializer::EscapeChar(unsigned char c, char* text)
{
    *text++ = '&';
    *text++ = '#';
    *text++ = 'x';
    int c0 = c>>4;
    int c1 = c&0xF;
    if (c0) {
        *text++ = c0 >= 10 ? 'A'+(c0-10) : '0'+c0;
    }
    *text++ = c1 >= 10 ? 'A'+(c1-10) : '0'+c1;
    *text++ = ';';
    *text   = '\0';
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::ProcessPending
+---------------------------------------------------------------------*/
NPT_Result  
NPT_XmlSerializer::ProcessPending()
{
    if (!m_ElementPending) return NPT_SUCCESS;
    m_ElementPending = false;
    return m_Output->Write(">", 1);
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::OutputEscapedString
+---------------------------------------------------------------------*/
NPT_Result  
NPT_XmlSerializer::OutputEscapedString(const char* text, bool attribute)
{
    const char* start = text;
    char escaped[7];
    while (char c = *text) {
        const char* insert = NULL;
        switch (c) {
            case '\r': {
                EscapeChar(c, escaped);
                insert = escaped;
                break;
            }
            case '\n':
            case '\t':
                if (attribute) {
                    EscapeChar(c, escaped);
                    insert = escaped;
                }
                break;

            case '&' : insert = "&amp;"; break;
            case '<' : insert = "&lt;";  break;
            case '>' : if (!attribute) insert = "&gt;";  break;
            case '"' : if (attribute) insert = "&quot;"; break;
            default : 
                break;
        }
        if (insert) {
            // output pending chars
            if (start != text) m_Output->WriteFully(start, (NPT_Size)(text-start));
            m_Output->WriteString(insert);
            start = ++text;
        } else {
            ++text;
        }
    }
    if (start != text) {
        m_Output->WriteFully(start, (NPT_Size)(text-start));
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::OutputIndentation
+---------------------------------------------------------------------*/
void
NPT_XmlSerializer::OutputIndentation(bool start)
{
    if (m_Depth || !start) m_Output->Write("\r\n", 2);

    // ensure we have enough chars in the prefix string
    unsigned int prefix_length = m_Indentation*m_Depth;
    if (m_IndentationPrefix.GetLength() < prefix_length) {
        unsigned int needed = prefix_length-m_IndentationPrefix.GetLength();
        for (unsigned int i=0; i<needed; i+=16) {
            m_IndentationPrefix.Append("                ", 16);
        }
    }

    // print the indentation prefix
    m_Output->WriteFully(m_IndentationPrefix.GetChars(), prefix_length);
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::StartElement
+---------------------------------------------------------------------*/
NPT_Result  
NPT_XmlSerializer::StartElement(const char* prefix, const char* name)
{
    ProcessPending();
    if (m_Indentation) OutputIndentation(true);
    m_ElementPending = true;
    m_ElementHasText = false;
    m_Depth++;
    m_Output->Write("<", 1);
    if (prefix && prefix[0]) {
        m_Output->WriteString(prefix);
        m_Output->Write(":", 1);
    }
    return m_Output->WriteString(name);
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::EndElement
+---------------------------------------------------------------------*/
NPT_Result  
NPT_XmlSerializer::EndElement(const char* prefix, const char* name)
{
    m_Depth--;

    if (m_ElementPending) {
        // this element has no children
        m_ElementPending = false;
        if (m_ShrinkEmptyElements) {
            return m_Output->WriteFully("/>", 2);
        } else {
            m_Output->Write(">",1);
        }
    } 

    if (m_Indentation && !m_ElementHasText) OutputIndentation(false);
    m_ElementHasText = false;
    m_Output->WriteFully("</", 2);
    if (prefix && prefix[0]) {
        m_Output->WriteString(prefix);
        m_Output->Write(":", 1);
    }
    m_Output->WriteString(name);
    return m_Output->Write(">", 1);
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::Attribute
+---------------------------------------------------------------------*/
NPT_Result  
NPT_XmlSerializer::Attribute(const char* prefix, const char* name, const char* value)
{
    m_Output->Write(" ", 1);
    if (prefix && prefix[0]) {
        m_Output->WriteString(prefix);
        m_Output->Write(":", 1);
    }
    m_Output->WriteString(name);
    m_Output->WriteFully("=\"", 2);
    OutputEscapedString(value, true);
    return m_Output->Write("\"", 1);
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::Text
+---------------------------------------------------------------------*/
NPT_Result  
NPT_XmlSerializer::Text(const char* text)
{
    ProcessPending();
    m_ElementHasText = true;
    return OutputEscapedString(text, false);
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::CdataSection
+---------------------------------------------------------------------*/
NPT_Result  
NPT_XmlSerializer::CdataSection(const char* data)
{
    ProcessPending();
    m_ElementHasText = true;
    m_Output->WriteFully("<![CDATA[", 9);
    m_Output->WriteString(data);
    return m_Output->WriteFully("]]>", 3);
}

/*----------------------------------------------------------------------
|   NPT_XmlSerializer::Comment
+---------------------------------------------------------------------*/
NPT_Result  
NPT_XmlSerializer::Comment(const char* comment)
{
    ProcessPending();
    m_Output->WriteFully("<!--", 4);
    m_Output->WriteString(comment);
    return m_Output->WriteFully("-->", 3);
}

/*----------------------------------------------------------------------
|   NPT_XmlWriter::Serialize
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlWriter::Serialize(NPT_XmlNode&      node, 
                         NPT_OutputStream& output, 
                         bool              add_xml_decl)
{
    NPT_XmlSerializer serializer(&output, m_Indentation, true, add_xml_decl);
    NPT_XmlNodeWriter node_writer(serializer);
    NPT_XmlNode* node_pointer = &node;
    node_writer(node_pointer);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_XmlCanonicalizer::Serialize
+---------------------------------------------------------------------*/
NPT_Result
NPT_XmlCanonicalizer::Serialize(NPT_XmlNode&      node, 
                                NPT_OutputStream& output, 
                                bool              add_xml_decl)
{
    // create a serializer with no indentation and no shrinking of empty elements
    NPT_XmlSerializer serializer(&output, 0, false, add_xml_decl);

    // serialize the node
    NPT_XmlNodeCanonicalWriter node_writer(serializer);
    NPT_XmlNode* node_pointer = &node;
    node_writer(node_pointer);

    return NPT_SUCCESS;
}
