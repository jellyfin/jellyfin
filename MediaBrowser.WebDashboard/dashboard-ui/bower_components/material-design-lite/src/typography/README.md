## Introduction

The Material Design Lite (MDL) **typography** component is a comprehensive approach to standardizing the use of typefaces in applications and page displays. MDL typography elements are intended to replace the myriad fonts used by developers (which vary significantly in appearance) and provide a robust, uniform library of text styles from which developers can choose.

The "Roboto" typeface is the standard for MDL display; it can easily be integrated into a web page using the CSS3 `@font-face` rule. However, Roboto is most simply accessed and included using a single standard HTML `<link>` element, which can be obtained at [this Google fonts page](http://www.google.com/fonts#UsePlace:use/Collection:Roboto).

Because of the many possible variations in font display characteristics in HTML and CSS, MDL typography aims to provide simple and intuitive styles that use the Roboto font and produce visually attractive and internally consistent text results. See the typography component's [Material Design specifications page](http://www.google.com/design/spec/style/typography.html) for details.

## Basic use

Include a link to the Google stylesheet that accesses the font and its desired variations.

```html
<head>
<link
 href='http://fonts.googleapis.com/css?family=Roboto:400,400italic,500,500italic,700,700italic'
 rel='stylesheet' type='text/css'>
...
</head>
```

### To include an MDL **typography** component:

&nbsp;1. Code any element (`<div>`,`<p>`,`<span>`, etc.) that can contain text, including whatever content is appropriate.
```html
<p>This is a standard paragraph.</p>
```
&nbsp;2. Add one or more MDL classes, separated by spaces, to the element using the `class` attribute.
```html
<p class="mdl-typography--body-1">This is a standard paragraph.</p>
```

The typography component is ready for use.

#### Examples

A "headline" paragraph.

```html
<p class="mdl-typography--headline">Regular 24px</p>
```

A "title" paragraph.
```html
<p class="mdl-typography--title">Medium 20px</p>
```

A "caption" span.
```html
<span class="mdl-typography--caption">Regular 12px</span>
```

A "button" span.
```html
<span class="mdl-typography--button">Medium (All Caps) 14px</span>
```
A "display 1" table cell.
```html
<td class="mdl-typography--display-1">Regular 34px</td>
```
A "body-1" paragraph, also uppercased.
```html
<p class="mdl-typography--body-1 mdl-typography--text-uppercase">
 This is a standard paragraph, but uppercased.
</p>
```

>**Note:** Because the Roboto font is intended to apply to the entire page, standard "unclassed" HTML elements (e.g., heading levels, divs, paragraphs, spans, tables, etc. with no `class` attribute) and text modifiers (e.g., strong, em, small, etc.) will use Roboto, while also retaining their inherent and/or inherited characteristics.
>
>Also note that MDL typography provides some automatic adjustments based on its display environment. For example, the `body-1` style renders at 14px on a mobile device, but 13px on a desktop. You need not do anything to activate these self-modifiers; they are built into the MDL styles.

## Configuration options

The MDL CSS classes specify the style to use. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-typography--body-1` | Regular 14px (Device), Regular 13px (Desktop) | Optional |
| `mdl-typography--body-1-force-preferred-font` | Regular 14px (Device), Regular 13px (Desktop) | Optional |
| `mdl-typography--body-2` | Medium 14px (Device), Medium 13px (Desktop) | Optional |
| `mdl-typography--body-2` | mdl-typography-body-2 | Optional |
| `mdl-typography--body-2-color-contrast` | Body with color contrast | Optional |
| `mdl-typography--body-2-force-preferred-font` | Medium 14px (Device), Medium 13px (Desktop) | Optional |
| `mdl-typography--button` | Medium (All Caps) 14px | Optional |
| `mdl-typography--caption` | Regular 12px | Optional |
| `mdl-typography--caption-color-contrast` | Caption with color contrast | Optional |
| `mdl-typography--display-1` | Regular 34px | Optional |
| `mdl-typography--display-1-color-contrast` | Display with color contrast | Optional |
| `mdl-typography--display-2` | Regular 45px | Optional |
| `mdl-typography--display-3` | Regular 56px | Optional |
| `mdl-typography--display-4` | Light 112px | Optional |
| `mdl-typography--headline` | Regular 24px | Optional |
| `mdl-typography--menu` | Medium 14px (Device), Medium 13px (Desktop) | Optional |
| `mdl-typography--subhead` | Regular 16px (Device), Regular 15px (Desktop) | Optional |
| `mdl-typography--subhead-color-contrast` | Subhead with color contrast | Optional |
| `mdl-typography--table-striped` | Striped table| Optional |
| `mdl-typography--text-capitalize` | Capitalized text | Optional |
| `mdl-typography--text-center` | Center aligned text | Optional |
| `mdl-typography--text-justify` | Justified text | Optional |
| `mdl-typography--text-left` | Left aligned text | Optional |
| `mdl-typography--text-lowercase` | Lowercased text | Optional |
| `mdl-typography--text-nowrap` | No wrap text | Optional |
| `mdl-typography--text-right` | Right aligned text | Optional |
| `mdl-typography--text-uppercase` | Uppercased text | Optional |
| `mdl-typography--title` | Medium 20px | Optional |
| `mdl-typography--title-color-contrast` | Title with color contrast | Optional |
