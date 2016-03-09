## Introduction

The Material Design Lite (MDL) **text field** component is an enhanced version of the standard HTML `<input type="text">` and `<input type="textarea">` elements. A text field consists of a horizontal line indicating where keyboard input can occur and, typically, text that clearly communicates the intended contents of the text field. The MDL text field component provides various types of text fields, and allows you to add both display and click effects.

Text fields are a common feature of most user interfaces, regardless of a site's content or function. Their design and use is therefore an important factor in the overall user experience. See the text field component's [Material Design specifications page](http://www.google.com/design/spec/components/text-fields.html) for details.

The enhanced text field component has a more vivid visual look than a standard text field, and may be initially or programmatically *disabled*.
There are three main types of text fields in the text field component, each with its own basic coding requirements. The types are *single-line*, *multi-line*, and *expandable*.

### To include a *single-line* MDL **text field** component:

&nbsp;1. Code a `<div>` element to hold the text field.
```html
<div>
...
</div>
```
&nbsp;2. Inside the div, code an `<input>` element with a `type` attribute of `"text"` (the text field), and an `id` attribute of your choice.
```html
<div>
  <input type="text" id="user">
</div>
```
&nbsp;3. Also inside the div, after the text field, code a `<label>` element with a `for` attribute whose value matches the `input` element's `id` value, and a short string to be used as the field's placeholder text.
```html
<div>
  <input type="text" id="user">
  <label for="user">User name</label>
</div>
```
&nbsp;4. Optionally, add a `pattern` attribute and value to the `<input>` element (see the [W3C HTML5 forms specification](http://www.w3.org/TR/html5/forms.html#the-pattern-attribute) for details) and an associated error message in a `<span>` element following the `<label>`.
```html
<div>
  <input type="text" id="user" pattern="[A-Z,a-z, ]*">
  <label for="user">User name</label>
  <span>Letters and spaces only</span>
</div>
```
&nbsp;5. Add one or more MDL classes, separated by spaces, to the div container, text field, field label, and error message using the `class` attribute.
```html
<div class="mdl-textfield mdl-js-textfield">
  <input class="mdl-textfield__input" type="text" id="user" pattern="[A-Z,a-z, ]*">
  <label class="mdl-textfield__label" for="user">User name</label>
  <span class="mdl-textfield__error">Letters and spaces only</span>
</div>
```
The single-line text field component is ready for use.

#### Examples

Single-line text field with a standard label.
```html
<div class="mdl-textfield mdl-js-textfield">
  <input class="mdl-textfield__input" type="text" id="fname">
  <label class="mdl-textfield__label" for="fname">First name</label>
</div>
```

Single-line text field with a floating label.
```html
<div class="mdl-textfield mdl-js-textfield mdl-textfield--floating-label">
  <input class="mdl-textfield__input" type="text" id="addr1">
  <label class="mdl-textfield__label" for="addr1">Address line 1</label>
</div>
```

Single-line text field with a standard label, pattern matching, and error message.
```html
<div class="mdl-textfield mdl-js-textfield">
  <input class="mdl-textfield__input" type="text" pattern="[0-9]*" id="phone">
  <label class="mdl-textfield__label" for="phone">Phone</label>
  <span class="mdl-textfield__error">Digits only</span>
</div>
```

### To include a *multi-line* MDL **text field** component:

&nbsp;1. Code a `<div>` element to hold the text field.
```html
<div>
...
</div>
```
&nbsp;2. Inside the div, code a `<textarea>` element with a `type` attribute of `"text"` (the multi-line text field), and an `id` attribute of your choice. Include a `rows` attribute with a value of `"1"` (this attribute sets the number of *concurrently visible* input rows).
```html
<div>
  <textarea type="text" rows="1" id="address"></textarea>
</div>
```
&nbsp;3. Also inside the div, after the text field, code a `<label>` element with a `for` attribute whose value matches the `<textarea>` element's `id` value, and a short string to be used as the field's placeholder text.
```html
<div>
  <textarea type="text" rows="1" id="address"></textarea>
  <label for="address">Full address</label>
</div>
```
&nbsp;4. Add one or more MDL classes, separated by spaces, to the div container, text field, and field label using the `class` attribute.
```html
<div class="mdl-textfield mdl-js-textfield">
  <textarea class="mdl-textfield__input" type="text" rows="1" id="address"></textarea>
  <label class="mdl-textfield__label" for="address">Full address</label>
</div>
```

The multi-line text field component is ready for use.

#### Examples

Multi-line text field with one visible input line.
```html
<div class="mdl-textfield mdl-js-textfield">
  <textarea class="mdl-textfield__input" type="text" rows="1" id="schools"></textarea>
  <label class="mdl-textfield__label" for="schools">Schools attended</label>
</div>
```

Multi-line text field with one visible input line and floating label.
```html
<div class="mdl-textfield mdl-js-textfield mdl-textfield--floating-label">
  <textarea class="mdl-textfield__input" type="text" rows= "1" id="schools"></textarea>
  <label class="mdl-textfield__label" for="schools">Schools attended</label>
</div>
```

Multi-line text field with multiple visible input lines and a maximum number of lines.
```html
<div class="mdl-textfield mdl-js-textfield">
  <textarea class="mdl-textfield__input" type="text" rows="3" maxrows="6"
   id="schools"></textarea>
  <label class="mdl-textfield__label" for="schools">Schools attended (max. 6)</label>
</div>
```

### To include an *expandable* MDL **text field** component:

&nbsp;1. Code an "outer" `<div>` element to hold the expandable text field.
```html
<div>
...
</div>
```
&nbsp;2. Inside the div, code a `<label>` element with a `for` attribute whose value will match the `<input>` element's `id` value (to be coded in step 5).
```html
<div>
  <label for="expando1">
  ...
  </label>
</div>
```
&nbsp;3. Inside the label, code a `<span>` element; the span should be empty, and should be the label's only content. This element will contain the expandable text field's icon.
```html
<div>
  <label for="expando1">
    <span></span>
  </label>
</div>
```
&nbsp;4. Still inside the "outer" div, after the label containing the span, code an "inner" (nested) `<div>` element.
```html
<div>
  <label for="expando1">
    <span></span>
  </label>
  <div>
  ...
  </div>
</div>
```
&nbsp;5. Inside the "inner" div, code an `<input>` element with a `type` attribute of `"text"` (the text field), and an `id` attribute whose value matches that of the `for` attribute in step 2.
```html
<div>
  <label for="expando1">
    <span></span>
  </label>
  <div>
    <input type="text" id="expando1">
  </div>
</div>
```
&nbsp;6. Still inside the "inner" div, after the text field, code a `<label>` element with a `for` attribute whose value also matches the `<input>` element's `id` value (coded in step 5), and a short string to be used as the field's placeholder text.
```html
<div>
  <label for="expando1">
    <span></span>
  </label>
  <div>
    <input type="text" id="expando1">
    <label for="expando1">Expandable text field</label>
  </div>
</div>
```
&nbsp;7. Add one or more MDL classes, separated by spaces, to the "outer" div container, label, and span, and to the "inner" div container, text field, and field label using the `class` attribute.
```html
<div class="mdl-textfield mdl-js-textfield mdl-textfield--expandable">
  <label class="mdl-button mdl-js-button mdl-button--icon" for="expando1">
    <i class="material-icons">search</i>
  </label>
  <div class="mdl-textfield__expandable-holder">
    <input class="mdl-textfield__input" type="text" id="expando1">
    <label class="mdl-textfield__label" for="expando1">Expandable text field</label>
  </div>
</div>
```

The expandable text field component is ready for use. It will expand when the icon (the empty `<span>`) is clicked or gains focus.

#### Examples

Expandable text field with a standard label.
```html
<div class="mdl-textfield mdl-js-textfield mdl-textfield--expandable">
  <label class="mdl-button mdl-js-button mdl-button--icon" for="search-expandable">
    <i class="material-icons">search</i>
  </label>
  <div class="mdl-textfield__expandable-holder">
    <input class="mdl-textfield__input" type="text" id="search-expandable">
    <label class="mdl-textfield__label" for="search-expandable">Search text</label>
  </div>
</div>
```

Expandable text field with a floating label.
```html
<div class="mdl-textfield mdl-js-textfield mdl-textfield--expandable
 mdl-textfield--floating-label">
  <label class="mdl-button mdl-js-button mdl-button--icon" for="search-expandable2">
    <i class="material-icons">search</i>
  </label>
  <div class="mdl-textfield__expandable-holder">
    <input class="mdl-textfield__input" type="text" id="search-expandable2">
    <label class="mdl-textfield__label" for="search-expandable2">
      Enter search text below
    </label>
  </div>
</div>
```
## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the text field. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-textfield` | Defines container as an MDL component | Required on "outer" div element|
| `mdl-js-textfield` | Assigns basic MDL behavior to input | Required on "outer" div element |
| `mdl-textfield__input` | Defines element as textfield input | Required on input or textarea element |
| `mdl-textfield__label` | Defines element as textfield label | Required on label element for input or textarea elements |
| `mdl-textfield--floating-label` | Applies *floating label* effect | Optional; goes on "outer" div element |
| `mdl-textfield__error` | Defines span as an MDL error message | Optional; goes on span element for MDL input element with *pattern*|
| `mdl-textfield--expandable` | Defines a div as an MDL expandable text field container | For expandable input fields, required on "outer" div element |
| `mdl-button` | Defines label as an MDL icon button | For expandable input fields, required on "outer" div's label element |
| `mdl-js-button` | Assigns basic behavior to icon container | For expandable input fields, required on "outer" div's label element |
| `mdl-button--icon` | Defines label as an MDL icon container | For expandable input fields, required on "outer" div's label element |
| `mdl-input__expandable-holder` | Defines a container as an MDL component | For expandable input fields, required on "inner" div element |
| `is-invalid` | Defines the textfield as invalid on initial load. | Optional on `mdl-textfield` element |

(1) The "search" icon is used here as an example. Other icons can be used by modifying the text. For a list of available icons, see [this page](https://www.google.com/design/icons).

>**Note:** Disabled versions of each text field type are provided, and are invoked with the standard HTML boolean attribute `disabled`. `<input class="mdl-textfield mdl-js-textfield" type="text" disabled>`
>This attribute may be added or removed programmatically via scripting.
