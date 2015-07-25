# paper-checkbox

`paper-checkbox` is a button that can be either checked or unchecked.  User
can tap the checkbox to check or uncheck it.  Usually you use checkboxes
to allow user to select multiple options from a set.  If you have a single
ON/OFF option, avoid using a single checkbox and use `paper-toggle-button`
instead.

Example:

```html
<paper-checkbox>label</paper-checkbox>

<paper-checkbox checked>label</paper-checkbox>
```

Styling a checkbox:

```html
<style is="custom-style">
  paper-checkbox {
    --paper-checkbox-label-color: #000;
    --paper-checkbox-checkmark-color: #fff;

    /* Unhecked state colors. */
    --paper-checkbox-unchecked-color: #5a5a5a;
    --paper-checkbox-unchecked-background-color: #5a5a5a;
    --paper-checkbox-unchecked-ink-color: #5a5a5a;

    /* Checked state colors. */
    --paper-checkbox-checked-color: #009688;
    --paper-checkbox-checked-ink-color: #009688;
  }
</style>
```
