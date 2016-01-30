
<!---

This README is automatically generated from the comments in these files:
iron-overlay-backdrop.html  iron-overlay-behavior.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build Status](https://travis-ci.org/PolymerElements/iron-overlay-behavior.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-overlay-behavior)

_[Demo and API Docs](https://elements.polymer-project.org/elements/iron-overlay-behavior)_


##&lt;iron-overlay-backdrop&gt;

`iron-overlay-backdrop` is a backdrop used by `Polymer.IronOverlayBehavior`. It should be a
singleton.

### Styling

The following custom properties and mixins are available for styling.

| Custom property | Description | Default |
| --- | --- | --- |
| `--iron-overlay-backdrop-background-color` | Backdrop background color | #000 |
| `--iron-overlay-backdrop-opacity` | Backdrop opacity | 0.6 |
| `--iron-overlay-backdrop` | Mixin applied to `iron-overlay-backdrop`. | {} |
| `--iron-overlay-backdrop-opened` | Mixin applied to `iron-overlay-backdrop` when it is displayed | {} |



##Polymer.IronOverlayBehavior

Use `Polymer.IronOverlayBehavior` to implement an element that can be hidden or shown, and displays
on top of other content. It includes an optional backdrop, and can be used to implement a variety
of UI controls including dialogs and drop downs. Multiple overlays may be displayed at once.

### Closing and canceling

A dialog may be hidden by closing or canceling. The difference between close and cancel is user
intent. Closing generally implies that the user acknowledged the content on the overlay. By default,
it will cancel whenever the user taps outside it or presses the escape key. This behavior is
configurable with the `no-cancel-on-esc-key` and the `no-cancel-on-outside-click` properties.
`close()` should be called explicitly by the implementer when the user interacts with a control
in the overlay element. When the dialog is canceled, the overlay fires an 'iron-overlay-canceled'
event. Call `preventDefault` on this event to prevent the overlay from closing.

### Positioning

By default the element is sized and positioned to fit and centered inside the window. You can
position and size it manually using CSS. See `Polymer.IronFitBehavior`.

### Backdrop

Set the `with-backdrop` attribute to display a backdrop behind the overlay. The backdrop is
appended to `<body>` and is of type `<iron-overlay-backdrop>`. See its doc page for styling
options.

### Limitations

The element is styled to appear on top of other content by setting its `z-index` property. You
must ensure no element has a stacking context with a higher `z-index` than its parent stacking
context. You should place this element as a child of `<body>` whenever possible.


