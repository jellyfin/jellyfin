
<!---

This README is automatically generated from the comments in these files:
paper-button.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/paper-button.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-button)

_[Demo and API docs](https://elements.polymer-project.org/elements/paper-button)_


##&lt;paper-button&gt;

Material design: [Buttons](https://www.google.com/design/spec/components/buttons.html)

`paper-button` is a button. When the user touches the button, a ripple effect emanates
from the point of contact. It may be flat or raised. A raised button is styled with a
shadow.

Example:

```html
<paper-button>Flat button</paper-button>
<paper-button raised>Raised button</paper-button>
<paper-button noink>No ripple effect</paper-button>
<paper-button toggles>Toggle-able button</paper-button>
```

A button that has `toggles` true will remain `active` after being clicked (and
will have an `active` attribute set). For more information, see the `Polymer.IronButtonState`
behavior.

You may use custom DOM in the button body to create a variety of buttons. For example, to
create a button with an icon and some text:

```html
<paper-button>
  <iron-icon icon="favorite"></iron-icon>
  custom button content
</paper-button>
```

To use `paper-button` as a link, wrap it in an anchor tag. Since `paper-button` will already
receive focus, you may want to prevent the anchor tag from receiving focus as well by setting
its tabindex to -1.

```html
<a href="https://www.polymer-project.org/" tabindex="-1">
  <paper-button raised>Polymer Project</paper-button>
</a>
```

### Styling

Style the button with CSS as you would a normal DOM element.

```css
paper-button.fancy {
  background: green;
  color: yellow;
}

paper-button.fancy:hover {
  background: lime;
}

paper-button[disabled],
paper-button[toggles][active] {
  background: red;
}
```

By default, the ripple is the same color as the foreground at 25% opacity. You may
customize the color using the `--paper-button-ink-color` custom property.

The following custom properties and mixins are also available for styling:

| Custom property | Description | Default |
| --- | --- | --- |
| `--paper-button-ink-color` | Background color of the ripple | `Based on the button's color` |
| `--paper-button` | Mixin applied to the button | `{}` |
| `--paper-button-disabled` | Mixin applied to the disabled button. Note that you can also use the `paper-button[disabled]` selector | `{}` |
| `--paper-button-flat-keyboard-focus` | Mixin applied to a flat button after it's been focused using the keyboard | `{}` |
| `--paper-button-raised-keyboard-focus` | Mixin applied to a raised button after it's been focused using the keyboard | `{}` |


