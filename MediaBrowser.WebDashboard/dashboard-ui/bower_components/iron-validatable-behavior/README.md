
<!---

This README is automatically generated from the comments in these files:
iron-validatable-behavior.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/iron-validatable-behavior.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-validatable-behavior)

_[Demo and API docs](https://elements.polymer-project.org/elements/iron-validatable-behavior)_


##Polymer.IronValidatableBehavior

`Use Polymer.IronValidatableBehavior` to implement an element that validates user input.
Use the related `Polymer.IronValidatorBehavior` to add custom validation logic to an iron-input.

By default, an `<iron-form>` element validates its fields when the user presses the submit button.
To validate a form imperatively, call the form's `validate()` method, which in turn will
call `validate()` on all its children. By using `Polymer.IronValidatableBehavior`, your
custom element will get a public `validate()`, which
will return the validity of the element, and a corresponding `invalid` attribute,
which can be used for styling.

To implement the custom validation logic of your element, you must override
the protected `_getValidity()` method of this behaviour, rather than `validate()`.
See [this](https://github.com/PolymerElements/iron-form/blob/master/demo/simple-element.html)
for an example.

### Accessibility

Changing the `invalid` property, either manually or by calling `validate()` will update the
`aria-invalid` attribute.


