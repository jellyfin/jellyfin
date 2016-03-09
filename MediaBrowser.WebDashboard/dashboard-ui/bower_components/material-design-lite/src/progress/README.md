## Introduction

The Material Design Lite (MDL) **progress** component is a visual indicator of background activity in a web page or application. A progress indicator consists of a (typically) horizontal bar containing some animation that conveys a sense of motion. While some progress devices indicate an approximate or specific percentage of completion, the MDL progress component simply communicates the fact that an activity is ongoing and is not yet complete.

Progress indicators are an established but non-standardized feature in user interfaces, and provide users with a visual clue to an application's status. Their design and use is therefore an important factor in the overall user experience. See the progress component's [Material Design specifications page](http://www.google.com/design/spec/components/progress-activity.html) for details.

### To include an MDL **progress** component:

&nbsp;1. Code a `<div>` element. Include any desired attributes and values, such as an id or width &mdash; typically done using external CSS rather than the inline `style` attribute as shown here.
```html
<div id="prog1" style="width:250px"></div>
```
&nbsp;2. Add one or more MDL classes, separated by spaces, to the div using the `class` attribute.
```html
<div id="prog1" style="width:250px" class="mdl-js-progress"></div>
```

The progress component is ready for use.

#### Examples

A static (non-animated) progress indicator.
```html
<div id="progstatic" style="width:250px" class="mdl-js-progress"></div>
```

An active (animated) progress indicator.
```html
<div id="progactive" style="width:200px" class="mdl-js-progress
 mdl-progress--indeterminate"></div>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the progress indicator. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-js-progress` | Assigns basic MDL behavior to progress indicator | Required |
| `mdl-progress--indeterminate` | Applies animation effect | Optional |

> Note: `mdl-progress__intermediate` does exist within the codebase. It is deprecated since the name is not in BEM alignment. It will be removed in 2.0.
