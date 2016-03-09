## Introduction

The Material Design Lite (MDL) **badge** component is an onscreen notification element. A badge consists of a small circle, typically containing a number or other characters, that appears in proximity to another object. A badge can be both a notifier that there are additional items associated with an object and an indicator of how many items there are.

You can use a badge to unobtrusively draw the user's attention to items they might not otherwise notice, or to emphasize that items may need their attention. For example:

* A "New messages" notification might be followed by a badge containing the number of unread messages.
* A "You have unpurchased items in your shopping cart" reminder might include a badge showing the number of items in the cart.
* A "Join the discussion!" button might have an accompanying badge indicating the number of users currently participating in the discussion.

A badge is almost always positioned near a link so that the user has a convenient way to access the additional information indicated by the badge. However, depending on the intent, the badge itself may or may not be part of the link.

Badges are a new feature in user interfaces, and provide users with a visual clue to help them discover additional relevant content. Their design and use is therefore an important factor in the overall user experience.

### To include an MDL **badge** component:

&nbsp;1. Code  an `<a>` (anchor/link) or a `<span>` element. Include any desired attributes and content.
```html
<a href="#">This link has a badge.</a>
```
&nbsp;2. Add one or more MDL classes, separated by spaces, to the element using the `class` attribute.
```html
<a href="#" class="mdl-badge">This link has a badge.</a>
```
&nbsp;3. Add a `data-badge` attribute and quoted string value for the badge.
```html
<a href="#" class="mdl-badge" data-badge="5">This link has a badge.</a>
```

The badge component is ready for use.

>**Note:** Because of the badge component's small size, the `data-badge` value should typically contain one to three characters. More than three characters will not cause an error, but some characters may fall outside the badge and thus be difficult or impossible to see. The value of the `data-badge` attribute is centered in the badge.

#### Examples

A badge inside a link.
```html
<a href="#" class="mdl-badge" data-badge="7">This link contains a badge.</a>
```

A badge near, but not included in, a link.
```html
<a href="#">This link is followed by a badge.</a>
<span class="mdl-badge" data-badge="12"></span>
```

A badge inside a link with too many characters to fit inside the badge.
```html
<a href="#" class="mdl-badge" data-badge="123456789">
This badge has too many characters.</a>
```

A badge inside a link with no badge background color.
```html
<a href="#" class="mdl-badge mdl-badge--no-background" data-badge="123">
This badge has no background color.</a>
```

## Configuration options

The MDL CSS classes apply various predefined visual enhancements to the badge. The table below lists the available classes and their effects.

| MDL class | Effect | Remarks |
|-----------|--------|---------|
| `mdl-badge` | Defines badge as an MDL component | Required on span or link |
| `mdl-badge--no-background` | Applies open-circle effect to badge | Optional |
| `mdl-badge--overlap` | Make the badge overlap with its container | Optional |
| `data-badge="value"` | Assigns string value to badge | Not a class, but a separate attribute; required on span or link |
