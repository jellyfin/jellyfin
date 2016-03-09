## Introduction

The Material Design Lite (MDL) **switch** component is an enhanced version of the standard HTML `<input type="checkbox">` element. A switch consists of a short horizontal "track" with a prominent circular state indicator and, typically, text that clearly communicates a binary condition that will be set or unset when the user clicks or touches it. Like checkboxes, switches may appear individually or in groups, and can be selected and deselected individually. However, switches provide a more intuitive visual representation of their state: left/gray for *off*, right/colored for *on*. The MDL switch component allows you to add both display and click effects.

Switches, particularly as a replacement for certain checkboxes, can be a valuable feature in user interfaces, regardless of a site's content or function. Their design and use is therefore an important factor in the overall user experience. See the switch component's [Material Design specifications page](http://www.google.com/design/spec/components/selection-controls.html#selection-controls-switch) for details.

The enhanced switch component has a more vivid visual look than a standard checkbox, and may be initially or programmatically *disabled*.

### To include an MDL **switch** component:

&nbsp;1. Code a `<label>` element and give it a `for` attribute whose value is the unique id of the switch it will contain.
```html
<label for="switch1">
...
</label>
```
&nbsp;2. Inside the label, code an `<input>` element and give it a `type` attribute whose value is `"checkbox"`. Also give it an `id` attribute whose value matches the label's `for` attribute value.
```html
<label for="switch1">
  <input type="checkbox" id="switch1">
</label>
```
&nbsp;3. Also inside the label, after the checkbox, code a `<span>` element containing the switch's text caption.
```html
<label for="switch1">
  <input type="checkbox" id="switch1">
  <span>Sound off/on</span>
</label>
```
&nbsp;4. Add one or more MDL classes, separated by spaces, to the label, switch, and caption using the `class` attribute.
```html
<label for="switch1" class="mdl-switch mdl-js-switch">
  <input type="checkbox" id="switch1" class="mdl-switch__input">
  <span class="mdl-switch__label">Sound off/on</span>
</label>
```

The switch component is ready for use.

#### Example

A switch with a ripple click effect.

```html
<label for="switch1" class="mdl-switch mdl-js-switch mdl-js-ripple-effect">
  <input type="checkbox" id="switch1" class="mdl-switch__input">
  <span class="mdl-switch__label">Sound off/on</span>
</label>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the switch. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-switch` | Defines label as an MDL component | Required on label element|
| `mdl-js-switch` | Assigns basic MDL behavior to label | Required on label element |
| `mdl-switch__input` | Applies basic MDL behavior to switch | Required on input element (switch) |
| `mdl-switch__label` | Applies basic MDL behavior to caption | Required on span element (caption) |
| `mdl-js-ripple-effect` | Applies *ripple* click effect | Optional; goes on label element, not input element (switch) |

>**Note:** Disabled versions of all available switch types are provided, and are invoked with the standard HTML boolean attribute `disabled`. `<input type="checkbox" id="switch5" class="mdl-switch__input" disabled>`
>This attribute may be added or removed programmatically via scripting.
