## Introduction

The Material Design Lite (MDL) **checkbox** component is an enhanced version of the standard HTML `<input type="checkbox">` element. A checkbox consists of a small square and, typically, text that clearly communicates a binary condition that will be set or unset when the user clicks or touches it. Checkboxes typically, but not necessarily, appear in groups, and can be selected and deselected individually. The MDL checkbox component allows you to add display and click effects.

Checkboxes are a common feature of most user interfaces, regardless of a site's content or function. Their design and use is therefore an important factor in the overall user experience. See the checkbox component's [Material Design specifications page](https://www.google.com/design/spec/components/selection-controls.html#selection-controls-checkbox) for details.

The enhanced checkbox component has a more vivid visual look than a standard checkbox, and may be initially or programmatically *disabled*.

### To include an MDL **checkbox** component:

&nbsp;1. Code a `<label>` element and give it a `for` attribute whose value is the unique id of the checkbox it will contain. The `for` attribute is optional when the `<input>` element is contained inside the `<label>` element, but is recommended for clarity.
```html
<label for="chkbox1">
...
</label>
```
&nbsp;2. Inside the label, code an `<input>` element and give it a `type` attribute whose value is `"checkbox"`. Also give it an `id` attribute whose value matches the label's `for` attribute value.
```html
<label for="chkbox1">
  <input type="checkbox" id="chkbox1">
</label>
```
&nbsp;3. Also inside the label, after the checkbox, code a `<span>` element containing the checkbox's text caption.
```html
<label for="chkbox1">
  <input type="checkbox" id="chkbox1">
  <span>Enable AutoSave</span>
</label>
```
&nbsp;4. Add one or more MDL classes, separated by spaces, to the label, checkbox, and caption using the `class` attribute.
```html
<label for="chkbox1" class="mdl-checkbox mdl-js-checkbox">
  <input type="checkbox" id="chkbox1" class="mdl-checkbox__input">
  <span class="mdl-checkbox__label">Enable AutoSave</span>
</label>
```

The checkbox component is ready for use.

#### Example

A checkbox with a ripple click effect.

```html
<label for="chkbox1" class="mdl-checkbox mdl-js-checkbox mdl-js-ripple-effect">
  <input type="checkbox" id="chkbox1" class="mdl-checkbox__input">
  <span class="mdl-checkbox__label">Enable AutoSave</span>
</label>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the checkbox. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-checkbox` | Defines label as an MDL component | Required on label element|
| `mdl-js-checkbox` | Assigns basic MDL behavior to label | Required on label element |
| `mdl-checkbox__input` | Applies basic MDL behavior to checkbox | Required on input element (checkbox) |
| `mdl-checkbox__label` | Applies basic MDL behavior to caption | Required on span element (caption) |
| `mdl-js-ripple-effect` | Applies *ripple* click effect | Optional; goes on label element, not input element (checkbox) |

>**Note:** Disabled versions of all the available checkbox types are provided, and are invoked with the standard HTML boolean attribute `disabled`. `<input type="checkbox" id="checkbox-5" class="mdl-checkbox__input" disabled>`
>This attribute may be added or removed programmatically via scripting.
