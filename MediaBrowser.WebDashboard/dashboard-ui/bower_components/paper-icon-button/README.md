paper-icon-button
=================

Material Design: <a href="http://www.google.com/design/spec/components/buttons.html">Buttons</a>

`paper-icon-button` is a button with an image placed at the center. When the user touches
the button, a ripple effect emanates from the center of the button.

`paper-icon-button` includes a default icon set.  Use `icon` to specify which icon
from the icon set to use.

```html
<paper-icon-button icon="menu"></paper-icon-button>
```

See [`iron-iconset`](#iron-iconset) for more information about
how to use a custom icon set.

Example:

```html
<link href="path/to/iron-icons/iron-icons.html" rel="import">

<paper-icon-button icon="favorite"></paper-icon-button>
<paper-icon-button src="star.png"></paper-icon-button>
```
