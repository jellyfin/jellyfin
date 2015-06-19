iron-a11y-keys-behavior
=======================

`Polymer.IronA11yKeysBehavior` provides a normalized interface for processing
keyboard commands that pertain to [WAI-ARIA best practices](http://www.w3.org/TR/wai-aria-practices/#kbd_general_binding).
The element takes care of browser differences with respect to Keyboard events
and uses an expressive syntax to filter key presses.

Use the `keyBindings` prototype property to express what combination of keys
will trigger the event to fire.

Use the `key-event-target` attribute to set up event handlers on a specific
node.
The `keys-pressed` event will fire when one of the key combinations set with the
`keys` property is pressed.
