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

You may use custom DOM in the button body to create a variety of buttons. For example, to create a button with an icon and some text:

```html		
    <paper-button>
      <iron-icon icon="favorite"></iron-icon>
      custom button content
    </paper-button>
```
