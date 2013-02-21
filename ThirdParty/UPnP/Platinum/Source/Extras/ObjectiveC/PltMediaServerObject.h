//
//  PltMediaServerObject.h
//  Platinum
//
//  Created by Sylvain on 9/14/10.
//  Copyright 2010 Plutinosoft LLC. All rights reserved.
//

#import "NptConfig.h"

#if defined(TARGET_OS_IPHONE) && TARGET_OS_IPHONE
#include <UIKit/UIKit.h>
#else
#import <Cocoa/Cocoa.h>
#endif

#import "PltUPnPObject.h"

// define 
#if !defined(_PLATINUM_H_)
typedef class PLT_HttpRequestContext PLT_HttpRequestContext;
typedef class NPT_HttpResponse NPT_HttpResponse;
#endif

/*----------------------------------------------------------------------
|   PLT_MediaServerObject
+---------------------------------------------------------------------*/
@interface PLT_MediaServerObject : PLT_DeviceHostObject {
    id delegate;
}

@property (nonatomic, assign) id delegate; // we do not retain to avoid circular ref count
@end

/*----------------------------------------------------------------------
|   PLT_MediaServerBrowseCapsule
+---------------------------------------------------------------------*/
@interface PLT_MediaServerBrowseCapsule : PLT_ActionObject {
    NSString*               objectId;
    NPT_UInt32              start;
    NPT_UInt32              count;
    NSString*               filter;
    NSString*               sort;
    PLT_HttpRequestContext* context;
}

- (id)initWithAction:(PLT_Action*)action objectId:(const char*)objectId filter:(const char*)filter start:(NPT_UInt32)start count:(NPT_UInt32)count sort:(const char*)sort context:(PLT_HttpRequestContext*)context;

@property (readonly, assign) NSString* objectId;
@property (readonly) NPT_UInt32 start;
@property (readonly) NPT_UInt32 count;
@property (readonly, assign) NSString* filter;
@property (readonly, assign) NSString* sort;
@end

/*----------------------------------------------------------------------
|   PLT_MediaServerSearchCapsule
+---------------------------------------------------------------------*/
@interface PLT_MediaServerSearchCapsule : PLT_MediaServerBrowseCapsule {
    NSString* search;
}

- (id)initWithAction:(PLT_Action*)action objectId:(const char*)objectId search:(const char*)search filter:(const char*)filter start:(NPT_UInt32)start count:(NPT_UInt32)count sort:(const char*)sort context:(PLT_HttpRequestContext*)context;

@property (readonly, assign) NSString* search;
@end

/*----------------------------------------------------------------------
|   PLT_MediaServerFileRequestCapsule
+---------------------------------------------------------------------*/
@interface PLT_MediaServerFileRequestCapsule : NSObject {
    NPT_HttpResponse*       response;
    PLT_HttpRequestContext* context;
}

- (id)initWithResponse:(NPT_HttpResponse*)response context:(PLT_HttpRequestContext*)context;
@end

/*----------------------------------------------------------------------
|   PLT_MediaServerDelegateObject
+---------------------------------------------------------------------*/
@protocol PLT_MediaServerDelegateObject
- (NPT_Result)onBrowseMetadata:(PLT_MediaServerBrowseCapsule*)info;
- (NPT_Result)onBrowseDirectChildren:(PLT_MediaServerBrowseCapsule*)info;
- (NPT_Result)onSearchContainer:(PLT_MediaServerSearchCapsule*)info;
- (NPT_Result)onFileRequest:(PLT_MediaServerFileRequestCapsule*)info;
@end
