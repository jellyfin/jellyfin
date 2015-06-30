paper-button
============

Material Design: <a href="http://www.google.com/design/spec/components/buttons.html">Buttons</a>

`paper-button` is a button. When the user touches the button, a ripple effect emanates
from the point of contact. It may be flat or raised. A raised button is styled with a
shadow.

Example:
```html
    <paper-button>flat button</paper-button>
    <paper-button raised>raised button</paper-button>
    <paper-button noink>No ripple effect</paper-button>
```
You may use custom DOM in the button body to create a variety of buttons. For example, to
create a button with an icon and some text:

```html
    <paper-button>
      <iron-icon icon="favorite"></iron-icon>
      custom button content
    </paper-button>
```
## Styling

Style the button with CSS as you would a normal DOM element.

```css
    /* make #my-button green with yellow text */
    #my-button {
        background: green;
        color: yellow;
    }
```
By default, the ripple is the same color as the foreground at 25% opacity. You may
customize the color using this selector:

```css
    /* make #my-button use a blue ripple instead of foreground color */
    #my-button::shadow paper-ripple {
      color: blue;
    }
```
The opacity of the ripple is not customizable via CSS.
