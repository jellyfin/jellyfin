paper-tabs
============

`paper-tabs` makes it easy to explore and switch between different views or functional aspects of
an app, or to browse categorized data sets.

Use `selected` property to get or set the selected tab.

Example:

```html
<paper-tabs selected="0">
  <paper-tab>TAB 1</paper-tab>
  <paper-tab>TAB 2</paper-tab>
  <paper-tab>TAB 3</paper-tab>
</paper-tabs>
```

See <a href="#paper-tab">paper-tab</a> for more information about
`paper-tab`.

A common usage for `paper-tabs` is to use it along with `iron-pages` to switch
between different views.

```html
<paper-tabs selected="{{selected}}">
  <paper-tab>Tab 1</paper-tab>
  <paper-tab>Tab 2</paper-tab>
  <paper-tab>Tab 3</paper-tab>
</paper-tabs>

<iron-pages selected="{{selected}}">
  <div>Page 1</div>
  <div>Page 2</div>
  <div>Page 3</div>
</iron-pages>
```

To use links in tabs, add `link` attribute to `paper-tab` and put an `<a>`
element in `paper-tab`.

Example:

```html
<paper-tabs selected="0">
  <paper-tab link>
    <a href="#link1" class="horizontal center-center layout">TAB ONE</a>
  </paper-tab>
  <paper-tab link>
    <a href="#link2" class="horizontal center-center layout">TAB TWO</a>
  </paper-tab>
  <paper-tab link>
    <a href="#link3" class="horizontal center-center layout">TAB THREE</a>
  </paper-tab>
</paper-tabs>
```
