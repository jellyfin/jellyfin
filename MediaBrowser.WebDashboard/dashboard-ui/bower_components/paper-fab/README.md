paper-fab
=========

Material Design: <a href="http://www.google.com/design/spec/components/buttons.html">Button</a>

`paper-fab` is a floating action button. It contains an image placed in the center and
comes in two sizes: regular size and a smaller size by applying the attribute `mini`. When
the user touches the button, a ripple effect emanates from the center of the button.

You may import `iron-icons` to use with this element, or provide a URL to a custom icon.
See `iron-iconset` for more information about how to use a custom icon set.

Example:

```html
<link href="path/to/iron-icons/iron-icons.html" rel="import">

<paper-fab icon="add"></paper-fab>
<paper-fab mini icon="favorite"></paper-fab>
<paper-fab src="star.png"></paper-fab>
```
