
<!---

This README is automatically generated from the comments in these files:
paper-menu-button-animations.html  paper-menu-button.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

[![Build Status](https://travis-ci.org/PolymerElements/paper-menu-button.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-menu-button)

_[Demo and API Docs](https://elements.polymer-project.org/elements/paper-menu-button)_


##&lt;paper-menu-button&gt;


Material design: [Dropdown buttons](https://www.google.com/design/spec/components/buttons.html#buttons-dropdown-buttons)

`paper-menu-button` allows one to compose a designated "trigger" element with
another element that represents "content", to create a dropdown menu that
displays the "content" when the "trigger" is clicked.

The child element with the class `dropdown-trigger` will be used as the
"trigger" element. The child element with the class `dropdown-content` will be
used as the "content" element.

The `paper-menu-button` is sensitive to its content's `iron-select` events. If
the "content" element triggers an `iron-select` event, the `paper-menu-button`
will close automatically.

Example:

    <paper-menu-button>
      <paper-icon-button icon="menu" class="dropdown-trigger"></paper-icon-button>
      <paper-menu class="dropdown-content">
        <paper-item>Share</paper-item>
        <paper-item>Settings</paper-item>
        <paper-item>Help</paper-item>
      </paper-menu>
    </paper-menu-button>

### Styling

The following custom properties and mixins are also available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-menu-button-dropdown-background` | Background color of the paper-menu-button dropdown | `#fff`
`--paper-menu-button` | Mixin applied to the paper-menu-button | `{}`
`--paper-menu-button-disabled` | Mixin applied to the paper-menu-button when disabled | `{}`
`--paper-menu-button-dropdown` | Mixin applied to the paper-menu-button dropdown | `{}`
`--paper-menu-button-content` | Mixin applied to the paper-menu-button content | `{}`



<!-- No docs for <paper-menu-grow-height-animation> found. -->

<!-- No docs for <paper-menu-grow-width-animation> found. -->

<!-- No docs for <paper-menu-shrink-height-animation> found. -->

<!-- No docs for <paper-menu-shrink-width-animation> found. -->
