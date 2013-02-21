/*****************************************************************
|
|   Platinum - File Media Server Delegate
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
#include "PltMediaItem.h"
#include "PltService.h"
#include "PltTaskManager.h"
#include "PltHttpServer.h"
#include "PltDidl.h"
#include "PltVersion.h"
#include "PltMimeType.h"

NPT_SET_LOCAL_LOGGER("platinum.media.server.file.delegate")

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::PLT_FileMediaServerDelegate
+---------------------------------------------------------------------*/
PLT_FileMediaServerDelegate::PLT_FileMediaServerDelegate(const char* url_root, 
                                                         const char* file_root) :
    m_UrlRoot(url_root),
    m_FileRoot(file_root),
    m_FilterUnknownOut(false)
{
    /* Trim excess separators */
    m_FileRoot.TrimRight("/\\");
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::~PLT_FileMediaServerDelegate
+---------------------------------------------------------------------*/
PLT_FileMediaServerDelegate::~PLT_FileMediaServerDelegate()
{
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::ProcessFileRequest
+---------------------------------------------------------------------*/
NPT_Result 
PLT_FileMediaServerDelegate::ProcessFileRequest(NPT_HttpRequest&              request, 
                                                const NPT_HttpRequestContext& context,
                                                NPT_HttpResponse&             response)
{
    NPT_HttpUrlQuery query(request.GetUrl().GetQuery());
    
    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINE, "PLT_FileMediaServerDelegate::ProcessFileRequest:", &request);
    
    if (request.GetMethod().Compare("GET") && request.GetMethod().Compare("HEAD")) {
        response.SetStatus(500, "Internal Server Error");
        return NPT_SUCCESS;
    }
    
    /* Extract file path from url */
    NPT_String file_path;
    NPT_CHECK_LABEL_WARNING(ExtractResourcePath(request.GetUrl(), file_path), failure);
    
    /* Serve file */
    NPT_CHECK_WARNING(ServeFile(request, context, response, NPT_FilePath::Create(m_FileRoot, file_path)));
    return NPT_SUCCESS;
    
failure:
    response.SetStatus(404, "File Not Found");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::ServeFile
+---------------------------------------------------------------------*/
NPT_Result 
PLT_FileMediaServerDelegate::ServeFile(const NPT_HttpRequest&        request, 
                                       const NPT_HttpRequestContext& context,
                                       NPT_HttpResponse&             response,
                                       const NPT_String&             file_path)
{
    NPT_CHECK_WARNING(PLT_HttpServer::ServeFile(request, context, response, file_path));
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::OnBrowseMetadata
+---------------------------------------------------------------------*/
NPT_Result
PLT_FileMediaServerDelegate::OnBrowseMetadata(PLT_ActionReference&          action, 
                                              const char*                   object_id, 
                                              const char*                   filter,
                                              NPT_UInt32                    starting_index,
                                              NPT_UInt32                    requested_count,
                                              const char*                   sort_criteria,
                                              const PLT_HttpRequestContext& context)
{
    NPT_COMPILER_UNUSED(sort_criteria);
    NPT_COMPILER_UNUSED(requested_count);
    NPT_COMPILER_UNUSED(starting_index);
    
    NPT_String didl;
    PLT_MediaObjectReference item;
    
    /* locate the file from the object ID */
    NPT_String filepath;
    if (NPT_FAILED(GetFilePath(object_id, filepath))) {
        /* error */
        NPT_LOG_WARNING("PLT_FileMediaServerDelegate::OnBrowse - ObjectID not found.");
        action->SetError(701, "No Such Object.");
        return NPT_FAILURE;
    }
    
    /* build the object didl */
    item = BuildFromFilePath(filepath, context, true, false, (NPT_String(filter).Find("ALLIP")!=-1));
    if (item.IsNull()) return NPT_FAILURE;
    NPT_String tmp;    
    NPT_CHECK_SEVERE(PLT_Didl::ToDidl(*item.AsPointer(), filter, tmp));
    
    /* add didl header and footer */
    didl = didl_header + tmp + didl_footer;
    
    NPT_CHECK_SEVERE(action->SetArgumentValue("Result", didl));
    NPT_CHECK_SEVERE(action->SetArgumentValue("NumberReturned", "1"));
    NPT_CHECK_SEVERE(action->SetArgumentValue("TotalMatches", "1"));
    
    /* update ID may be wrong here, it should be the one of the container?
       TODO: We need to keep track of the overall updateID of the CDS */
    NPT_CHECK_SEVERE(action->SetArgumentValue("UpdateId", "1"));
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::OnBrowseDirectChildren
+---------------------------------------------------------------------*/
NPT_Result
PLT_FileMediaServerDelegate::OnBrowseDirectChildren(PLT_ActionReference&          action, 
                                                    const char*                   object_id, 
                                                    const char*                   filter,
                                                    NPT_UInt32                    starting_index,
                                                    NPT_UInt32                    requested_count,
                                                    const char*                   sort_criteria,
                                                    const PLT_HttpRequestContext& context)
{
    NPT_COMPILER_UNUSED(sort_criteria);
    
    /* locate the file from the object ID */
    NPT_String dir;
    NPT_FileInfo info;
    if (NPT_FAILED(GetFilePath(object_id, dir)) || 
        NPT_FAILED(NPT_File::GetInfo(dir, &info)) ||
        info.m_Type != NPT_FileInfo::FILE_TYPE_DIRECTORY) {
        /* error */
        NPT_LOG_WARNING_1("ObjectID \'%s\' not found or not allowed", object_id);
        action->SetError(701, "No such Object");
        NPT_CHECK_WARNING(NPT_FAILURE);
    }
    
    /* get uuid from device via action reference */
    NPT_String uuid = action->GetActionDesc().GetService()->GetDevice()->GetUUID();
    
    /* Try to get list from cache */
    NPT_Result res;
    NPT_Reference<NPT_List<NPT_String> > entries;
    NPT_TimeStamp cached_entries_time;
    if (NPT_FAILED(m_DirCache.Get(uuid, dir, entries, &cached_entries_time)) ||
        cached_entries_time < info.m_ModificationTime) {
        /* if not found in cache or if current dir has newer modified time
           fetch fresh new list from source */
        
        entries = new NPT_List<NPT_String>();
        res = NPT_File::ListDir(dir, *entries);
        if (NPT_FAILED(res)) {
            NPT_LOG_WARNING_1("PLT_FileMediaServerDelegate::OnBrowseDirectChildren - failed to open dir %s", (const char*) dir);
            NPT_CHECK_WARNING(res);
        }
        
        /* sort results according to modification date */
        res = entries->Sort(NPT_FileDateComparator(dir));
        if (NPT_FAILED(res)) {
            NPT_LOG_WARNING_1("PLT_FileMediaServerDelegate::OnBrowseDirectChildren - failed to open sort dir %s", (const char*) dir);
            NPT_CHECK_WARNING(res);
        }
        
        /* add new list to cache */
        m_DirCache.Put(uuid, dir, entries, &info.m_ModificationTime);
    }
    
    unsigned long cur_index = 0;
    unsigned long num_returned = 0;
    unsigned long total_matches = 0;
    NPT_String didl = didl_header;
    bool allip = (NPT_String(filter).Find("ALLIP") != -1);
    
    PLT_MediaObjectReference item;
    for (NPT_List<NPT_String>::Iterator it = entries->GetFirstItem();
         it;
         ++it) {
        NPT_String filepath = NPT_FilePath::Create(dir, *it);
        
        /* verify we want to process this file first */
        if (!ProcessFile(filepath, filter)) continue;
        
        /* build item object from file path */
        item = BuildFromFilePath(filepath, 
                                 context,
                                 true,
                                 false,
                                 allip);
        
        /* generate didl if within range requested */
        if (!item.IsNull()) {
            if ((cur_index >= starting_index) && 
                ((num_returned < requested_count) || (requested_count == 0))) {
                NPT_String tmp;
                NPT_CHECK_SEVERE(PLT_Didl::ToDidl(*item.AsPointer(), filter, tmp));
                
                didl += tmp;
                ++num_returned;
            }
            ++cur_index;
            ++total_matches;        
        }
    };
    
    didl += didl_footer;
    
    NPT_LOG_FINE_6("BrowseDirectChildren from %s returning %d-%d/%d objects (%d out of %d requested)",
                   (const char*)context.GetLocalAddress().GetIpAddress().ToString(),
                   starting_index, starting_index+num_returned, total_matches, num_returned, requested_count);
    
    NPT_CHECK_SEVERE(action->SetArgumentValue("Result", didl));
    NPT_CHECK_SEVERE(action->SetArgumentValue("NumberReturned", NPT_String::FromInteger(num_returned)));
    NPT_CHECK_SEVERE(action->SetArgumentValue("TotalMatches", NPT_String::FromInteger(total_matches))); // 0 means we don't know how many we have but most browsers don't like that!!
    NPT_CHECK_SEVERE(action->SetArgumentValue("UpdateId", "1"));
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::OnSearchContainer
+---------------------------------------------------------------------*/
NPT_Result
PLT_FileMediaServerDelegate::OnSearchContainer(PLT_ActionReference&          action, 
                                               const char*                   object_id, 
                                               const char*                   search_criteria,
                                               const char*                   /* filter */,
                                               NPT_UInt32                    /* starting_index */,
                                               NPT_UInt32                    /* requested_count */,
                                               const char*                   /* sort_criteria */,
                                               const PLT_HttpRequestContext& /* context */)
{
    /* parse search criteria */
    
    /* TODO: HACK TO PASS DLNA */
    if (search_criteria && NPT_StringsEqual(search_criteria, "Unknownfieldname")) {
        /* error */
        NPT_LOG_WARNING_1("Unsupported or invalid search criteria %s", search_criteria);
        action->SetError(708, "Unsupported or invalid search criteria");
        return NPT_FAILURE;
    }
    
    /* locate the file from the object ID */
    NPT_String dir;
    if (NPT_FAILED(GetFilePath(object_id, dir))) {
        /* error */
        NPT_LOG_WARNING("ObjectID not found.");
        action->SetError(710, "No Such Container.");
        return NPT_FAILURE;
    }
    
    /* retrieve the item type */
    NPT_FileInfo info;
    NPT_Result res = NPT_File::GetInfo(dir, &info);
    if (NPT_FAILED(res) || (info.m_Type != NPT_FileInfo::FILE_TYPE_DIRECTORY)) {
        /* error */
        NPT_LOG_WARNING("No such container");
        action->SetError(710, "No such container");
        return NPT_FAILURE;
    }
    
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::GetFilePath
+---------------------------------------------------------------------*/
NPT_Result
PLT_FileMediaServerDelegate::GetFilePath(const char* object_id, 
                                         NPT_String& filepath) 
{
    if (!object_id) return NPT_ERROR_INVALID_PARAMETERS;
    
    filepath = m_FileRoot;
    
    /* object id is formatted as 0/<filepath> */
    if (NPT_StringLength(object_id) >= 1) {
        filepath += (object_id + (object_id[0]=='0'?1:0));
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::BuildSafeResourceUri
+---------------------------------------------------------------------*/
NPT_String
PLT_FileMediaServerDelegate::BuildSafeResourceUri(const NPT_HttpUrl& base_uri, 
                                                  const char*        host, 
                                                  const char*        file_path)
{
    NPT_String result;
    NPT_HttpUrl uri = base_uri;
    
    if (host) uri.SetHost(host);
    
    NPT_String uri_path = uri.GetPath();
    if (!uri_path.EndsWith("/")) uri_path += "/";

	/* some controllers (like WMP) will call us with an already urldecoded version.
       We're intentionally prepending a known urlencoded string
       to detect it when we receive the request urlencoded or already decoded to avoid double decoding*/
    uri_path += "%/";
    uri_path += file_path;
    
    /* set path */
    uri.SetPath(uri_path);
    
    /* 360 hack: force inclusion of port in case it's 80 */
    return uri.ToStringWithDefaultPort(0);
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::ExtractResourcePath
+---------------------------------------------------------------------*/
NPT_Result
PLT_FileMediaServerDelegate::ExtractResourcePath(const NPT_HttpUrl& url, 
                                                 NPT_String&        file_path)
{
    /* Extract non decoded path, we need to autodetect urlencoding */
    NPT_String uri_path = url.GetPath();
    NPT_String url_root_encode = NPT_Uri::PercentEncode(m_UrlRoot, NPT_Uri::PathCharsToEncode);
    
    NPT_Ordinal skip = 0;
    if (uri_path.StartsWith(m_UrlRoot)) {
        skip = m_UrlRoot.GetLength();
    } else if (uri_path.StartsWith(url_root_encode)) {
        skip = url_root_encode.GetLength();
    } else {
        return NPT_FAILURE;
    }
    
    /* account for extra slash */
    skip += ((m_UrlRoot=="/")?0:1);
    file_path = uri_path.SubString(skip);
    
    /* detect if client such as WMP sent a non urlencoded url */
    if (file_path.StartsWith("%/")) {
        NPT_LOG_FINE("Received a urldecoded version of our url!");
        file_path.Erase(0, 2);
    } else {
        /* remove our prepended string we used to detect urldecoded version */
        if (file_path.StartsWith("%25/")) file_path.Erase(0, 4);

        /* ok to urldecode */
        file_path = NPT_Uri::PercentDecode(file_path);
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::BuildResourceUri
+---------------------------------------------------------------------*/
NPT_String
PLT_FileMediaServerDelegate::BuildResourceUri(const NPT_HttpUrl& base_uri, 
                                              const char*        host, 
                                              const char*        file_path)
{
    return BuildSafeResourceUri(base_uri, host, file_path);
}

/*----------------------------------------------------------------------
|   PLT_FileMediaServerDelegate::BuildFromFilePath
+---------------------------------------------------------------------*/
PLT_MediaObject*
PLT_FileMediaServerDelegate::BuildFromFilePath(const NPT_String&             filepath, 
                                               const PLT_HttpRequestContext& context,
                                               bool                          with_count /* = true */,
                                               bool                          keep_extension_in_title /* = false */,
                                               bool                          allip /* = false */)
{
    NPT_String            root = m_FileRoot;
    PLT_MediaItemResource resource;
    PLT_MediaObject*      object = NULL;
    
    NPT_LOG_FINEST_1("Building didl for file '%s'", (const char*)filepath);
    
    /* retrieve the entry type (directory or file) */
    NPT_FileInfo info; 
    NPT_CHECK_LABEL_FATAL(NPT_File::GetInfo(filepath, &info), failure);
    
    if (info.m_Type == NPT_FileInfo::FILE_TYPE_REGULAR) {
        object = new PLT_MediaItem();
        
        /* Set the title using the filename for now */
        object->m_Title = NPT_FilePath::BaseName(filepath, keep_extension_in_title);
        if (object->m_Title.GetLength() == 0) goto failure;
        
        /* make sure we return something with a valid mimetype */
        if (m_FilterUnknownOut && 
            NPT_StringsEqual(PLT_MimeType::GetMimeType(filepath, &context), 
                             "application/octet-stream")) {
            goto failure;
        }
        
        /* Set the protocol Info from the extension */
        resource.m_ProtocolInfo = PLT_ProtocolInfo::GetProtocolInfo(filepath, true, &context);
        if (!resource.m_ProtocolInfo.IsValid())  goto failure;
        
        /* Set the resource file size */
        resource.m_Size = info.m_Size;
        
        /* format the resource URI */
        NPT_String url = filepath.SubString(root.GetLength()+1);
        
        // get list of ip addresses
        NPT_List<NPT_IpAddress> ips;
        NPT_CHECK_LABEL_SEVERE(PLT_UPnPMessageHelper::GetIPAddresses(ips), failure);
        
        /* if we're passed an interface where we received the request from
           move the ip to the top so that it is used for the first resource */
        if (context.GetLocalAddress().GetIpAddress().ToString() != "0.0.0.0") {
            ips.Remove(context.GetLocalAddress().GetIpAddress());
            ips.Insert(ips.GetFirstItem(), context.GetLocalAddress().GetIpAddress());
        } else if (!allip) {
            NPT_LOG_WARNING("Couldn't determine local interface IP so we might return an unreachable IP");
        }
        object->m_ObjectClass.type = PLT_MediaItem::GetUPnPClass(filepath, &context);
        
        /* add as many resources as we have interfaces s*/
        NPT_HttpUrl base_uri("127.0.0.1", context.GetLocalAddress().GetPort(), NPT_HttpUrl::PercentEncode(m_UrlRoot, NPT_Uri::PathCharsToEncode));
        NPT_List<NPT_IpAddress>::Iterator ip = ips.GetFirstItem();        
        while (ip) {
            resource.m_Uri = BuildResourceUri(base_uri, ip->ToString(), url);
            object->m_Resources.Add(resource);
            ++ip;
            
            /* if we only want the one resource reachable by client */
            if (!allip) break;
        }
    } else {
        object = new PLT_MediaContainer;
        
        /* Assign a title for this container */
        if (filepath.Compare(root, true) == 0) {
            object->m_Title = "Root";
        } else {
            object->m_Title = NPT_FilePath::BaseName(filepath, keep_extension_in_title);
            if (object->m_Title.GetLength() == 0) goto failure;
        }
        
        /* Get the number of children for this container */
        NPT_LargeSize count = 0;
        if (with_count && NPT_SUCCEEDED(NPT_File::GetSize(filepath, count))) {
            ((PLT_MediaContainer*)object)->m_ChildrenCount = (NPT_Int32)count;
        }
        
        object->m_ObjectClass.type = "object.container.storageFolder";
    }
    
    /* is it the root? */
    if (filepath.Compare(root, true) == 0) {
        object->m_ParentID = "-1";
        object->m_ObjectID = "0";
    } else {
        NPT_String directory = NPT_FilePath::DirName(filepath);
        /* is the parent path the root? */
        if (directory.GetLength() == root.GetLength()) {
            object->m_ParentID = "0";
        } else {
            object->m_ParentID = "0" + filepath.SubString(root.GetLength(), directory.GetLength() - root.GetLength());
        }
        object->m_ObjectID = "0" + filepath.SubString(root.GetLength());
    }
    
    return object;
    
failure:
    delete object;
    return NULL;
}
