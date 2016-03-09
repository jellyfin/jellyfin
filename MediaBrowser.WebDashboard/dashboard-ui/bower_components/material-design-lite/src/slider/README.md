## Introduction

The Material Design Lite (MDL) **slider** component is an enhanced version of the new HTML5 `<input type="range">` element. A slider consists of a horizontal line upon which sits a small, movable disc (the *thumb*) and, typically, text that clearly communicates a value that will be set when the user moves it.

Sliders are a fairly new feature in user interfaces, and allow users to choose a value from a predetermined range by moving the thumb through the range (lower values to the left, higher values to the right). Their design and use is an important factor in the overall user experience. See the slider component's [Material Design specifications page](http://www.google.com/design/spec/components/sliders.html) for details.

The enhanced slider component may be initially or programmatically *disabled*.

### To include an MDL **slider** component:

&nbsp;1. Code a `<p>` paragraph element and style it as desired. Include a CSS `width` property (directly or via a CSS class), which determines the slider's size.
```html
<p style="width:300px">
...
</p>
```
&nbsp;2. Inside the paragraph container, code an `<input>` element and give it a `type` attribute whose value is `"range"`. Also give it an `id` attribute to make it available for scripting, and `min` and `max` attributes whose values specify the slider's range. Give it a `value` attribute whose value sets the initial thumb position (optional; if omitted, defaults to 50% of the maximum), and a `step` attribute whose value specifies the increment by which the thumb moves (also optional; if omitted, defaults to 1). Finally, give it an event handler to be executed when the user changes the slider's value.
```html
<p style="width:300px">
  <input type="range" id="s1" min="0" max="10" value="4" step="2">
</p>
```
&nbsp;3. Add one or more MDL classes, separated by spaces, to the slider using the `class` attribute.
```html
<p style="width:300px">
  <input class="mdl-slider mdl-js-slider" type="range" id="s1" min="0" max="10" value="4" step="2">
</p>
```

The slider component is ready for use.

#### Example

A slider that controls volume.
```html
<p style="width:300px">
<input class="mdl-slider mdl-js-slider" type="range" id="s1" min="0" max="10" value="4" step="2">
</p>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the slider. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-slider` | Defines input element as an MDL component | Required |
| `mdl-js-slider` | Assigns basic MDL behavior to input element | Required |

>**Note:** A disabled version of the slider is provided, and is invoked with the standard HTML boolean attribute `disabled`. `<input class="mdl-slider mdl-js-slider" type="range" id="s1" min="0" max="10" value="4" step="2" disabled>`
>This attribute may be added or removed programmatically via scripting.

>**Note:** Although the *value* attribute is used to set a slider's initial value, it should not be used
to modify the value programmatically; instead, use the MDL `change()` method. For example, assuming
that *slider1* is a slider object and *newvalue* is a variable containing the desired value, do not
use `slider1.value = newvalue`; instead, use `slider1.MaterialSlider.change(newvalue)`.

## License

Copyright Google, 2015. Licensed under an Apache-2 license.
