//
//  PltMediaServerObject.mm
//  Platinum
//
//  Created by Sylvain on 9/14/10.
//  Copyright 2010 Plutinosoft LLC. All rights reserved.
//

#import "Platinum.h"
#import "PltMediaServerObject.h"
#import "PltMediaConnect.h"

/*----------------------------------------------------------------------
|   PLT_MediaServerDelegate_Wrapper
+---------------------------------------------------------------------*/
class PLT_MediaServerDelegate_Wrapper : public PLT_MediaServerDelegate {
public:
    PLT_MediaServerDelegate_Wrapper(PLT_MediaServerObject* target) : m_Target(target) {}
    
    NPT_Result OnBrowseMetadata(PLT_ActionReference&          action, 
                                const char*                   object_id, 
                                const char*                   filter,
                                NPT_UInt32                    starting_index,
                                NPT_UInt32                    requested_count,
                                const char*                   sort_criteria,
                                const PLT_HttpRequestContext& context) {
        if (![[m_Target delegate] respondsToSelector:@selector(onBrowseMetadata:)]) 
            return NPT_FAILURE;
        
        PLT_MediaServerBrowseCapsule* capsule = 
            [[PLT_MediaServerBrowseCapsule alloc] initWithAction:action.AsPointer()
                                                        objectId:object_id
                                                          filter:filter
                                                           start:starting_index
                                                           count:requested_count
                                                            sort:sort_criteria
                                                         context:(PLT_HttpRequestContext*)&context];
        NPT_Result result = [[m_Target delegate] onBrowseMetadata:capsule];
        [capsule release];
        
        return result;
    }
    
    NPT_Result OnBrowseDirectChildren(PLT_ActionReference&          action, 
                                      const char*                   object_id, 
                                      const char*                   filter,
                                      NPT_UInt32                    starting_index,
                                      NPT_UInt32                    requested_count,
                                      const char*                   sort_criteria, 
                                      const PLT_HttpRequestContext& context) {
        if (![[m_Target delegate] respondsToSelector:@selector(onBrowseDirectChildren:)]) 
            return NPT_FAILURE;
        
        PLT_MediaServerBrowseCapsule* capsule = 
            [[PLT_MediaServerBrowseCapsule alloc] initWithAction:action.AsPointer()
                                                        objectId:object_id
                                                          filter:filter
                                                           start:starting_index
                                                           count:requested_count
                                                            sort:sort_criteria
                                                         context:(PLT_HttpRequestContext*)&context];
        NPT_Result result = [[m_Target delegate] onBrowseDirectChildren:capsule];
        [capsule release];
        
        return result;
    }
    
    NPT_Result OnSearchContainer(PLT_ActionReference&          action, 
                                 const char*                   container_id, 
                                 const char*                   search_criteria,
                                 const char*                   filter,
                                 NPT_UInt32                    starting_index,
                                 NPT_UInt32                    requested_count,
                                 const char*                   sort_criteria, 
                                 const PLT_HttpRequestContext& context) {
        if (![[m_Target delegate] respondsToSelector:@selector(onSearchContainer:)]) 
            return NPT_FAILURE;
        
        PLT_MediaServerSearchCapsule* capsule = 
            [[PLT_MediaServerSearchCapsule alloc] initWithAction:action.AsPointer()
                                                        objectId:container_id
                                                          search:search_criteria
                                                          filter:filter
                                                           start:starting_index
                                                           count:requested_count
                                                            sort:sort_criteria
                                                         context:(PLT_HttpRequestContext*)&context];
        NPT_Result result = [[m_Target delegate] onSearchContainer:capsule];
        [capsule release];
        
        return result;
    }
    
    NPT_Result ProcessFileRequest(NPT_HttpRequest&              request,
                                  const NPT_HttpRequestContext& context,
                                  NPT_HttpResponse&             response) {
        PLT_HttpRequestContext _context(request, context);
        PLT_MediaServerFileRequestCapsule* capsule = 
            [[PLT_MediaServerFileRequestCapsule alloc] initWithResponse:&response
                                                                context:&_context];
        NPT_Result result = [[m_Target delegate] onFileRequest:capsule];
        [capsule release];
        
        return result;
    }

private:
    PLT_MediaServerObject* m_Target;
};

/*----------------------------------------------------------------------
|   PLT_MediaServerBrowseCapsule
+---------------------------------------------------------------------*/
@implementation PLT_MediaServerBrowseCapsule

@synthesize objectId, filter, start, count, sort;

- (id)initWithAction:(PLT_Action*)action objectId:(const char*)_id filter:(const char*)_filter start:(NPT_UInt32)_start count:(NPT_UInt32)_count sort:(const char*)_sort context:(PLT_HttpRequestContext*)_context
{
    if ((self = [super initWithAction:action])) {
        objectId = [[NSString alloc] initWithCString:_id encoding:NSUTF8StringEncoding];
        filter   = [[NSString alloc] initWithCString:(_filter==NULL)?"":_filter
                                            encoding:NSUTF8StringEncoding];
        sort     = [[NSString alloc] initWithCString:(_sort==NULL)?"":_sort
                                            encoding:NSUTF8StringEncoding];
        start    = _start;
        count    = _count;
        context  = _context;
    }
    return self;
}

- (void)dealloc
{
    [objectId release];
    [filter release];
    [sort release];
    [super dealloc];
}

@end

/*----------------------------------------------------------------------
|   PLT_MediaServerSearchCapsule
+---------------------------------------------------------------------*/
@implementation PLT_MediaServerSearchCapsule

@synthesize search;

- (id)initWithAction:(PLT_Action*)action objectId:(const char*)_id search:(const char*)_search filter:(const char*)_filter start:(NPT_UInt32)_start count:(NPT_UInt32)_count sort:(const char*)_sort context:(PLT_HttpRequestContext*)_context
{
    if ((self = [super initWithAction:action
                             objectId:_id
                               filter:_filter
                                start:_start
                                count:_count
                                 sort:_sort
                              context:_context])) {
        search = [[NSString alloc] initWithCString:_search encoding:NSUTF8StringEncoding];
    }
    return self;
}

- (void)dealloc
{
    [search release];
    [super dealloc];
}

@end

/*----------------------------------------------------------------------
|   PLT_MediaServerFileRequestCapsule
+---------------------------------------------------------------------*/
@implementation PLT_MediaServerFileRequestCapsule

- (id)initWithResponse:(NPT_HttpResponse*)_response context:(PLT_HttpRequestContext*)_context
{
    if ((self = [super init])) {
        response = _response;
        context  = _context;
    }
    return self;
}

- (void)dealloc
{
    [super dealloc];
}

@end

/*----------------------------------------------------------------------
|   PLT_DeviceHostObject
+---------------------------------------------------------------------*/
@interface PLT_DeviceHostObject (priv)
- (PLT_DeviceHostReference&)getDevice;
@end

/*----------------------------------------------------------------------
|   PLT_MediaServerObject
+---------------------------------------------------------------------*/
@implementation PLT_MediaServerObject

@synthesize delegate;

- (id)init 
{
    PLT_MediaConnect* server = new PLT_MediaConnect("Test");
    PLT_DeviceHostReference _device(server);
    if ((self = [super initWithDeviceHost:&_device])) {
        server->SetDelegate(new PLT_MediaServerDelegate_Wrapper(self));
    }
    return self;
}

- (void)dealloc
{
    PLT_DeviceHostReference& host = [self getDevice];
    delete ((PLT_MediaServer*)host.AsPointer())->GetDelegate();
    [super dealloc];
}

@end
