# iron-autogrow-textarea

`iron-autogrow-textarea` is an element containing a textarea that grows in height as more
lines of input are entered. Unless an explicit height or the `maxRows` property is set, it will
never scroll.

Example:

    <iron-autogrow-textarea></iron-autogrow-textarea>

Because the `textarea`'s `value` property is not observable, you should use
this element's `bind-value` instead for imperative updates.