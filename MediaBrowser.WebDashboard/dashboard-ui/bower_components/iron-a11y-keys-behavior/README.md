
<!---

This README is automatically generated from the comments in these files:
iron-a11y-keys-behavior.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/iron-a11y-keys-behavior.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-a11y-keys-behavior)

_[Demo and API docs](https://elements.polymer-project.org/elements/iron-a11y-keys-behavior)_


##Polymer.IronA11yKeysBehavior

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


