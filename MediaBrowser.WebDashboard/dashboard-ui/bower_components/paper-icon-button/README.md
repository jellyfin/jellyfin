
<!---

This README is automatically generated from the comments in these files:
paper-icon-button.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

[![Build Status](https://travis-ci.org/PolymerElements/paper-icon-button.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-icon-button)

_[Demo and API Docs](https://elements.polymer-project.org/elements/paper-icon-button)_


##&lt;paper-icon-button&gt;


Material design: [Icon toggles](https://www.google.com/design/spec/components/buttons.html#buttons-toggle-buttons)

`paper-icon-button` is a button with an image placed at the center. When the user touches
the button, a ripple effect emanates from the center of the button.

`paper-icon-button` includes a default icon set.  Use `icon` to specify which icon
from the icon set to use.

    <paper-icon-button icon="menu"></paper-icon-button>

See [`iron-iconset`](#iron-iconset) for more information about
how to use a custom icon set.

Example:

    <link href="path/to/iron-icons/iron-icons.html" rel="import">

    <paper-icon-button icon="favorite"></paper-icon-button>
    <paper-icon-button src="star.png"></paper-icon-button>

### Styling

Style the button with CSS as you would a normal DOM element. If you are using the icons
provided by `iron-icons`, they will inherit the foreground color of the button.

    /* make a red "favorite" button */
    <paper-icon-button icon="favorite" style="color: red;"></paper-icon-button>

By default, the ripple is the same color as the foreground at 25% opacity. You may
customize the color using the `--paper-icon-button-ink-color` custom property.

The following custom properties and mixins are available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-icon-button-disabled-text` | The color of the disabled button | `--disabled-text-color`
`--paper-icon-button-ink-color` | Selected/focus ripple color | `--primary-text-color`
`--paper-icon-button` | Mixin for a button | `{}`
`--paper-icon-button-disabled` | Mixin for a disabled button | `{}`
`--paper-icon-button-hover` | Mixin for button on hover | `{}`


