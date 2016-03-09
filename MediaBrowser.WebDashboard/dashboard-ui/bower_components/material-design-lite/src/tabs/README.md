## Introduction

The Material Design Lite (MDL) **tab** component is a user interface element that allows different content blocks to share the same screen space in a mutually exclusive manner. Tabs are always presented in sets of two or more, and they make it easy to explore and switch among different views or functional aspects of an app, or to browse categorized data sets individually. Tabs serve as "headings" for their respective content; the *active* tab &mdash; the one whose content is currently displayed &mdash; is always visually distinguished from the others so the user knows which heading the current content belongs to.

Tabs are an established but non-standardized feature in user interfaces, and allow users to view different, but often related, blocks of content (often called *panels*). Tabs save screen real estate and provide intuitive and logical access to data while reducing navigation and associated user confusion. Their design and use is an important factor in the overall user experience. See the tab component's [Material Design specifications page](http://www.google.com/design/spec/components/tabs.html) for details.

### To include a set of MDL **tab** components:

&nbsp;1. Code a `<div>` element; this is the "outer" div, intended to contain all of the tabs and their content.
```html
<div>
</div>
```
&nbsp;2. Inside the "outer" div, code one "inner" div for the tabs themselves, and one for each tab's content, all siblings. That is, for three content tabs, you would code four "inner" divs.
```html
<div>
  <div>
  ...
  </div>
  <div>
  ...
  </div>
  <div>
  ...
  </div>
  <div>
  ...
  </div>
</div>
```
&nbsp;3. Inside the first "inner" div (the tabs), code one anchor `<a>` (link) tag for each tab. Include `href` attributes with values to match the tabs' `id` attribute values, and some brief link text. On the remaining "inner" divs (the content), code `id` attributes whose values match the links' `href`s.
```html
<div>
  <div>
    <a href="#tab1">Tab One</a>
    <a href="#tab2">Tab Two</a>
    <a href="#tab3">Tab Three</a>
  </div>
  <div id="tab1">
  ...
  </div>
  <div id="tab2">
  ...
  </div>
  <div id="tab3">
  ...
  </div>
</div>
```
&nbsp;4. Inside the remaining "inner" divs, code the content you intend to display in each panel; use standard HTML tags to structure the content as desired.
```html
<div>
  <div>
    <a href="#tab1">Tab One</a>
    <a href="#tab2">Tab Two</a>
    <a href="#tab3">Tab Three</a>
  </div>
  <div id="tab1">
    <p>First tab's content.</p>
  </div>
  <div id="tab2">
    <p>Second tab's content.</p>
  </div>
  <div id="tab3">
    <p>Third tab's content.</p>
  </div>
</div>
```
&nbsp;5. Add one or more MDL classes, separated by spaces, to the "outer" and "inner" divs using the `class` attribute; be sure to include the `is-active` class on the tab you want to be displayed by default.
```html
<div class="mdl-tabs mdl-js-tabs">
  <div class="mdl-tabs__tab-bar">
    <a href="#tab1" class="mdl-tabs__tab">Tab One</a>
    <a href="#tab2" class="mdl-tabs__tab">Tab Two</a>
    <a href="#tab3" class="mdl-tabs__tab">Tab Three</a>
  </div>
  <div class="mdl-tabs__panel is-active" id="tab1">
    <p>First tab's content.</p>
  </div>
  <div class="mdl-tabs__panel" id="tab2">
    <p>Second tab's content.</p>
  </div>
  <div class="mdl-tabs__panel" id="tab3">
    <p>Third tab's content.</p>
  </div>
</div>
```

The tab components are ready for use.

#### Example

Three tabs, with ripple effect on tab links.

```html
<div class="mdl-tabs mdl-js-tabs mdl-js-ripple-effect">
  <div class="mdl-tabs__tab-bar">
    <a href="#about-panel" class="mdl-tabs__tab is-active">About the Beatles</a>
    <a href="#members-panel" class="mdl-tabs__tab">Members</a>
    <a href="#albums-panel" class="mdl-tabs__tab">Discography</a>
  </div>
  <div class="mdl-tabs__panel is-active" id="about-panel">
    <p><b>The Beatles</b> were a four-piece musical group from Liverpool, England.
    Formed in 1960, their career spanned just over a decade, yet they are widely
    regarded as the most influential band in history.</p>
    <p>Their songs are among the best-loved music of all time. It is said that every
    minute of every day, a radio station somewhere is playing a Beatles song.</p>
  </div>
  <div class="mdl-tabs__panel" id="members-panel">
    <p>The Beatles' members were:</p>
    <ul>
      <li>John Lennon (1940-1980)</li>
      <li>Paul McCartney (1942-)</li>
      <li>George Harrison (1943-2001)</li>
      <li>Ringo Starr (1940-)</li>
    </ul>
  </div>
  <div class="mdl-tabs__panel" id="albums-panel">
    <p>The Beatles' original UK LPs, in order of release:</p>
    <ol>
      <li>Please Please Me (1963)</li>
      <li>With the Beatles (1963)</li>
      <li>A Hard Day's Night (1964)</li>
      <li>Beatles for Sale (1964)</li>
      <li>Help! (1965)</li>
      <li>Rubber Soul (1965)</li>
      <li>Revolver (1966)</li>
      <li>Sgt. Pepper's Lonely Hearts Club Band (1967)</li>
      <li>The Beatles ("The White Album", 1968)</li>
      <li>Yellow Submarine (1969)</li>
      <li>Abbey Road (1969)</li>
      <li>Let It Be (1970)</li>
    </ol>
  </div>
</div>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the tabs. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-tabs` | Defines a tabs container as an MDL component | Required on "outer" div element |
| `mdl-js-tabs` | Assigns basic MDL behavior to tabs container | Required on "outer" div element|
| `mdl-js-ripple-effect` | Applies *ripple* click effect to tab links | Optional; goes on "outer" div element|
| `mdl-tabs__tab-bar` | Defines a container as an MDL tabs link bar | Required on first "inner" div element |
| `mdl-tabs__tab` | Defines an anchor (link) as an MDL tab activator | Required on all links in first "inner" div element |
| `is-active` | Defines a tab as the default display tab | Required on one (and only one) of the "inner" div (tab) elements |
| `mdl-tabs__panel` | Defines a container as tab content | Required on each of the "inner" div (tab) elements |
