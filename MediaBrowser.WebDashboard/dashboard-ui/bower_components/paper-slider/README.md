paper-slider
============

`paper-slider` allows user to select a value from a range of values by
moving the slider thumb.  The interactive nature of the slider makes it a
great choice for settings that reflect intensity levels, such as volume,
brightness, or color saturation.

Example:

```html
<paper-slider></paper-slider>
```

Use `min` and `max` to specify the slider range. Default is `0` to `100`. For example:
```html
<paper-slider min="10" max="200" value="110"></paper-slider>
```

### Styling slider

The following custom properties and mixins are available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-slider-bar-color` | The background color of the slider | `transparent`
`--paper-slider-active-color` | The progress bar color | `--google-blue-700`
`--paper-slider-secondary-color` | The secondary progress bar color | `--google-blue-300`
`--paper-slider-knob-color` | The knob color | `--google-blue-700`
`--paper-slider-disabled-knob-color` | The disabled knob color | `--google-grey-500`
`--paper-slider-pin-color` | The pin color | `--google-blue-700`
`--paper-slider-font-color` | The pin's text color | `#fff`
`--paper-slider-disabled-active-color` | The disabled progress bar color | `--google-grey-500`
`--paper-slider-disabled-secondary-color` | The disabled secondary progress bar color | `--google-grey-300`

Example:

```
paper-slider {
  --paper-slider-bar-color: #fff;
  --paper-slider-active-color: #0f9d58;
  --paper-slider-knob-color: #0f9d58;
  --paper-slider-pin-color: #0f9d58;
  --paper-slider-font-color: #0f9d58;
  --paper-slider-secondary-color: #0f9d58;
  --paper-slider-disabled-active-color: #ccc;
  --paper-slider-disabled-secondary-color: #ccc;
}
```
