# paper-dialog-behavior

`paper-dialog-behavior` implements behavior related to a Material Design dialog. Use this behavior
and include `paper-dialog-common.css` in your element to implement a dialog.

`paper-dialog-common.css` provide styles for a header, content area, and an action area for buttons.
Use the `<h2>` tag for the header and the `buttons` class for the action area. You can use the
`paper-dialog-scrollable` element (in its own repository) if you need a scrolling content area.

Use the `dialog-dismiss` and `dialog-confirm` attributes on interactive controls to close the
dialog.

For example, if `<paper-dialog-impl>` implements this behavior:

```html
<paper-dialog-impl>
    <h2>Header</h2>
    <div>Dialog body</div>
    <div class="buttons">
        <paper-button dialog-dismiss>Cancel</paper-button>
        <paper-button dialog-confirm>Accept</paper-button>
    </div>
</paper-dialog-impl>
```
