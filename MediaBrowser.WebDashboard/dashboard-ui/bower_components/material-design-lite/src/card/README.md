## Introduction

The Material Design Lite (MDL) **card** component is a user interface element representing a virtual piece of paper that contains related data &mdash; such as a photo, some text, and a link &mdash; that are all about a single subject.

Cards are a convenient means of coherently displaying related content that is composed of different types of objects. They are also well-suited for presenting similar objects whose size or supported actions can vary considerably, like photos with captions of variable length. Cards have a constant width and a variable height, depending on their content.

Cards are a fairly new feature in user interfaces, and allow users an access point to more complex and detailed information. Their design and use is an important factor in the overall user experience. See the card component's [Material Design specifications page](http://www.google.com/design/spec/components/cards.html) for details.

### To include an MDL **card** component:

&nbsp;1. Code a `<div>` element; this is the "outer" container, intended to hold all of the card's content.
```html
<div>
</div>
```
&nbsp;2. Inside the div, code one or more "inner" divs, one for each desired content block. A card containing a title, an image, some text, and an action bar would contain four "inner" divs, all siblings.
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
&nbsp;3. Add one or more MDL classes, separated by spaces, to the "outer" div and the "inner" divs (depending on their intended use) using the `class` attribute.
```html
<div class="mdl-card">
  <div class="mdl-card__title">
  ...
  </div>
  <div class="mdl-card__media">
  ...
  </div>
  <div class="mdl-card__supporting-text">
  ...
  </div>
  <div class="mdl-card__actions">
  ...
  </div>
</div>
```
&nbsp;4. Add content to each "inner" div, again depending on its intended use, using standard HTML elements and, optionally, additional MDL classes.
```html
<div class="mdl-card">
  <div class="mdl-card__title">
    <h2 class="mdl-card__title-text">title Text Goes Here</h2>
  </div>
  <div class="mdl-card__media">
    <img src="photo.jpg" width="220" height="140" border="0" alt="" style="padding:20px;">
  </div>
  <div class="mdl-card__supporting-text">
    This text might describe the photo and provide further information, such as where and
    when it was taken.
  </div>
  <div class="mdl-card__actions">
    <a href="(URL or function)">Related Action</a>
  </div>
</div>
```

The card component is ready for use.

#### Examples

A card (no shadow) with a title, image, text, and action.

```html
<div class="mdl-card">
  <div class="mdl-card__title">
     <h2 class="mdl-card__title-text">Auckland Sky Tower<br>Auckland, New Zealand</h2>
  </div>
  <div class="mdl-card__media">
    <img src="skytower.jpg" width="173" height="157" border="0" alt=""
     style="padding:10px;">
  </div>
  <div class="mdl-card__supporting-text">
  The Sky Tower is an observation and telecommunications tower located in Auckland,
  New Zealand. It is 328 metres (1,076 ft) tall, making it the tallest man-made structure
  in the Southern Hemisphere.
  </div>
  <div class="mdl-card__actions">
     <a href="http://en.wikipedia.org/wiki/Sky_Tower_%28Auckland%29">Wikipedia entry</a>
  </div>
</div>
```

Card (level-3 shadow) with an image, caption, and text:

```html
<div class="mdl-card mdl-shadow--4dp">
  <div class="mdl-card__media"><img src="skytower.jpg" width="173" height="157" border="0"
   alt="" style="padding:10px;">
  </div>
  <div class="mdl-card__supporting-text">
    Auckland Sky Tower, taken March 24th, 2014
  </div>
  <div class="mdl-card__supporting-text">
  The Sky Tower is an observation and telecommunications tower located in Auckland,
  New Zealand. It is 328 metres (1,076 ft) tall, making it the tallest man-made structure
  in the Southern Hemisphere.
  </div>
</div>
```

## Configuration options

The MDL CSS classes apply various predefined visual and behavioral enhancements to the card. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-card` | Defines div element as an MDL card container | Required on "outer" div |
| `mdl-card--border` | Adds a border to the card section that it's applied to | Used on the "inner" divs |
| `mdl-shadow--2dp through mdl-shadow--16dp` | Assigns variable shadow depths (2, 3, 4, 6, 8, or 16) to card | Optional, goes on "outer" div; if omitted, no shadow is present |
| `mdl-card__title` | Defines div as a card title container | Required on "inner" title div |
| `mdl-card__title-text` | Assigns appropriate text characteristics to card title | Required on head tag (H1 - H6) inside title div |
| `mdl-card__subtitle-text` | Assigns text characteristics to a card subtitle | Optional. Should be a child of the title element. |
| `mdl-card__media` | Defines div as a card media container | Required on "inner" media div |
| `mdl-card__supporting-text` | Defines div as a card body text container and assigns appropriate text characteristics to body text | Required on "inner" body text div; text goes directly inside the div with no intervening containers |
| `mdl-card__actions` | Defines div as a card actions container and assigns appropriate text characteristics to actions text | Required on "inner" actions div; content goes directly inside the div with no intervening containers |
