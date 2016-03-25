## Introduction

The Material Design Lite (MDL) **icon-toggle** component is an enhanced version of the standard HTML `<input type="checkbox">` element. An icon-toggle consists of a user defined icon that indicates, by visual highlighting, a binary condition that will be set or unset when the user clicks or touches it. Like checkboxes, icon-toggles may appear individually or in groups, and can be selected and deselected individually.

Icon toggles, particularly as a replacement for certain checkboxes, can be a valuable feature in user interfaces, regardless of a site's content or function. Their design and use is therefore an important factor in the overall user experience. See the icon-toggle component's [Material Design specifications page](http://www.google.com/design/spec/components/buttons.html#buttons-other-buttons) for details.

The icon-toggle component can have a more customized visual look than a standard checkbox, and may be initially or programmatically *disabled*.

### To include an MDL **icon-toggle** component:

&nbsp;1. Code a `<label>` element and give it a `for` attribute whose value is the unique id of the icon-toggle it will contain.
```html
<label for="icon-toggle-1">
...
</label>
```
&nbsp;2. Inside the label, code an `<input>` element and give it a `type` attribute whose value is `"checkbox"`. Also give it an `id` attribute whose value matches the label's `for` attribute value.
```html
<label for="icon-toggle-1">
  <input type="checkbox" id="icon-toggle-1">
</label>
```
&nbsp;3. Also inside the label, after the input element, code an `<i>` element containing the icon-toggle's desired icon.
```html
<label for="icon-toggle-1">
  <input type="checkbox" id="icon-toggle-1">
  <i class="mdl-icon-toggle__label material-icons">format_bold</i>
</label>
```
&nbsp;4. Add one or more MDL classes, separated by spaces, to the label and input elements, using the `class` attribute.
```html
<label class="mdl-icon-toggle mdl-js-icon-toggle mdl-js-ripple-effect" for="icon-toggle-1">
  <input type="checkbox" id="icon-toggle-1" class="mdl-icon-toggle__input">
  <i class="mdl-icon-toggle__label material-icons">format_bold</i>
</label>
```

The icon-toggle component is ready for use.

#### Example

An icon-toggle with a ripple click effect.

```html
<label class="mdl-icon-toggle mdl-js-icon-toggle mdl-js-ripple-effect" for="icon-toggle-1">
  <input type="checkbox" id="icon-toggle-1" class="mdl-icon-toggle__input">
  <i class="mdl-icon-toggle__label material-icons">format_bold</i>
</label>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the icon-toggle. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-icon-toggle` | Defines label as an MDL component | Required on label element|
| `mdl-js-icon-toggle` | Assigns basic MDL behavior to label | Required on label element |
| `mdl-icon-toggle__input` | Applies basic MDL behavior to icon-toggle | Required on input element (icon-toggle) |
| `mdl-icon-toggle__label` | Applies basic MDL behavior to caption | Required on i element (icon) |
| `mdl-js-ripple-effect` | Applies *ripple* click effect | Optional; goes on label element, not input element (icon-toggle) |

>**Note:** Disabled versions of all available input types are provided, and are invoked with the standard HTML boolean attribute `disabled`. `<input type="checkbox" id="icon-toggle-5" class="mdl-icon-toggle__input" disabled>`
>This attribute may be added or removed programmatically via scripting.
