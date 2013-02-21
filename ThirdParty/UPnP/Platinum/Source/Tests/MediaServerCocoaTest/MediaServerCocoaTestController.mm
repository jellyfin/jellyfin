//
//  MediaServerCocoaTestController.mm
//  Platinum
//
//  Created by Sylvain on 9/14/10.
//  Copyright 2010 Plutinosoft LLC. All rights reserved.
//

#import "MediaServerCocoaTestController.h"
#import "Neptune.h"


@implementation MediaServerCocoaTestController

+ (void)initialize {
    NPT_LogManager::GetDefault().Configure("plist:.level=INFO;.handlers=ConsoleHandler;.ConsoleHandler.outputs=1;.ConsoleHandler.filter=61");
}

- (void)awakeFromNib {
    upnp = [[PLT_UPnPObject alloc] init];
    
    // create server and add ourselves as the delegate
    PLT_MediaServerObject* server = [[PLT_MediaServerObject alloc] init];
    [server setDelegate:self];
    [upnp addDevice:server];
    
    [mainButton setTarget:self];
    [mainButton setTitle:@"Start"];
    [mainButton setAction:@selector(performUPnPStarStop:)];
}

- (void)performUPnPStarStop:(id)sender {
    if ([upnp isRunning]) {
        [upnp stop];
        [mainButton setTitle:@"Start"];
    } else {
        [upnp start];
        [mainButton setTitle:@"Stop"];
    }
}

- (void)dealloc {
    [upnp release];
    [super dealloc];
}

#pragma mark PLT_MediaServerDelegateObject
- (NPT_Result)onBrowseMetadata:(PLT_MediaServerBrowseCapsule*)info
{
    return NPT_FAILURE;
}

- (NPT_Result)onBrowseDirectChildren:(PLT_MediaServerBrowseCapsule*)info
{
    return NPT_FAILURE;
}

- (NPT_Result)onSearchContainer:(PLT_MediaServerSearchCapsule*)info
{
    return NPT_FAILURE;
}

- (NPT_Result)onFileRequest:(PLT_MediaServerFileRequestCapsule*)info
{
    return NPT_FAILURE;
}

@end
