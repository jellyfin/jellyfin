
<!---

This README is automatically generated from the comments in these files:
iron-dropdown.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

[![Build Status](https://travis-ci.org/PolymerElements/iron-dropdown.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-dropdown)

_[Demo and API Docs](https://elements.polymer-project.org/elements/iron-dropdown)_


##&lt;iron-dropdown&gt;


`<iron-dropdown>` is a generalized element that is useful when you have
hidden content (`.dropdown-content`) that is revealed due to some change in
state that should cause it to do so.

Note that this is a low-level element intended to be used as part of other
composite elements that cause dropdowns to be revealed.

Examples of elements that might be implemented using an `iron-dropdown`
include comboboxes, menubuttons, selects. The list goes on.

The `<iron-dropdown>` element exposes attributes that allow the position
of the `.dropdown-content` relative to the `.dropdown-trigger` to be
configured.

    <iron-dropdown horizontal-align="right" vertical-align="top">
      <div class="dropdown-content">Hello!</div>
    </iron-dropdown>

In the above example, the `<div>` with class `.dropdown-content` will be
hidden until the dropdown element has `opened` set to true, or when the `open`
method is called on the element.


