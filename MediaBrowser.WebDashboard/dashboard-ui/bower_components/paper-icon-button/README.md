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

Styling
-------

Style the button with CSS as you would a normal DOM element. If you are using the icons
provided by `iron-icons`, they will inherit the foreground color of the button.

```html
<!-- make a red "favorite" button -->
<paper-icon-button icon="favorite" style="color: red;"></paper-icon-button>
```

By default, the ripple is the same color as the foreground at 25% opacity. You may
customize the color using this selector:

```css
/* make #my-button use a blue ripple instead of foreground color */
#my-button::shadow #ripple {
  color: blue;
}
```

The opacity of the ripple is not customizable via CSS.
