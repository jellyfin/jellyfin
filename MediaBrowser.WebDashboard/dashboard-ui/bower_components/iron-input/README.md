# iron-input

An input with data binding.

By default you can only get notified of changes to an `input`'s `value` due to user input:

```html
<input value="{{myValue::input}}">
```

`iron-input` adds the `bind-value` property that mirrors the `value` property, and can be used
for two-way data binding. `bind-value` will notify if it is changed either by user input or by script.

```html
<input is="iron-input" bind-value="{{myValue}}">
```
