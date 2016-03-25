## Introduction

The Material Design Lite (MDL) **footer** component is a comprehensive container intended to present a substantial amount of related content in a visually attractive and logically intuitive area. Although it is called "footer", it may be placed at any appropriate location on a device screen, either before or after other content.

An MDL footer component takes two basic forms: *mega-footer* and *mini-footer*. As the names imply, mega-footers contain more (and more complex) content than mini-footers. A mega-footer presents multiple sections of content separated by horizontal rules, while a mini-footer presents a single section of content. Both footer forms have their own internal structures, including required and optional elements, and typically include both informational and clickable content, such as links.

Footers, as represented by this component, are a fairly new feature in user interfaces, and allow users to view discrete blocks of content in a coherent and consistently organized way. Their design and use is an important factor in the overall user experience.

### To include an MDL **mega-footer** component:

&nbsp;1a. Code a `<footer>` element. Inside the footer, include one `<div>` element for each content section, typically three: *top*, *middle*, and *bottom*.
```html
<footer>
  <div>
  ...
  </div>
  <div>
  ...
  </div>
  <div>
  ...
  </div>
</footer>
```
&nbsp;1b. Add the appropriate MDL classes to the footer and divs using the `class` attribute.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
  ...
  </div>
  <div class="mdl-mega-footer__middle-section">
  ...
  </div>
  <div class="mdl-mega-footer__bottom-section">
  ...
  </div>
</footer>
```
&nbsp;2a. Inside the top section div, code two sibling "inner" divs for the *left* and *right* content sections.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
    <div>
    ...
    </div>
    <div>
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__middle-section">
  ...
  </div>
  <div class="mdl-mega-footer__bottom-section">
  ...
  </div>
</footer>
```
&nbsp;2b. Add the appropriate MDL classes to the two "inner" left and right divs using the `class` attribute.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
    <div class="mdl-mega-footer__left-section">
    ...
    </div>
    <div class="mdl-mega-footer__right-section">
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__middle-section">
  ...
  </div>
  <div class="mdl-mega-footer__bottom-section">
  ...
  </div>
</footer>
```
&nbsp;3a. Inside the middle section div, code one or more sibling "inner" divs for the *drop-down* content sections. That is, for two drop-down sections, you would code two divs.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
    <div class="mdl-mega-footer__left-section">
    ...
    </div>
    <div class="mdl-mega-footer__right-section">
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__middle-section">
    <div>
    ...
    </div>
    <div>
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__bottom-section">
  ...
  </div>
</footer>
```
&nbsp;3b. Add the appropriate MDL classes to the two "inner" drop-down divs using the `class` attribute.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
    <div class="mdl-mega-footer__left-section">
    ...
    </div>
    <div class="mdl-mega-footer__right-section">
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__middle-section">
    <div class="mdl-mega-footer__drop-down-section">
    ...
    </div>
    <div class="mdl-mega-footer__drop-down-section">
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__bottom-section">
  ...
  </div>
</footer>
```
&nbsp;4a. Inside the bottom section div, code an "inner" div for the section heading and a sibling unordered list for the bottom section links.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
    <div class="mdl-mega-footer__left-section">
    ...
    </div>
    <div class="mdl-mega-footer__right-section">
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__middle-section">
    <div class="mdl-mega-footer__drop-down-section">
    ...
    </div>
    <div class="mdl-mega-footer__drop-down-section">
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__bottom-section">
    <div>
      ...
    </div>
    <ul>
      ...
    </ul>
  </div>
</footer>
```
&nbsp;4b. Add the appropriate MDL classes to the "inner" div heading and list using the `class` attribute.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
    <div class="mdl-mega-footer__left-section">
    ...
    </div>
    <div class="mdl-mega-footer__right-section">
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__middle-section">
    <div class="mdl-mega-footer__drop-down-section">
    ...
    </div>
    <div class="mdl-mega-footer__drop-down-section">
    ...
    </div>
  </div>
  <div class="mdl-mega-footer__bottom-section">
    <div class="mdl-logo">
    </div>
    <ul class="mdl-mega-footer__link-list">
      ...
    </ul>
  </div>
