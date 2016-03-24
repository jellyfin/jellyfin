
<!---

This README is automatically generated from the comments in these files:
paper-dialog-behavior.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/paper-dialog-behavior.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-dialog-behavior)

_[Demo and API docs](https://elements.polymer-project.org/elements/paper-dialog-behavior)_


##Polymer.PaperDialogBehavior

Use `Polymer.PaperDialogBehavior` and `paper-dialog-shared-styles.html` to implement a Material Design
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

`paper-dialog-shared-styles.html` provide styles for a header, content area, and an action area for buttons.
Use the `<h2>` tag for the header and the `buttons` class for the action area. You can use the
`paper-dialog-scrollable` element (in its own repository) if you need a scrolling content area.

Use the `dialog-dismiss` and `dialog-confirm` attributes on interactive controls to close the
dialog. If the user dismisses the dialog with `dialog-confirm`, the `closingReason` will update
to include `confirmed: true`.

### Styling

The following custom properties and mixins are available for styling.

| Custom property | Description | Default |
| --- | --- | --- |
| `--paper-dialog-background-color` | Dialog background color | `--primary-background-color` |
| `--paper-dialog-color` | Dialog foreground color | `--primary-text-color` |
| `--paper-dialog` | Mixin applied to the dialog | `{}` |
| `--paper-dialog-title` | Mixin applied to the title (`<h2>`) element | `{}` |
| `--paper-dialog-button-color` | Button area foreground color | `--default-primary-color` |

### Accessibility

This element has `role="dialog"` by default. Depending on the context, it may be more appropriate
to override this attribute with `role="alertdialog"`.

If `modal` is set, the element will set `aria-modal` and prevent the focus from exiting the element.
It will also ensure that focus remains in the dialog.


