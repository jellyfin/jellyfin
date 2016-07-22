
<!---

This README is automatically generated from the comments in these files:
iron-a11y-announcer.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/iron-a11y-announcer.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-a11y-announcer)

_[Demo and API docs](https://elements.polymer-project.org/elements/iron-a11y-announcer)_


##&lt;iron-a11y-announcer&gt;

`iron-a11y-announcer` is a singleton element that is intended to add a11y
to features that require on-demand announcement from screen readers. In
order to make use of the announcer, it is best to request its availability
in the announcing element.

Example:

```javascript
Polymer({

  is: 'x-chatty',

  attached: function() {
    // This will create the singleton element if it has not
    // been created yet:
    Polymer.IronA11yAnnouncer.requestAvailability();
  }
});
```

After the `iron-a11y-announcer` has been made available, elements can
make announces by firing bubbling `iron-announce` events.

Example:

```javascript
this.fire('iron-announce', {
  text: 'This is an announcement!'
}, { bubbles: true });
```

Note: announcements are only audible if you have a screen reader enabled.


