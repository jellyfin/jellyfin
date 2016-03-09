## Introduction

The Material Design Lite (MDL) **layout** component is a comprehensive approach to page layout that uses MDL development tenets, allows for efficient use of MDL components, and automatically adapts to different browsers, screen sizes, and devices.

Appropriate and accessible layout is a critical feature of all user interfaces, regardless of a site's content or function. Page design and presentation is therefore an important factor in the overall user experience. See the layout component's [Material Design specifications page](http://www.google.com/design/spec/layout/principles.html) for details.

Use of MDL layout principles simplifies the creation of scalable pages by providing reusable components and encourages consistency across environments by establishing recognizable visual elements, adhering to logical structural grids, and maintaining appropriate spacing across multiple platforms and screen sizes. MDL layout is extremely powerful and dynamic, allowing for great consistency in outward appearance and behavior while maintaining development flexibility and ease of use.

### To include a basic MDL **layout** component:

&nbsp;1. Code a `<div>` element. This is the "outer" div that holds the entire layout.
```html
<div>
</div>
```

>**Note:** The layout cannot be applied directly on the `<body>` element. Always create a nested `<div>` element.

&nbsp;2. Add MDL classes as indicated, separated by spaces, to the div using the `class` attribute.
```html
<div class="mdl-layout mdl-js-layout">
</div>
```

&nbsp;3. Inside the div, code a `<header>` element. This holds the header row with navigation links that is displayed on large screens, and the menu icon that opens the navigation drawer for smaller screens. Add the MDL class to the header using the `class` attribute.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
  </header>
</div>
```

&nbsp;4. Inside the header, add a `<div>` to produce the menu icon, and include the MDL class as indicated. The div has no content of its own.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
  </header>
</div>
```

&nbsp;5. Still inside the header, add another `<div>` to hold the header row's content, and include the MDL class as indicated.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
    </div>
  </header>
</div>
```

&nbsp;6. Inside the header row div, add a span containing the layout title, and include the MDL class as indicated.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Simple Layout</span>
    </div>
  </header>
</div>
```

&nbsp;7. Following the span, add a `<div>` to align the header's navigation links to the right, and include the MDL class as indicated.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Simple Layout</span>
      <div class="mdl-layout-spacer"></div>
    </div>
  </header>
</div>
```

&nbsp;8. Following the spacer div, add a `<nav>` element to contain the header's navigation links, and include the MDL class as indicated. Inside the nav, add one anchor `<a>` element for each header link, and include the MDL class as indicated. This completes the layout's header.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Simple Layout</span>
      <div class="mdl-layout-spacer"></div>
      <nav class="mdl-navigation">
        <a class="mdl-navigation__link" href="#">Nav link 1</a>
        <a class="mdl-navigation__link" href="#">Nav link 2</a>
        <a class="mdl-navigation__link" href="#">Nav link 3</a>
      </nav>
    </div>
  </header>
</div>
```

&nbsp;9. Following the header, add a `<div>` element to hold the slide-out drawer's content, and add the MDL class as indicated. The drawer appears automatically on smaller screens, and may be opened with the menu icon on any screen size.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Simple Layout</span>
      <div class="mdl-layout-spacer"></div>
      <nav class="mdl-navigation">
        <a class="mdl-navigation__link" href="#">Nav link 1</a>
        <a class="mdl-navigation__link" href="#">Nav link 2</a>
        <a class="mdl-navigation__link" href="#">Nav link 3</a>
      </nav>
    </div>
  </header>
  <div class="mdl-layout__drawer">
  </div>
</div>
```

&nbsp;10. Inside the drawer div, add a span containing the layout title (this should match the title in step 5), and include the MDL class as indicated.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Simple Layout</span>
      <div class="mdl-layout-spacer"></div>
      <nav class="mdl-navigation">
        <a class="mdl-navigation__link" href="#">Nav link 1</a>
        <a class="mdl-navigation__link" href="#">Nav link 2</a>
        <a class="mdl-navigation__link" href="#">Nav link 3</a>
      </nav>
    </div>
  </header>
  <div class="mdl-layout__drawer">
    <span class="mdl-layout__title">Simple Layout</span>
  </div>
</div>
```

&nbsp;11. Following the span, add a `<nav>` element to contain the drawer's navigation links, and one anchor `<a>` element for each drawer link (these should match the links in step 7), and include the MDL classes as indicated. This completes the layout's drawer.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Simple Layout</span>
      <div class="mdl-layout-spacer"></div>
      <nav class="mdl-navigation">
        <a class="mdl-navigation__link" href="#">Nav link 1</a>
        <a class="mdl-navigation__link" href="#">Nav link 2</a>
        <a class="mdl-navigation__link" href="#">Nav link 3</a>
      </nav>
    </div>
  </header>
  <div class="mdl-layout__drawer">
    <span class="mdl-layout__title">Simple Layout</span>
    <nav class="mdl-navigation">
      <a class="mdl-navigation__link" href="#">Nav link 2</a>
      <a class="mdl-navigation__link" href="#">Nav link 2</a>
      <a class="mdl-navigation__link" href="#">Nav link 3</a>
    </nav>
  </div>
</div>
```

&nbsp;12. Finally, following the drawer div, add a `<main>` element to hold the layout's primary content, and include the MDL class as indicated. Inside that element, add your desired content.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Simple Layout</span>
      <div class="mdl-layout-spacer"></div>
      <nav class="mdl-navigation">
        <a class="mdl-navigation__link" href="#">Nav link 1</a>
        <a class="mdl-navigation__link" href="#">Nav link 2</a>
        <a class="mdl-navigation__link" href="#">Nav link 3</a>
      </nav>
    </div>
  </header>
  <div class="mdl-layout__drawer">
    <span class="mdl-layout__title">Simple Layout</span>
    <nav class="mdl-navigation">
      <a class="mdl-navigation__link" href="#">Nav link 2</a>
      <a class="mdl-navigation__link" href="#">Nav link 2</a>
      <a class="mdl-navigation__link" href="#">Nav link 3</a>
    </nav>
  </div>
  <main class="mdl-layout__content">
    <p>Content</p>
    <p>Goes</p>
    <p>Here</p>
  </main>
</div>
```

The layout component is ready for use.

#### Examples

A layout with a fixed header for larger screens and a collapsible drawer for smaller screens.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header">
    <div class="mdl-layout-icon"></div>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Material Design Lite</span>
      <div class="mdl-layout-spacer"></div>
      <nav class="mdl-navigation">
        <a class="mdl-navigation__link" href="#">Hello</a>
        <a class="mdl-navigation__link" href="#">World.</a>
        <a class="mdl-navigation__link" href="#">How</a>
        <a class="mdl-navigation__link" href="#">Are</a>
        <a class="mdl-navigation__link" href="#">You?</a>
      </nav>
    </div>
  </header>
  <div class="mdl-layout__drawer">
    <span class="mdl-layout__title">Material Design Lite</span>
    <nav class="mdl-navigation">
      <a class="mdl-navigation__link" href="#">Hello</a>
      <a class="mdl-navigation__link" href="#">World.</a>
      <a class="mdl-navigation__link" href="#">How</a>
      <a class="mdl-navigation__link" href="#">Are</a>
      <a class="mdl-navigation__link" href="#">You?</a>
    </nav>
  </div>
  <main class="mdl-layout__content">
    <div>Content</div>
  </main>
</div>
```

The same layout with a non-fixed header that scrolls with the content.
```html
<div class="mdl-layout mdl-js-layout">
  <header class="mdl-layout__header mdl-layout__header--scroll">
    <img class="mdl-layout-icon"></img>
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Material Design Lite</span>
      <div class="mdl-layout-spacer"></div>
      <nav class="mdl-navigation">
        <a class="mdl-navigation__link" href="#">Hello</a>
        <a class="mdl-navigation__link" href="#">World.</a>
        <a class="mdl-navigation__link" href="#">How</a>
        <a class="mdl-navigation__link" href="#">Are</a>
        <a class="mdl-navigation__link" href="#">You?</a>
      </nav>
    </div>
  </header>
  <div class="mdl-layout__drawer">
    <span class="mdl-layout__title">Material Design Lite</span>
    <nav class="mdl-navigation">
      <a class="mdl-navigation__link" href="#">Hello</a>
      <a class="mdl-navigation__link" href="#">World.</a>
      <a class="mdl-navigation__link" href="#">How</a>
      <a class="mdl-navigation__link" href="#">Are</a>
      <a class="mdl-navigation__link" href="#">You?</a>
    </nav>
  </div>
  <main class="mdl-layout__content">
    <div>Content</div>
  </main>
</div>
```

A layout with a fixed drawer that serves as sidebar navigation on larger screens. The drawer collapses and the menu icon is displayed on smaller screens.
```html
<div class="mdl-layout mdl-js-layout mdl-layout--fixed-drawer">
  <header class="mdl-layout__header">
    <div class="mdl-layout__header-row">
      <span class="mdl-layout__title">Fixed drawer layout demo</span>
    </div>
  </header>
  <div class="mdl-layout__drawer">
    <span class="mdl-layout__title">Material Design Lite</span>
    <nav class="mdl-navigation">
      <a class="mdl-navigation__link" href="#">Hello</a>
      <a class="mdl-navigation__link" href="#">World.</a>
      <a class="mdl-navigation__link" href="#">How</a>
      <a class="mdl-navigation__link" href="#">Are</a>
      <a class="mdl-navigation__link" href="#">You?</a>
    </nav>
  </div>
  <main class="mdl-layout__content">
    <div>Content</div>
  </main>
</div>
```

A layout with a fixed drawer but no header.
```html
<div class="mdl-layout mdl-js-layout mdl-layout--fixed-drawer">
  <div class="mdl-layout__drawer">
    <span class="mdl-layout__title">Material Design Lite</span>
    <nav class="mdl-navigation">
      <a class="mdl-navigation__link" href="#">Hello</a>
      <a class="mdl-navigation__link" href="#">World.</a>
      <a class="mdl-navigation__link" href="#">How</a>
      <a class="mdl-navigation__link" href="#">Are</a>
      <a class="mdl-navigation__link" href="#">You?</a>
    </nav>
  </div>
  <main class="mdl-layout__content">
    <div>Content</div>
  </main>
</div>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the layout. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-layout` | Defines container as an MDL component | Required on outer div element |
| `mdl-js-layout` | Assigns basic MDL behavior to layout | Required on outer div element |
| `mdl-layout__header` | Defines container as an MDL component | Required on header element |
| `mdl-layout-icon` | Used for adding an application icon. Gets concealed by menu icon if both are visible.  | Goes on optional icon element |
| `mdl-layout__header-row` | Defines container as MDL header row | Required on header content div |
| `mdl-layout__title` | Defines layout title text | Required on layout title span |
| `mdl-layout-spacer` | Used to align elements inside a header or drawer, by growing to fill remaining space. Commonly used for aligning elements to the right. | Goes on optional div following layout title |
| `mdl-navigation` | Defines container as MDL navigation group | Required on nav element |
| `mdl-navigation__link` | Defines anchor as MDL navigation link | Required on header and/or drawer anchor elements |
| `mdl-layout__drawer` | Defines container as MDL layout drawer | Required on drawer div element |
| `mdl-layout__content` | Defines container as MDL layout content | Required on main element |
| `mdl-layout__header--scroll` | Makes the header scroll with the content | Optional; goes on header element |
| `mdl-layout--fixed-drawer` | Makes the drawer always visible and open in larger screens | Optional; goes on outer div element (not drawer div element) |
| `mdl-layout--fixed-header` | Makes the header always visible, even in small screens | Optional; goes on outer div element |
| `mdl-layout--no-drawer-button` | Does not display a drawer button | Optional; goes on `mdl-layout` element |
| `mdl-layout--no-desktop-drawer-button` | Does not display a drawer button in desktop mode | Optional; goes on `mdl-layout` element |
| `mdl-layout--large-screen-only` | Hides an element on smaller screens | Optional; goes on any descendant of `mdl-layout` |
| `mdl-layout--small-screen-only` | Hides an element on larger screens | Optional; goes on any descendant of `mdl-layout` |
| `mdl-layout__header--waterfall` | Allows a "waterfall" effect with multiple header lines | Optional; goes on header element |
| `mdl-layout__header--waterfall-hide-top` | Hides the top rather than the bottom rows on a waterfall header | Optional; goes on header element. Requires `mdl-layout__header--waterfall` |
| `mdl-layout__header--transparent` | Makes header transparent (draws on top of layout background) | Optional; goes on header element |
| `mdl-layout__header--seamed` | Uses a header without a shadow | Optional; goes on header element |
| `mdl-layout__tab-bar` | Defines container as an MDL tab bar | Required on div element inside header (tabbed layout) |
| `mdl-layout__tab` | Defines anchor as MDL tab link | Required on tab bar anchor elements |
| `is-active` | Defines tab as default active tab | Optional; goes on tab bar anchor element and associated tab section element|
| `mdl-layout__tab-panel` | Defines container as tab content panel | Required on tab section elements |
| `mdl-layout--fixed-tabs` | Uses fixed tabs instead of the default scrollable tabs | Optional; goes on outer div element (not div inside header) |
