paper-dropdown-menu
===================

`paper-dropdown-menu` is similar to a native browser select element.
`paper-dropdown-menu` works with selectable content. The currently selected
item is displayed in the control. If no item is selected, the `label` is
displayed instead.
The child element with the class `dropdown-content` will be used as the dropdown
menu. It could be a `paper-menu` or element that triggers `iron-activate` when
selecting its children.
Example:

    <paper-dropdown-menu label="Your favourite pastry">
      <paper-menu class="dropdown-content">
        <paper-item>Croissant</paper-item>
        <paper-item>Donut</paper-item>
        <paper-item>Financier</paper-item>
        <paper-item>Madeleine</paper-item>
      </paper-menu>
    </paper-dropdown-menu>
    
This example renders a dropdown menu with 4 options.
### Styling
The following custom properties and mixins are also available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-dropdown-menu` | A mixin that is applied to the element host | `{}`
`--paper-dropdown-menu-disabled` | A mixin that is applied to the element host when disabled | `{}`
`--paper-dropdown-menu-ripple` | A mixin that is applied to the internal ripple | `{}`
`--paper-dropdown-menu-button` | A mixin that is applied to the internal menu button | `{}`
`--paper-dropdown-menu-input` | A mixin that is applied to the internal paper input | `{}`
`--paper-dropdown-menu-icon` | A mixin that is applied to the internal icon | `{}`
You can also use any of the `paper-input-container` and `paper-menu-button`
style mixins and custom properties to style the internal input and menu button
respectively.
