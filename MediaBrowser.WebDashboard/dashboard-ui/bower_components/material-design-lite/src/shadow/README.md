## Introduction

The Material Design Lite (MDL) **shadow** is not a component in the same sense as an MDL card, menu, or textbox; it is a visual effect that can be assigned to a user interface element. The effect simulates a three-dimensional positioning of the element, as though it is slightly raised above the surface it rests upon &mdash; a positive *z-axis* value, in user interface terms. The shadow starts at the edges of the element and gradually fades outward, providing a realistic 3-D effect.

Shadows are a convenient and intuitive means of distinguishing an element from its surroundings. A shadow can draw the user's eye to an object and emphasize the object's importance, uniqueness, or immediacy.

Shadows are a well-established feature in user interfaces, and provide users with a visual clue to an object's intended use or value. Their design and use is an important factor in the overall user experience.

### To include an MDL **shadow** effect:

&nbsp;1. Code an element, such as a `<div>`, that is to receive the shadow effect; size and style it as desired, and add any required content.
```html
<div>
Some content
</div>
```
&nbsp;2. Add an MDL shadow class to the element using the `class` attribute.
```html
<div class="mdl-shadow--4dp">
Some content
</div>
```

The shadowed component is ready for use.

#### Examples

A div with a user-specified class and a small shadow.

```html
<div class="my-shadow-card mdl-shadow--2dp">Small shadow</div>
```

A div with a user-specified class and a medium-large shadow.

```html
<div class="my-shadow-card mdl-shadow--6dp">Medium-large shadow</div>
```

## Configuration options

The MDL CSS classes apply various predefined visual shadows to the element. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-shadow--2dp` | Assigns a small shadow to the object | Optional; if omitted, no shadow is present |
| `mdl-shadow--3dp` | Assigns a medium-small shadow to the object | Optional; if omitted, no shadow is present |
| `mdl-shadow--4dp` | Assigns a medium shadow to the object | Optional; if omitted, no shadow is present |
| `mdl-shadow--6dp` | Assigns a medium-large shadow to the object | Optional; if omitted, no shadow is present |
| `mdl-shadow--8dp` | Assigns a large shadow to the object | Optional; if omitted, no shadow is present |
| `mdl-shadow--16dp` | Assigns an extra-large shadow to the object | Optional; if omitted, no shadow is present|
