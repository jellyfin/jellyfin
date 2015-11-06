
<!---

This README is automatically generated from the comments in these files:
iron-autogrow-textarea.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

[![Build Status](https://travis-ci.org/PolymerElements/iron-autogrow-textarea.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-autogrow-textarea)

_[Demo and API Docs](https://elements.polymer-project.org/elements/iron-autogrow-textarea)_


##&lt;iron-autogrow-textarea&gt;


`iron-autogrow-textarea` is an element containing a textarea that grows in height as more
lines of input are entered. Unless an explicit height or the `maxRows` property is set, it will
never scroll.

Example:

    <iron-autogrow-textarea></iron-autogrow-textarea>

Because the `textarea`'s `value` property is not observable, you should use
this element's `bind-value` instead for imperative updates.

### Styling
The following custom properties and mixins are available for styling:
Custom property | Description | Default
----------------|-------------|----------
`--iron-autogrow-textarea` | Mixin applied to the textarea | `{}`


