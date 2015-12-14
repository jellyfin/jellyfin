
<!---

This README is automatically generated from the comments in these files:
paper-icon-item.html  paper-item-behavior.html  paper-item-body.html  paper-item.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

[![Build Status](https://travis-ci.org/PolymerElements/paper-item.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-item)

_[Demo and API Docs](https://elements.polymer-project.org/elements/paper-item)_


##&lt;paper-item&gt;


Material design: [Lists](https://www.google.com/design/spec/components/lists.html)

`<paper-item>` is an interactive list item. By default, it is a horizontal flexbox.

    <paper-item>Item</paper-item>

Use this element with `<paper-item-body>` to make Material Design styled two-line and three-line
items.

    <paper-item>
      <paper-item-body two-line>
        <div>Show your status</div>
        <div secondary>Your status is visible to everyone</div>
      </paper-item-body>
      <iron-icon icon="warning"></iron-icon>
    </paper-item>

### Styling

The following custom properties and mixins are available for styling:

Custom property               | Description                                    | Default
------------------------------|------------------------------------------------|----------
`--paper-item-min-height`     | Minimum height of the item                     | `48px`
`--paper-item`                | Mixin applied to the item                      | `{}`
`--paper-item-selected-weight`| The font weight of a selected item             | `bold`
`--paper-item-selected`       | Mixin applied to selected paper-items                | `{}`
`--paper-item-disabled-color` | The color for disabled paper-items             | `--disabled-text-color`
`--paper-item-disabled`       | Mixin applied to disabled paper-items        | `{}`
`--paper-item-focused`        | Mixin applied to focused paper-items         | `{}`
`--paper-item-focused-before` | Mixin applied to :before focused paper-items | `{}`

### Accessibility

This element has `role="listitem"` by default. Depending on usage, it may be more appropriate to set
`role="menuitem"`, `role="menuitemcheckbox"` or `role="menuitemradio"`.

    <paper-item role="menuitemcheckbox">
      <paper-item-body>
        Show your status
      </paper-item-body>
      <paper-checkbox></paper-checkbox>
    </paper-item>



##&lt;paper-icon-item&gt;


`<paper-icon-item>` is a convenience element to make an item with icon. It is an interactive list
item with a fixed-width icon area, according to Material Design. This is useful if the icons are of
varying widths, but you want the item bodies to line up. Use this like a `<paper-item>`. The child
node with the attribute `item-icon` is placed in the icon area.

    <paper-icon-item>
      <iron-icon icon="favorite" item-icon></iron-icon>
      Favorite
    </paper-icon-item>
    <paper-icon-item>
      <div class="avatar" item-icon></div>
      Avatar
    </paper-icon-item>

### Styling

The following custom properties and mixins are available for styling:

Custom property               | Description                                    | Default
------------------------------|------------------------------------------------|----------
`--paper-item-icon-width`     | Width of the icon area                         | `56px`
`--paper-icon-item`           | Mixin applied to the item                      | `{}`
`--paper-item-selected-weight`| The font weight of a selected item             | `bold`
`--paper-item-selected`       | Mixin applied to selected paper-items                | `{}`
`--paper-item-disabled-color` | The color for disabled paper-items             | `--disabled-text-color`
`--paper-item-disabled`       | Mixin applied to disabled paper-items        | `{}`
`--paper-item-focused`        | Mixin applied to focused paper-items         | `{}`
`--paper-item-focused-before` | Mixin applied to :before focused paper-items | `{}`


##&lt;paper-item-body&gt;


Use `<paper-item-body>` in a `<paper-item>` or `<paper-icon-item>` to make two- or
three- line items. It is a flex item that is a vertical flexbox.

    <paper-item>
      <paper-item-body two-line>
        <div>Show your status</div>
        <div secondary>Your status is visible to everyone</div>
      </paper-item-body>
    </paper-item>

The child elements with the `secondary` attribute is given secondary text styling.

### Styling

The following custom properties and mixins are available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-item-body-two-line-min-height`   | Minimum height of a two-line item          | `72px`
`--paper-item-body-three-line-min-height` | Minimum height of a three-line item        | `88px`
`--paper-item-body-secondary-color`       | Foreground color for the `secondary` area  | `--secondary-text-color`
`--paper-item-body-secondary`             | Mixin applied to the `secondary` area      | `{}`



<!-- No docs for Polymer.PaperItemBehavior found. -->