</footer>
```
&nbsp;5. Add content to the top (left and right), middle (drop-downs), and bottom (text and links) sections of the footer; include any appropriate MDL classes using the `class` attribute.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
    <div class="mdl-mega-footer__left-section">
      <button class="mdl-mega-footer__social-btn"></button>
      <button class="mdl-mega-footer__social-btn"></button>
      <button class="mdl-mega-footer__social-btn"></button>
    </div>
    <div class="mdl-mega-footer__right-section">
      <a href="">Link 1</a>
      <a href="">Link 2</a>
      <a href="">Link 3</a>
    </div>
  </div>
  <div class="mdl-mega-footer__middle-section">
    <div class="mdl-mega-footer__drop-down-section">
      <h1 class="mdl-mega-footer__heading">Drop-down 1 Heading</h1>
      <ul class="mdl-mega-footer__link-list">
        <li><a href="">Link A</a></li>
        <li><a href="">Link B</a></li>
        <li><a href="">Link C</a></li>
        <li><a href="">Link D</a></li>
      </ul>
    </div>
    <div class="mdl-mega-footer__drop-down-section">
      <h1 class="mdl-mega-footer__heading">Drop-down 2 Heading</h1>
      <ul class="mdl-mega-footer__link-list">
        <li><a href="">Link A</a></li>
        <li><a href="">Link B</a></li>
        <li><a href="">Link C</a></li>
      </ul>
    </div>
  </div>
  <div class="mdl-mega-footer__bottom-section">
    <div class="mdl-logo">
    Mega-Footer Bottom Section Heading
    </div>
    <ul class="mdl-mega-footer__link-list">
      <li><a href="">Link A</a></li>
      <li><a href="">Link B</a></li>
    </ul>
  </div>
</footer>
```

The mega-footer component is ready for use.

#### Examples

A mega-footer component with three sections and two drop-down sections in the middle section.
```html
<footer class="mdl-mega-footer">
  <div class="mdl-mega-footer__top-section">
    <div class="mdl-mega-footer__left-section">
      <button class="mdl-mega-footer__social-btn"></button>
      <button class="mdl-mega-footer__social-btn"></button>
      <button class="mdl-mega-footer__social-btn"></button>
    </div>
    <div class="mdl-mega-footer__right-section">
      <a href="#">Introduction</a>
      <a href="#">App Status Dashboard</a>
      <a href="#">Terms of Service</a>
    </div>
  </div>
  <div class="mdl-mega-footer__middle-section">
    <div class="mdl-mega-footer__drop-down-section">
      <h1 class="mdl-mega-footer__heading">Learning and Support</h1>
      <ul class="mdl-mega-footer__link-list">
        <li><a href="#">Resource Center</a></li>
        <li><a href="#">Help Center</a></li>
        <li><a href="#">Community</a></li>
        <li><a href="#">Learn with Google</a></li>
        <li><a href="#">Small Business Community</a></li>
        <li><a href="#">Think Insights</a></li>
      </ul>
    </div>
    <div class="mdl-mega-footer__drop-down-section">
      <h1 class="mdl-mega-footer__heading">Just for Developers</h1>
      <ul class="mdl-mega-footer__link-list">
        <li><a href="#">Google Developers</a></li>
        <li><a href="#">AdWords API</a></li>
        <li><a href="#">AdWords Scipts</a></li>
        <li><a href="#">AdWords Remarketing Tag</a></li>
      </ul>
    </div>
  </div>
  <div class="mdl-mega-footer__bottom-section">
    <div class="mdl-logo">
      More Information
    </div>
    <ul class="mdl-mega-footer__link-list">
      <li><a href="#">Help</a></li>
      <li><a href="#">Privacy and Terms</a></li>
    </ul>
  </div>
</footer>
```

### To include an MDL **mini-footer** component:

