
<!---

This README is automatically generated from the comments in these files:
iron-multi-selectable.html  iron-selectable.html  iron-selector.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/iron-selector.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-selector)

_[Demo and API docs](https://elements.polymer-project.org/elements/iron-selector)_


##&lt;iron-selector&gt;

  `iron-selector` is an element which can be used to manage a list of elements
  that can be selected.  Tapping on the item will make the item selected.  The `selected` indicates
  which item is being selected.  The default is to use the index of the item.

  Example:

```html
  <iron-selector selected="0">
    <div>Item 1</div>
    <div>Item 2</div>
    <div>Item 3</div>
  </iron-selector>
```

  If you want to use the attribute value of an element for `selected` instead of the index,
  set `attrForSelected` to the name of the attribute.  For example, if you want to select item by
  `name`, set `attrForSelected` to `name`.

  Example:

```html
  <iron-selector attr-for-selected="name" selected="foo">
    <div name="foo">Foo</div>
    <div name="bar">Bar</div>
    <div name="zot">Zot</div>
  </iron-selector>
```

  `iron-selector` is not styled. Use the `iron-selected` CSS class to style the selected element.

  Example:

```html
  <style>
    .iron-selected {
      background: #eee;
    }
  </style>

  ...

  <iron-selector selected="0">
    <div>Item 1</div>
    <div>Item 2</div>
    <div>Item 3</div>
  </iron-selector>
```



<!-- No docs for Polymer.IronMultiSelectableBehavior found. -->

<!-- No docs for Polymer.IronSelectableBehavior found. -->
