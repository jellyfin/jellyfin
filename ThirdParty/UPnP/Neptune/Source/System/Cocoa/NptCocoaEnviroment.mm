/*****************************************************************
|
|      Neptune - System Support: Cocoa Implementation
|
|      (c) 2002-2006 Gilles Boccon-Gibod
|      Author: Sylvain Rebaud (sylvain@rebaud.com)
|
****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptSystem.h"

#import <Foundation/Foundation.h>
#import <SystemConfiguration/SystemConfiguration.h>

#if defined(TARGET_OS_IPHONE) && TARGET_OS_IPHONE
#import <UIKit/UIKit.h>
#endif

NPT_Result
NPT_GetSystemMachineName(NPT_String& name)
{
    // we need a pool because of UTF8String
    NSAutoreleasePool* pool = [[NSAutoreleasePool alloc] init];
    
#if !defined(TARGET_OS_IPHONE) || !TARGET_OS_IPHONE
    CFStringRef _name = SCDynamicStoreCopyComputerName(NULL, NULL);
    name = [(NSString *)_name UTF8String];
    [(NSString *)_name release];
#else
    name = [[[UIDevice currentDevice] name] UTF8String];
#endif
    
    [pool release];
    return NPT_SUCCESS;
}
