iron-resizable-behavior
=======================

`IronResizableBehavior` is a behavior that can be used in Polymer elements to
coordinate the flow of resize events between "resizers" (elements that control the
size or hidden state of their children) and "resizables" (elements that need to be
notified when they are resized or un-hidden by their parents in order to take
action on their new measurements). 

Elements that perform measurement should add the `IronResizableBehavior` behavior to
their element definition and listen for the `iron-resize` event on themselves.
This event will be fired when they become showing after having been hidden,
when they are resized explicitly by another resizable, or when the window has been
resized.

Note, the `iron-resize` event is non-bubbling.
