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

Styling
-------

Style the button with CSS as you would a normal DOM element. If you are using the icons
provided by `iron-icons`, the icon will inherit the foreground color of the button.

```html
<!-- make a blue "cloud" button -->
<paper-fab icon="cloud" style="color: blue;"></paper-fab>
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
