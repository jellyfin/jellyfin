paper-ripple
============

`paper-ripple` provides a visual effect that other paper elements can
use to simulate a rippling effect emanating from the point of contact.  The
effect can be visualized as a concentric circle with motion.

Example:

```html
<paper-ripple></paper-ripple>
```

`paper-ripple` listens to "mousedown" and "mouseup" events so it would display ripple
effect when touches on it.  You can also defeat the default behavior and
manually route the down and up actions to the ripple element.  Note that it is
important if you call downAction() you will have to make sure to call
upAction() so that `paper-ripple` would end the animation loop.

Example:

```html
<paper-ripple id="ripple" style="pointer-events: none;"></paper-ripple>
...
<script>
  downAction: function(e) {
    this.$.ripple.downAction({x: e.x, y: e.y});
  },
  upAction: function(e) {
    this.$.ripple.upAction();
  }
</script>
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


By default, the ripple is centered on the point of contact. Apply the ``recenters`` attribute to have the ripple grow toward the center of its container.

```html
<paper-ripple recenters></paper-ripple>
```

Apply `center` to center the ripple inside its container from the start.

```html
<paper-ripple center></paper-ripple>
```

Apply `circle` class to make the rippling effect within a circle.

```html
<paper-ripple class="circle"></paper-ripple>
```
