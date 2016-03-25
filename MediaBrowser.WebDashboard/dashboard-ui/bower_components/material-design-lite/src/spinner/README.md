## Introduction

The Material Design Lite (MDL) **spinner** component is an enhanced replacement for the classic "wait cursor" (which varies significantly among hardware and software versions) and indicates that there is an ongoing process, the results of which are not yet available. A spinner consists of an open circle that changes colors as it animates in a clockwise direction, and clearly communicates that a process has been started but not completed.

A spinner performs no action itself, either by its display nor when the user clicks or touches it, and does not indicate a process's specific progress or degree of completion. The MDL spinner component provides various types of spinners, and allows you to add display effects.

Spinners are a fairly new feature of most user interfaces, and provide users with a consistent visual cue about ongoing activity, regardless of hardware device,  operating system, or browser environment. Their design and use is an important factor in the overall user experience.

### To include an MDL **spinner** component:

&nbsp;1. Code an element, such as a `<div>`, `<p>`, or `<span>`, to contain the spinner; the element should have no content of its own.
```html
<div></div>
```
&nbsp;2. Add one or more MDL classes, separated by spaces, to the container using the `class` attribute.
```html
<div class="mdl-spinner mdl-js-spinner is-active"></div>
```

The spinner component is ready for use.

#### Examples

A default spinner in a div.

```html
<div class="mdl-spinner mdl-js-spinner is-active"></div>
```

A single-color spinner in a paragraph.

```html
<p class="mdl-spinner mdl-js-spinner mdl-spinner--single-color is-active"></p>
```

## Configuration options

The MDL CSS classes apply various predefined visual enhancements to the spinner. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-spinner` | Defines the container as an MDL spinner component | Required |
| `mdl-js-spinner` | Assigns basic MDL behavior to spinner | Required |
| `is-active` | Makes the spinner visible and animated | Optional |
| `mdl-spinner--single-color` | Uses a single (primary palette) color instead of changing colors | Optional

>**Note:** There is no specific *disabled* version of a spinner; the presence or absence of the `is-active` class determines whether the spinner is visible. For example, this spinner is inactive and invisible: `<div class="mdl-spinner mdl-js-spinner"></div>`
>This attribute may be added or removed programmatically via scripting.