&nbsp;1a. Code a `<footer>` element. Inside the footer, code two `<div>` elements, one for the *left* section and one for the *right* section.
```html
<footer>
  <div>
  ...
  </div>
  <div>
  ...
  </div>
</footer>
```
&nbsp;1b. Add the appropriate MDL classes to the footer and divs using the `class` attribute.
```html
<footer class="mdl-mini-footer">
  <div class="mdl-mini-footer__left-section">
  ...
  </div>
  <div class="mdl-mini-footer__right-section">
  ...
  </div>
</footer>
```
&nbsp;2a. Inside the left section div, code an "inner" div for the section heading and a sibling unordered list for the left section links.
```html
<footer class="mdl-mini-footer">
  <div class="mdl-mini-footer__left-section">
    <div>
      ...
    </div>
    <ul>
      ...
    </ul>
  </div>
  <div class="mdl-mini-footer__right-section">
  ...
  </div>
</footer>
```
&nbsp;2b. Add the appropriate MDL classes to the "inner" div and list using the `class` attribute.
```html
<footer class="mdl-mini-footer">
  <div class="mdl-mini-footer__left-section">
    <div class="mdl-logo">
      ...
    </div>
    <ul class="mdl-mini-footer__link-list">
      ...
    </ul>
  </div>
  <div class="mdl-mini-footer__right-section">
  ...
  </div>
</footer>
```
&nbsp;3. Add content to the left (text and links) and right (text or decoration) sections of the footer; include any appropriate MDL classes using the `class` attribute.
```html
<footer class="mdl-mini-footer">
  <div class="mdl-mini-footer__left-section">
    <div class="mdl-logo">
      Mini-footer Heading
    </div>
    <ul class="mdl-mini-footer__link-list">
      <li><a href="">Link 1</a></li>
      <li><a href="">Link 2</a></li>
      <li><a href="">Link 3</a></li>
    </ul>
  </div>
  <div class="mdl-mini-footer__right-section">
    <button class="mdl-mini-footer__social-btn"></button>
    <button class="mdl-mini-footer__social-btn"></button>
    <button class="mdl-mini-footer__social-btn"></button>
  </div>
</footer>
```

The mini-footer component is ready for use.

#### Examples

A mini-footer with left and right sections.

```html
<footer class="mdl-mini-footer">
  <div class="mdl-mini-footer__left-section">
    <div class="mdl-logo">
      More Information
    </div>
    <ul class="mdl-mini-footer__link-list">
      <li><a href="#">Help</a></li>
      <li><a href="#">Privacy and Terms</a></li>
      <li><a href="#">User Agreement</a></li>
    </ul>
  </div>
  <div class="mdl-mini-footer__right-section">
    <button class="mdl-mini-footer__social-btn"></button>
    <button class="mdl-mini-footer__social-btn"></button>
    <button class="mdl-mini-footer__social-btn"></button>
  </div>
</footer>
```

## Configuration options

The MDL CSS classes apply various predefined visual enhancements to the footer. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-mega-footer` | Defines container as an MDL mega-footer component | Required on footer element |
| `mdl-mega-footer__top-section` | Defines container as a footer top section | Required on top section "outer" div element |
| `mdl-mega-footer__left-section` | Defines container as a left section | Required on left section "inner" div element |
| `mdl-mega-footer__social-btn` | Defines a decorative square within mega-footer | Required on button element (if used) |
| `mdl-mega-footer__right-section` | Defines container as a right section | Required on right section "inner" div element |
| `mdl-mega-footer__middle-section` | Defines container as a footer middle section | Required on middle section "outer" div element |
| `mdl-mega-footer__drop-down-section` | Defines container as a drop-down (vertical) content area | Required on drop-down "inner" div elements |
| `mdl-mega-footer__heading` | Defines a heading as a mega-footer heading | Required on h1 element inside drop-down section |
| `mdl-mega-footer__link-list` | Defines an unordered list as a drop-down (vertical) list | Required on ul element inside drop-down section |
| `mdl-mega-footer__bottom-section` | Defines container as a footer bottom section | Required on bottom section "outer" div element |
| `mdl-logo` | Defines a container as a styled section heading | Required on "inner" div element in mega-footer bottom-section or mini-footer left-section |
| `mdl-mini-footer` | Defines container as an MDL mini-footer component | Required on footer element |
| `mdl-mini-footer__left-section` | Defines container as a left section | Required on left section "inner" div element |
| `mdl-mini-footer__link-list` | Defines an unordered list as an inline (horizontal) list | Required on ul element sibling to "mdl-logo" div element |
| `mdl-mini-footer__right-section` | Defines container as a right section | Required on right section "inner" div element |
| `mdl-mini-footer__social-btn` | Defines a decorative square within mini-footer | Required on button element (if used) |
