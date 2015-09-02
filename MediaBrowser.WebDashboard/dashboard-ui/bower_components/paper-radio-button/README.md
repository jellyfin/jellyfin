# paper-radio-button

`paper-radio-button` is a button that can be either checked or unchecked.
User can tap the radio button to check or uncheck it.

Use a `<paper-radio-group>` to group a set of radio buttons.  When radio buttons
are inside a radio group, exactly one radio button in the group can be checked
at any time.
Example:

```html
<paper-radio-button></paper-radio-button>
<paper-radio-button>Item label</paper-radio-button>
```
Styling a radio button:

```html
<style is="custom-style">
  :root {
    /* Unchecked state colors. */
    --paper-radio-button-unchecked-color: #5a5a5a;
    --paper-radio-button-unchecked-background-color: #fff;
    --paper-radio-button-unchecked-ink-color: #5a5a5a;

    /* Checked state colors. */
    --paper-radio-button-checked-color: #009688;
    --paper-radio-button-checked-ink-color: #0f9d58;

    --paper-radio-button-label-color: black;
  }
</style>
```
