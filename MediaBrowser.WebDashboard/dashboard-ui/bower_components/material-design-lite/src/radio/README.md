## Introduction

The Material Design Lite (MDL) **radio** component is an enhanced version of the standard HTML `<input type="radio">`, or "radio button" element. A radio button consists of a small circle and, typically, text that clearly communicates a condition that will be set when the user clicks or touches it. Radio buttons always appear in groups of two or more and, while they can be individually selected, can only be deselected by selecting a different radio button in the same group (which deselects all other radio buttons in the group). The MDL radio component allows you to add display and click effects.

Radio buttons are a common feature of most user interfaces, regardless of a site's content or function. Their design and use is therefore an important factor in the overall user experience. See the radio component's [Material Design specifications page](https://www.google.com/design/spec/components/selection-controls.html#selection-controls-radio-button) for details.

The enhanced radio component has a more vivid visual look than a standard radio button, and may be initially or programmatically *disabled*.

### To include an MDL **radio** component:

&nbsp;1. Code a `<label>` element and give it a `for` attribute whose value is the unique id of the radio button it will contain. The `for` attribute is optional when the `<input>` element is contained inside the `<label>` element, but is recommended for clarity.
```html
<label for="radio1">
...
</label>
```
&nbsp;2. Inside the label, code an `<input>` element and give it a `type` attribute whose value is `"radio"`. Also give it an `id` attribute whose value matches the label's `for` attribute value, and a `name` attribute whose value identifies the radio button group. Optionally, give it a `value` attribute whose value provides some information about the radio button for scripting purposes.
```html
<label for="radio1">
  <input type="radio" id="radio1" name="flash" value="on">
</label>
```
&nbsp;3. Also inside the label, after the radio button, code a `<span>` element containing the radio button's text caption.
```html
<label for="radio1">
  <input type="radio" id="radio1" name="flash" value="on">
  <span>Always on</span>
</label>
```
&nbsp;4. Add one or more MDL classes, separated by spaces, to the label, checkbox, and caption using the `class` attribute.
```html
<label for="radio1" class="mdl-radio mdl-js-radio">
  <input type="radio" id="radio1" name="flash" value="on" class="mdl-radio__button">
  <span class="mdl-radio__label">Always on</span>
</label>
```
&nbsp;5. Repeat steps 1 through 4 for the other radio components in the group. For each one:
* on the `label` element, specify a unique `for` attribute value
* on the `input` element, specify an `id` attribute value that matches its `label` element's `for` attribute value
* on the `input` element, specify the same `name` attribute value for all radio components in the group
* optionally, on the `input` element, specify a unique `value` attribute value

The radio components are ready for use.

#### Example

A group of radio buttons to control a camera's flash setting.
```html
<label class="mdl-radio mdl-js-radio mdl-js-ripple-effect" for="flash1">
  <input checked class="mdl-radio__button" id="flash1" name="flash" type="radio"
   value="on">
  <span class="mdl-radio__label">Always on</span>
</label>
<label class="mdl-radio mdl-js-radio mdl-js-ripple-effect" for="flash2">
  <input class="mdl-radio__button" id="flash2" name="flash" type="radio" value="off">
  <span class="mdl-radio__label">Always off</span>
</label>
<label class="mdl-radio mdl-js-radio mdl-js-ripple-effect" for="flash3">
  <input class="mdl-radio__button" id="flash3" name="flash" type="radio" value="auto">
  <span class="mdl-radio__label">Automatic</span>
</label>
```
## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the radio button. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-radio` | Defines label as an MDL component | Required on label element|
| `mdl-js-radio` | Assigns basic MDL behavior to label | Required on label element |
| `mdl-radio__button` | Applies basic MDL behavior to radio | Required on input element (radio button) |
| `mdl-radio__label` | Applies basic MDL behavior to caption | Required on span element (caption) |
| `mdl-js-ripple-effect` | Applies *ripple* click effect | Optional; goes on label element, not input element (radio button) |

>**Note:** Disabled versions of all the available radio button types are provided, and are invoked with the standard HTML boolean attribute `disabled`. `<input type="radio" id="radio5" name="flash" class="mdl-radio__button" disabled>`
>This attribute may be added or removed programmatically via scripting.
