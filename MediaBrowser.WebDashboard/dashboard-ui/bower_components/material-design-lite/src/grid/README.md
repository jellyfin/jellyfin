## Introduction

The Material Design Lite (MDL) **grid** component is a simplified method for laying out content for multiple screen sizes. It reduces the usual coding burden required to correctly display blocks of content in a variety of display conditions.

The MDL grid is defined and enclosed by a container element. A grid has 12 columns in the desktop screen size, 8 in the tablet size, and 4 in the phone size, each size having predefined margins and gutters. Cells are laid out sequentially in a row, in the order they are defined, with some exceptions:

- If a cell doesn't fit in the row in one of the screen sizes, it flows into the following line.
- If a cell has a specified column size equal to or larger than the number of columns for the current screen size, it takes up the entirety of its row.

You can set a maximum grid width, after which the grid stays centered with padding on either side, by setting its `max-width` CSS property.

Grids are a fairly new and non-standardized feature in most user interfaces, and provide users with a way to view content in an organized manner that might otherwise be difficult to understand or retain. Their design and use is an important factor in the overall user experience.

### To include an MDL **grid** component:

&nbsp;1. Code a `<div>` element; this is the "outer" container, intended to hold all of the grid's cells. Style the div as desired (colors, font size, etc.).
```html
<div>
</div>
```
&nbsp;2. Add the `mdl-grid` MDL class to the div using the `class` attribute.
```
<div class="mdl-grid">
</div>
```
&nbsp;3. For each cell, code one "inner" div, including the text to be displayed.
```html
<div class="mdl-grid">
  <div>Content</div>
  <div>goes</div>
  <div>here</div>
</div>
```
&nbsp;4. Add the `mdl-cell` class and an `mdl-cell--COLUMN-col` class, where COLUMN specifies the column size for the cell, to the "inner" divs using the `class` attribute.
```html
<div class="mdl-grid">
  <div class="mdl-cell mdl-cell--4-col">Content</div>
  <div class="mdl-cell mdl-cell--4-col">goes</div>
  <div class="mdl-cell mdl-cell--4-col">here</div>
</div>
```
&nbsp;5. Optionally add an `mdl-cell--COLUMN-col-DEVICE` class, where COLUMN specifies the column size for the cell on a specific device, and DEVICE specifies the device type.
```html
<div class="mdl-grid">
  <div class="mdl-cell mdl-cell--4-col mdl-cell--8-col-tablet">Content</div>
  <div class="mdl-cell mdl-cell--4-col mdl-cell--8-col-tablet">goes</div>
  <div class="mdl-cell mdl-cell--4-col mdl-cell--8-col-tablet">here</div>
</div>
```

The grid component is ready for use.

#### Examples

A grid with five cells of column size 1.
```html
<div class="mdl-grid">
  <div class="mdl-cell mdl-cell--1-col">CS 1</div>
  <div class="mdl-cell mdl-cell--1-col">CS 1</div>
  <div class="mdl-cell mdl-cell--1-col">CS 1</div>
  <div class="mdl-cell mdl-cell--1-col">CS 1</div>
  <div class="mdl-cell mdl-cell--1-col">CS 1</div>
</div>
```

A grid with three cells, one of column size 6, one of column size 4, and one of column size 2.
```html
<div class="mdl-grid">
  <div class="mdl-cell mdl-cell--6-col">CS 6</div>
  <div class="mdl-cell mdl-cell--4-col">CS 4</div>
  <div class="mdl-cell mdl-cell--2-col">CS 2</div>
</div>
```

A grid with three cells of column size 6 that will display as column size 8 on a tablet device.
```html
<div class="mdl-grid">
  <div class="mdl-cell mdl-cell--6-col mdl-cell--8-col-tablet">CS 6 (8 on tablet)</div>
  <div class="mdl-cell mdl-cell--6-col mdl-cell--8-col-tablet">CS 6 (8 on tablet)</div>
  <div class="mdl-cell mdl-cell--6-col mdl-cell--8-col-tablet">CS 6 (8 on tablet)</div>
</div>
```

A grid with four cells of column size 2 that will display as column size 4 on a phone device.

```html
<div class="mdl-grid">
  <div class="mdl-cell mdl-cell--2-col mdl-cell--4-col-phone">CS 2 (4 on phone)</div>
  <div class="mdl-cell mdl-cell--2-col mdl-cell--4-col-phone">CS 2 (4 on phone)</div>
  <div class="mdl-cell mdl-cell--2-col mdl-cell--4-col-phone">CS 2 (4 on phone)</div>
  <div class="mdl-cell mdl-cell--2-col mdl-cell--4-col-phone">CS 2 (4 on phone)</div>
</div>
```

## Configuration options

The MDL CSS classes apply various predefined visual enhancements and behavioral effects to the grid. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-grid` | Defines a container as an MDL grid component | Required on "outer" div element |
| `mdl-cell` | Defines a container as an MDL cell | Required on "inner" div elements |
| `mdl-grid--no-spacing` | Modifies the grid cells to have no margin between them. | Optional on grid container. |
| `mdl-cell--N-col` | Sets the column size for the cell to N | N is 1-12 inclusive, defaults to 4; optional on "inner" div elements|
| `mdl-cell--N-col-desktop` | Sets the column size for the cell to N in desktop mode only | N is 1-12 inclusive; optional on "inner" div elements|
| `mdl-cell--N-col-tablet` | Sets the column size for the cell to N in tablet mode only | N is 1-8 inclusive; optional on "inner" div elements|
| `mdl-cell--N-col-phone` | Sets the column size for the cell to N in phone mode only | N is 1-4 inclusive; optional on "inner" div elements|
| `mdl-cell--N-offset` | Adds N columns of whitespace before the cell | N is 1-11 inclusive; optional on "inner" div elements|
| `mdl-cell--N-offset-desktop` | Adds N columns of whitespace before the cell in desktop mode | N is 1-11 inclusive; optional on "inner" div elements|
| `mdl-cell--N-offset-tablet` | Adds N columns of whitespace before the cell in tablet mode | N is 1-7 inclusive; optional on "inner" div elements|
| `mdl-cell--N-offset-phone` | Adds N columns of whitespace before the cell in phone mode | N is 1-3 inclusive; optional on "inner" div elements|
| `mdl-cell--order-N` | Reorders cell to position N | N is 1-12 inclusive; optional on "inner" div elements|
| `mdl-cell--order-N-desktop` | Reorders cell to position N when in desktop mode | N is 1-12 inclusive; optional on "inner" div elements|
| `mdl-cell--order-N-tablet` | Reorders cell to position N when in tablet mode | N is 1-12 inclusive; optional on "inner" div elements|
| `mdl-cell--order-N-phone` | Reorders cell to position N when in phone mode | N is 1-12 inclusive; optional on "inner" div elements|
| `mdl-cell--hide-desktop` | Hides the cell when in desktop mode | Optional on "inner" div elements |
| `mdl-cell--hide-tablet` | Hides the cell when in tablet mode | Optional on "inner" div elements |
| `mdl-cell--hide-phone` | Hides the cell when in phone mode | Optional on "inner" div elements |
| `mdl-cell--stretch` | Stretches the cell vertically to fill the parent | Default; optional on "inner" div elements |
| `mdl-cell--top` | Aligns the cell to the top of the parent | Optional on "inner" div elements |
| `mdl-cell--middle` | Aligns the cell to the middle of the parent | Optional on "inner" div elements |
|`mdl-cell--bottom` | Aligns the cell to the bottom of the parent | Optional on "inner" div elements |
