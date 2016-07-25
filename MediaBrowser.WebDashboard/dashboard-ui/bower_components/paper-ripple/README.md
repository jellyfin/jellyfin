
<!---

This README is automatically generated from the comments in these files:
paper-ripple.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/paper-ripple.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-ripple)

_[Demo and API docs](https://elements.polymer-project.org/elements/paper-ripple)_


##&lt;paper-ripple&gt;

Material design: [Surface reaction](https://www.google.com/design/spec/animation/responsive-interaction.html#responsive-interaction-surface-reaction)

`paper-ripple` provides a visual effect that other paper elements can
use to simulate a rippling effect emanating from the point of contact.  The
effect can be visualized as a concentric circle with motion.

Example:

```html
<div style="position:relative">
  <paper-ripple></paper-ripple>
</div>
```

Note, it's important that the parent container of the ripple be relative position, otherwise
the ripple will emanate outside of the desired container.

`paper-ripple` listens to "mousedown" and "mouseup" events so it would display ripple
effect when touches on it.  You can also defeat the default behavior and
manually route the down and up actions to the ripple element.  Note that it is
important if you call `downAction()` you will have to make sure to call
`upAction()` so that `paper-ripple` would end the animation loop.

Example:

```html
<paper-ripple id="ripple" style="pointer-events: none;"></paper-ripple>
...
downAction: function(e) {
  this.$.ripple.downAction({detail: {x: e.x, y: e.y}});
},
upAction: function(e) {
  this.$.ripple.upAction();
}
```

Styling ripple effect:

  Use CSS color property to style the ripple:

```css
paper-ripple {
  color: #4285f4;
}
```

  Note that CSS color property is inherited so it is not required to set it on
  the `paper-ripple` element directly.

By default, the ripple is centered on the point of contact.  Apply the `recenters`
attribute to have the ripple grow toward the center of its container.

```html
<paper-ripple recenters></paper-ripple>
```

You can also  center the ripple inside its container from the start.

```html
<paper-ripple center></paper-ripple>
```

Apply `circle` class to make the rippling effect within a circle.

```html
<paper-ripple class="circle"></paper-ripple>
```


