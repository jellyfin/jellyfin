## Introduction

The Material Design Lite (MDL) **button** component is an enhanced version of the standard HTML `<button>` element. A button consists of text and/or an image that clearly communicates what action will occur when the user clicks or touches it. The MDL button component provides various types of buttons, and allows you to add both display and click effects.

Buttons are a ubiquitous feature of most user interfaces, regardless of a site's content or function. Their design and use is therefore an important factor in the overall user experience. See the button component's [Material Design specifications page](http://www.google.com/design/spec/components/buttons.html) for details.

The available button display types are *flat* (default), *raised*, *fab*, *mini-fab*, and *icon*; any of these types may be plain (light gray) or *colored*, and may be initially or programmatically *disabled*. The *fab*, *mini-fab*, and *icon* button types typically use a small image as their caption rather than text.

### To include an MDL **button** component:

&nbsp;1. Code a `<button>` element. Include any desired attributes and values, such as an id or event handler, and add a text caption or image as appropriate.
```html
<button>Save</button>
```
&nbsp;2. Add one or more MDL classes, separated by spaces, to the button using the `class` attribute.
```html
<button class="mdl-button mdl-js-button mdl-button--raised">Save</button>
```

The button component is ready for use.

#### Examples

A button with the "raised" effect.
```html
<button class="mdl-button mdl-js-button mdl-button--raised">Save</button>
```

A button with the "fab" effect.
```html
<button class="mdl-button mdl-js-button mdl-button--fab">OK</button>
```

A button with the "icon" and "colored" effects.
```html
<button class="mdl-button mdl-js-button mdl-button--icon mdl-button--colored">?</button>
```


## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the button. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-button` | Defines button as an MDL component | Required |
| `mdl-js-button` | Assigns basic MDL behavior to button | Required |
| (none) | Applies *flat* display effect to button (default) |  |
| `mdl-button--raised` | Applies *raised* display effect | Mutually exclusive with *fab*, *mini-fab*, and *icon* |
| `mdl-button--fab` | Applies *fab* (circular) display effect | Mutually exclusive with *raised*, *mini-fab*, and *icon* |
| `mdl-button--mini-fab` | Applies *mini-fab* (small fab circular) display effect | Mutually exclusive with *raised*, *fab*, and *icon* |
| `mdl-button--icon` | Applies *icon* (small plain circular) display effect | Mutually exclusive with *raised*, *fab*, and *mini-fab*  |
| `mdl-button--colored` | Applies *colored* display effect (primary or accent color, depending on the type of button) | Colors are defined in `material.min.css` |
| `mdl-button--primary` | Applies *primary* color display effect | Colors are defined in `material.min.css` |
| `mdl-button--accent` | Applies *accent* color display effect | Colors are defined in `material.min.css` |
| `mdl-js-ripple-effect` | Applies *ripple* click effect | May be used in combination with any other classes |

>**Note:** Disabled versions of all the available button types are provided, and are invoked with the standard HTML boolean attribute `disabled`. `<button class="mdl-button mdl-js-button mdl-button--raised mdl-js-ripple-effect" disabled>Raised Ripples Disabled</button>`. Alternatively, the `mdl-button--disabled` class can be used to achieve the same result.
>This attribute may be added or removed programmatically via scripting.
