paper-spinner
=============

Element providing material design circular spinner.

##### Example

```html
<paper-spinner active></paper-spinner>
```

The default spinner cycles between four layers of colors; by default they are
blue, red, yellow and green. It can be customized so that it uses one color only
by setting all the layer colors to the same value.

##### Example

```html
<style is="custom-style">
  paper-spinner .rainbow {
    --paper-spinner-layer-1-color: yellow;
    --paper-spinner-layer-2-color: red;
    --paper-spinner-layer-3-color: blue;
    --paper-spinner-layer-4-color: green;
  }

  paper-spinner .red {
    --paper-spinner-layer-1-color: red;
    --paper-spinner-layer-2-color: red;
    --paper-spinner-layer-3-color: red;
    --paper-spinner-layer-4-color: red;
  }
</style>
```

Alt attribute should be set to provide adequate context for accessibility. If not provided,
it defaults to 'loading'.
Empty alt can be provided to mark the element as decorative if alternative content is provided
in another form (e.g. a text block following the spinner).

##### Example

```html
<paper-spinner alt="Loading contacts list" active></paper-spinner>
```
