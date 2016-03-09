## Introduction

The Material Design Lite (MDL) **menu** component is a user interface element that allows users to select one of a number of options. The selection typically results in an action initiation, a setting change, or other observable effect. Menu options are always presented in sets of two or more, and options may be programmatically enabled or disabled as required. The menu appears when the user is asked to choose among a series of options, and is usually dismissed after the choice is made.

Menus are an established but non-standardized feature in user interfaces, and allow users to make choices that direct the activity, progress, or characteristics of software. Their design and use is an important factor in the overall user experience. See the menu component's [Material Design specifications page](http://www.google.com/design/spec/components/menus.html) for details.

### To include an MDL **menu** component:

> **Note:** The menu requires a non-static positioned parent element. Positioning options may not work properly if the menu is inside of a statically positioned node.

&nbsp;1. Code a `<button>` element; this is the clickable toggle that will show and hide the menu options. Include an `id` attribute whose value will match the `for` (or `data-mdl-for`) attribute of the unordered list coded in the next step. Inside the button, code a `<i>` or `<span>` element to contain an icon of your choice.
```html
<button id="menu1">
  <i></i>
</button>
```
&nbsp;2. Code a `<ul>` unordered list element; this is the container that holds the options. Include a `for` attribute whose value matches the `id` attribute of the button element.
```html
<ul for="menu1">
</ul>
```
&nbsp;3. Inside the unordered list, code one `<li>` element for each option. Include any desired attributes and values, such as an id or event handler, and add a text caption as appropriate.
```html
<ul for="menu1">
  <li>Continue</li>
  <li>Stop</li>
  <li>Pause</li>
</ul>
```
&nbsp;4. Add one or more MDL classes, separated by spaces, to the button and span elements using the `class` attribute.
```html
<button id="menu1" class="mdl-button mdl-js-button mdl-button--icon">
  <i class="material-icons">more_vert</i>
</button>
```
&nbsp;5. Add one or more MDL classes, separated by spaces, to the unordered list and the list items using the `class` attribute.
```html
<ul class="mdl-menu mdl-js-menu" for="menu1">
  <li class="mdl-menu__item">Continue</li>
  <li class="mdl-menu__item">Stop</li>
  <li class="mdl-menu__item">Pause</li>
</ul>
```

The menu component is ready for use.

#### Examples

A menu with three options.
```html
<button id="menu-speed" class="mdl-button mdl-js-button mdl-button--icon">
  <i class="material-icons">more_vert</i>
</button>
<ul class="mdl-menu mdl-js-menu" for="menu-speed">
  <li class="mdl-menu__item">Fast</li>
  <li class="mdl-menu__item">Medium</li>
  <li class="mdl-menu__item">Slow</li>
</ul>
```
A menu with three options, with ripple effect on button and option links.
```html
<button id="menu-speed" class="mdl-button mdl-js-button mdl-button--icon">
  <i class="material-icons">more_vert</i>
</button>
<ul class="mdl-menu mdl-js-menu mdl-js-ripple-effect" for="menu-speed">
  <li class="mdl-menu__item">Fast</li>
  <li class="mdl-menu__item">Medium</li>
  <li class="mdl-menu__item">Slow</li>
</ul>
```
A menu with three options, the second of which is disabled by default.
```html
<button id="menu-speed" class="mdl-button mdl-js-button mdl-button--icon">
  <i class="material-icons">more_vert</i>
</button>
<ul class="mdl-menu mdl-js-menu" for="menu-speed">
  <li class="mdl-menu__item">Fast</li>
  <li class="mdl-menu__item" disabled>Medium</li>
  <li class="mdl-menu__item">Slow</li>
</ul>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the menu. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-button` | Defines button as an MDL component | Required on button element |
| `mdl-js-button` | Assigns basic MDL behavior to button | Required on button element |
| `mdl-button--icon` | Applies *icon* (small plain circular) display effect to button | Required on button element |
| `material-icons` | Defines span as a material icon | Required on an inline element |
| `mdl-menu` | Defines an unordered list container as an MDL component | Required on ul element |
| `mdl-js-menu` | Assigns basic MDL behavior to menu | Required on ul element |
| `mdl-menu__item` | Defines buttons as MDL menu options and assigns basic MDL behavior | Required on list item elements |
| `mdl-menu__item--full-bleed-divider` | Modifies an item to have a full bleed divider between it and the next list item. | Optional on list item elements |
| `mdl-js-ripple-effect` | Applies *ripple* click effect to option links | Optional; goes on unordered list element |
| `mdl-menu--top-left` | Positions menu above button, aligns left edge of menu with button  | Optional; goes on unordered list element |
| (none) | Positions menu below button, aligns left edge of menu with button | Default |
| `mdl-menu--top-right` | Positions menu above button, aligns right edge of menu with button | Optional; goes on unordered list element |
| `mdl-menu--bottom-right` | Positions menu below button, aligns right edge of menu with button | Optional; goes on unordered list element |

(1) The "more-vert" icon class is used here as an example. Other icons can be used by modifying the class name. For a list of available icons, see [this page](http://google.github.io/web-starter-kit/latest/styleguide/icons/demo.html); hover over an icon to see its class name.

(2) The `i` or `span` element in "button"" element can be used interchangeably.

>**Note:** Disabled versions of the menu options are provided, and are invoked with the standard HTML boolean attribute `disabled` or `data-mdl-disabled`. `<li class="mdl-menu__item" disabled>Medium</li>`
>This attribute may be added or removed programmatically via scripting.
