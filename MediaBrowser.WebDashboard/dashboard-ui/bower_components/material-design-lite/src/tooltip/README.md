## Introduction

The Material Design Lite (MDL) **tooltip** component is an enhanced version of the standard HTML tooltip as produced by the `title` attribute. A tooltip consists of text and/or an image that clearly communicates additional information about an element when the user hovers over or, in a touch-based UI, touches the element. The MDL tooltip component is pre-styled (colors, fonts, and other settings are contained in *material.min.css*) to provide a vivid, attractive visual element that displays related but typically non-essential content, e.g., a definition, clarification, or brief instruction.

Tooltips are a ubiquitous feature of most user interfaces, regardless of a site's content or function. Their design and use is an important factor in the overall user experience. See the tooltip component's [Material Design specifications page](http://www.google.com/design/spec/components/tooltips.html) for details.

### To include an MDL **tooltip** component:

&nbsp;1. Code an element, such as a `<div>`, `<p>`, or `<span>`, and style it as desired; this will be the tooltip's target. Include an `id` attribute and unique value to link the container to its tooltip.
```html
<p id="tt1">HTML</p>
```
&nbsp;2. Following the target element, code a second element, such as a `<div>`, `<p>`, or `<span>`; this will be the tooltip itself. Include a `for` attribute whose value matches that of the target's `id`.
```html
<p id="tt1">HTML</p>
<span for="tt1">HyperText Markup Language</span>
```
&nbsp;3. Add one or more MDL classes, separated by spaces, to the tooltip element using the `class` attribute.
```html
<p id="tt1">HTML</p>
<span for="tt1" class="mdl-tooltip">HyperText Markup Language</span>
```

The tooltip component is ready for use.

#### Examples

A target with a simple text tooltip.
```html
<p>HTML is related to but different from <span id="xml"><i>XML</i></span>.</p>
<span class="mdl-tooltip" for="xml">eXtensible Markup Language</span>
```

A target with "rich" (containing HTML markup) tooltip text.
```html
<p>HTML is related to but different from <span id="xml"><i>XML</i></span>.</p>
<span class="mdl-tooltip" for="xml">e<b>X</b>tensible <b>M</b>arkup <b>L</b>anguage</span>
```

A target with a long text tooltip that automatically wraps.
```html
<p>HTML is related to but different from <span id="xml"><i>XML</i></span>.</p>
<span class="mdl-tooltip" for="xml">XML is an acronym for eXtensible Markup Language</span>
```

A target with tooltip text in a larger font size.
```html
<p>HTML is related to but different from <span id="xml"><i>XML</i></span>.</p>
<span class="mdl-tooltip mdl-tooltip--large" for="xml">eXtensible Markup Language</span>
```

A target with a tooltip containing both an image and text.
```html
<p>HTML is related to but different from <span id="xml"><i>XML</i></span>.</p>
<span class="mdl-tooltip" for="xml">
<img src="xml-logo-small.png" width="20" height="10"> eXtensible Markup Language</span>
```

## Configuration options

The MDL CSS classes apply various predefined visual enhancements to the tooltip. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-tooltip` | Defines a container as an MDL tooltip | Required on tooltip container element |
| `mdl-tooltip--large` | Applies large-font effect | Optional; goes on tooltip container element |
| `mdl-tooltip--left` | Positions the tooltip to the left of the target | Optional; goes on tooltip container element |
| `mdl-tooltip--right` | Positions the tooltip to the right of the target | Optional; goes on tooltip container element |
| `mdl-tooltip--top` | Positions the tooltip to the top of the target | Optional; goes on tooltip container element |
| `mdl-tooltip--bottom` | Positions the tooltip to the bottom of the target | Optional; goes on tooltip container element |
