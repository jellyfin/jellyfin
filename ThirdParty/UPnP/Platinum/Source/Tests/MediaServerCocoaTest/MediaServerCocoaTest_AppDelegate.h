//
//  MediaServerCocoaTest_AppDelegate.h
//  MediaServerCocoaTest
//
//  Created by Sylvain on 9/14/10.
//  Copyright Plutinosoft LLC 2010 . All rights reserved.
//

#import <Cocoa/Cocoa.h>

@interface MediaServerCocoaTest_AppDelegate : NSObject 
{
    NSWindow *window;
    
    NSPersistentStoreCoordinator *persistentStoreCoordinator;
    NSManagedObjectModel         *managedObjectModel;
    NSManagedObjectContext       *managedObjectContext;
}

@property (nonatomic, retain) IBOutlet NSWindow *window;

@property (nonatomic, retain, readonly) NSPersistentStoreCoordinator *persistentStoreCoordinator;
@property (nonatomic, retain, readonly) NSManagedObjectModel *managedObjectModel;
@property (nonatomic, retain, readonly) NSManagedObjectContext *managedObjectContext;

- (IBAction)saveAction:sender;

@end
