
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
will trigger the callback. A key binding has the format
`"KEY+MODIFIER:EVENT": "callback"` (`"KEY": "callback"` or
`"KEY:EVENT": "callback"` are valid as well). Some examples:

```javascript
 keyBindings: {
   'space': '_onKeydown', // same as 'space:keydown'
   'shift+tab': '_onKeydown',
   'enter:keypress': '_onKeypress',
   'esc:keyup': '_onKeyup'
 }
```

The callback will receive with an event containing the following information in `event.detail`:

```javascript
 _onKeydown: function(event) {
   console.log(event.detail.combo); // KEY+MODIFIER, e.g. "shift+tab"
   console.log(event.detail.key); // KEY only, e.g. "tab"
   console.log(event.detail.event); // EVENT, e.g. "keydown"
   console.log(event.detail.keyboardEvent); // the original KeyboardEvent
 }
```

Use the `keyEventTarget` attribute to set up event handlers on a specific
node.

See the [demo source code](https://github.com/PolymerElements/iron-a11y-keys-behavior/blob/master/demo/x-key-aware.html)
for an example.


