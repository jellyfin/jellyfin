//
//  PltUPnPObject.mm
//  Platinum
//
//  Created by Sylvain on 9/14/10.
//  Copyright 2010 Plutinosoft LLC. All rights reserved.
//

#import "Platinum.h"
#import "PltUPnPObject.h"

/*----------------------------------------------------------------------
|   PLT_ActionObject
+---------------------------------------------------------------------*/
/*@interface PLT_ActionObject (priv)
- (id)initWithAction:(PLT_Action*)action;
@end*/

@implementation PLT_ActionObject

- (id)initWithAction:(PLT_Action *)_action
{
    if ((self = [super init])) {
        action = _action;
    }
    return self;
}

- (void)dealloc
{
    [super dealloc];
}

- (NPT_Result)setValue:(NSString *)value forArgument:(NSString *)argument
{
    return action->SetArgumentValue([argument UTF8String], [value UTF8String]);
}

- (NPT_Result)setErrorCode:(unsigned int)code withDescription:(NSString*)description
{
    return action->SetError(code, [description UTF8String]);
}

@end

/*----------------------------------------------------------------------
|   PLT_DeviceHostObject
+---------------------------------------------------------------------*/
@interface PLT_DeviceHostObject (priv)
- (PLT_DeviceHostReference&)getDevice;
@end

@implementation PLT_DeviceHostObject

- (id)initWithDeviceHost:(PLT_DeviceHostReference*)_device
{
    if ((self = [super init])) {
        device = new PLT_DeviceHostReference(*_device);
    }
    return self;
}

- (void)dealloc
{
    delete device;
    [super dealloc];
}

- (PLT_DeviceHostReference&)getDevice 
{
    return *device;
}

@end

/*----------------------------------------------------------------------
|   PLT_UPnPObject
+---------------------------------------------------------------------*/
@implementation PLT_UPnPObject

- (id)init
{
    if ((self = [super init])) {
        upnp = new PLT_UPnP();
    }
    return self;
}

-(void) dealloc
{
    delete upnp;
    [super dealloc];
}

- (NPT_Result)start
{
    return upnp->Start();
}

- (NPT_Result)stop
{
    return upnp->Stop();
}

- (bool)isRunning
{
    return upnp->IsRunning();
}

- (NPT_Result)addDevice:(PLT_DeviceHostObject*)device
{
    return upnp->AddDevice([device getDevice]);
}

- (NPT_Result)removeDevice:(PLT_DeviceHostObject*)device
{
    return upnp->RemoveDevice([device getDevice]);
}

@end
