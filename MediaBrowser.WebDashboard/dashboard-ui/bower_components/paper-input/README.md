
<!---

This README is automatically generated from the comments in these files:
paper-input-addon-behavior.html  paper-input-behavior.html  paper-input-char-counter.html  paper-input-container.html  paper-input-error.html  paper-input.html  paper-textarea.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

-->

[![Build Status](https://travis-ci.org/PolymerElements/paper-input.svg?branch=master)](https://travis-ci.org/PolymerElements/paper-input)

_[Demo and API Docs](https://elements.polymer-project.org/elements/paper-input)_


##&lt;paper-input&gt;


Material design: [Text fields](https://www.google.com/design/spec/components/text-fields.html)

`<paper-input>` is a single-line text field with Material Design styling.

    <paper-input label="Input label"></paper-input>

It may include an optional error message or character counter.

    <paper-input error-message="Invalid input!" label="Input label"></paper-input>
    <paper-input char-counter label="Input label"></paper-input>

It can also include custom prefix or suffix elements, which are displayed
before or after the text input itself. In order for an element to be
considered as a prefix, it must have the `prefix` attribute (and similarly
for `suffix`).

    <paper-input label="total">
      <div prefix>$</div>
      <paper-icon-button suffix icon="clear"></paper-icon-button>
    </paper-input>

A `paper-input` can use the native `type=search` or `type=file` features.
However, since we can't control the native styling of the input, in these cases
it's recommended to use a placeholder text, or `always-float-label`,
as to not overlap the native UI (search icon, file button, etc.).

    <paper-input label="search!" type="search"
        placeholder="search for cats" autosave="test" results="5">
    </paper-input>

See `Polymer.PaperInputBehavior` for more API docs.

### Styling

See `Polymer.PaperInputContainer` for a list of custom properties used to
style this element.



##&lt;paper-input-char-counter&gt;


`<paper-input-char-counter>` is a character counter for use with `<paper-input-container>`. It
shows the number of characters entered in the input and the max length if it is specified.

    <paper-input-container>
      <input is="iron-input" maxlength="20">
      <paper-input-char-counter></paper-input-char-counter>
    </paper-input-container>

### Styling

The following mixin is available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-input-char-counter` | Mixin applied to the element | `{}`


##&lt;paper-input-container&gt;


`<paper-input-container>` is a container for a `<label>`, an `<input is="iron-input">` or
`<iron-autogrow-textarea>` and optional add-on elements such as an error message or character
counter, used to implement Material Design text fields.

For example:

    <paper-input-container>
      <label>Your name</label>
      <input is="iron-input">
    </paper-input-container>

### Listening for input changes

By default, it listens for changes on the `bind-value` attribute on its children nodes and perform
tasks such as auto-validating and label styling when the `bind-value` changes. You can configure
the attribute it listens to with the `attr-for-value` attribute.

### Using a custom input element

You can use a custom input element in a `<paper-input-container>`, for example to implement a
compound input field like a social security number input. The custom input element should have the
`paper-input-input` class, have a `notify:true` value property and optionally implements
`Polymer.IronValidatableBehavior` if it is validatable.

    <paper-input-container attr-for-value="ssn-value">
      <label>Social security number</label>
      <ssn-input class="paper-input-input"></ssn-input>
    </paper-input-container>

### Validation

If the `auto-validate` attribute is set, the input container will validate the input and update
the container styling when the input value changes.

### Add-ons

Add-ons are child elements of a `<paper-input-container>` with the `add-on` attribute and
implements the `Polymer.PaperInputAddonBehavior` behavior. They are notified when the input value
or validity changes, and may implement functionality such as error messages or character counters.
They appear at the bottom of the input.

### Prefixes and suffixes
These are child elements of a `<paper-input-container>` with the `prefix`
or `suffix` attribute, and are displayed inline with the input, before or after.

    <paper-input-container>
      <div prefix>$</div>
      <label>Total</label>
      <input is="iron-input">
      <paper-icon-button suffix icon="clear"></paper-icon-button>
    </paper-input-container>

### Styling

The following custom properties and mixins are available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-input-container-color` | Label and underline color when the input is not focused | `--secondary-text-color`
`--paper-input-container-focus-color` | Label and underline color when the input is focused | `--default-primary-color`
`--paper-input-container-invalid-color` | Label and underline color when the input is is invalid | `--google-red-500`
`--paper-input-container-input-color` | Input foreground color | `--primary-text-color`
`--paper-input-container` | Mixin applied to the container | `{}`
`--paper-input-container-disabled` | Mixin applied to the container when it's disabled | `{}`
`--paper-input-container-label` | Mixin applied to the label | `{}`
`--paper-input-container-label-focus` | Mixin applied to the label when the input is focused | `{}`
`--paper-input-container-input` | Mixin applied to the input | `{}`
`--paper-input-container-underline` | Mixin applied to the underline | `{}`
`--paper-input-container-underline-focus` | Mixin applied to the underline when the input is focued | `{}`
`--paper-input-container-underline-disabled` | Mixin applied to the underline when the input is disabled | `{}`
`--paper-input-prefix` | Mixin applied to the input prefix | `{}`
`--paper-input-suffix` | Mixin applied to the input suffix | `{}`

This element is `display:block` by default, but you can set the `inline` attribute to make it
`display:inline-block`.


##&lt;paper-input-error&gt;


`<paper-input-error>` is an error message for use with `<paper-input-container>`. The error is
displayed when the `<paper-input-container>` is `invalid`.

    <paper-input-container>
      <input is="iron-input" pattern="[0-9]*">
      <paper-input-error>Only numbers are allowed!</paper-input-error>
    </paper-input-container>

### Styling

The following custom properties and mixins are available for styling:

Custom property | Description | Default
----------------|-------------|----------
`--paper-input-container-invalid-color` | The foreground color of the error | `--google-red-500`
`--paper-input-error`                   | Mixin applied to the error        | `{}`


##&lt;paper-textarea&gt;


`<paper-textarea>` is a multi-line text field with Material Design styling.

    <paper-textarea label="Textarea label"></paper-textarea>

See `Polymer.PaperInputBehavior` for more API docs.

### Validation

Currently only `required` and `maxlength` validation is supported.

### Styling

See `Polymer.PaperInputContainer` for a list of custom properties used to
style this element.


##Polymer.PaperInputBehavior


Use `Polymer.PaperInputBehavior` to implement inputs with `<paper-input-container>`. This
behavior is implemented by `<paper-input>`. It exposes a number of properties from
`<paper-input-container>` and `<input is="iron-input">` and they should be bound in your
template.

The input element can be accessed by the `inputElement` property if you need to access
properties or methods that are not exposed.


##Polymer.PaperInputAddonBehavior


Use `Polymer.PaperInputAddonBehavior` to implement an add-on for `<paper-input-container>`. A
add-on appears below the input, and may display information based on the input value and
validity such as a character counter or an error message.

