# paper-drawer-panel

`paper-drawer-panel` contains a drawer panel and a main panel.  The drawer
and the main panel are side-by-side with drawer on the left.  When the browser
window size is smaller than the `responsiveWidth`, `paper-drawer-panel`
changes to narrow layout.  In narrow layout, the drawer will be stacked on top
of the main panel.  The drawer will slide in/out to hide/reveal the main
panel.

Use the attribute `drawer` to indicate that the element is the drawer panel and
`main` to indicate that the element is the main panel.

Example:

```html
<paper-drawer-panel>
  <div drawer> Drawer panel... </div>
  <div main> Main panel... </div>
</paper-drawer-panel>
```

The drawer and the main panels are not scrollable.  You can set CSS overflow
property on the elements to make them scrollable or use `paper-header-panel`.

Example:

```html
<paper-drawer-panel>
  <paper-header-panel drawer>
    <paper-toolbar></paper-toolbar>
    <div> Drawer content... </div>
  </paper-header-panel>
  <paper-header-panel main>
    <paper-toolbar></paper-toolbar>
    <div> Main content... </div>
  </paper-header-panel>
</paper-drawer-panel>
```

An element that should toggle the drawer will automatically do so if it's
given the `paper-drawer-toggle` attribute.  Also this element will automatically
be hidden in wide layout.

Example:

```html
<paper-drawer-panel>
  <paper-header-panel drawer>
    <paper-toolbar>
      <div>Application</div>
    </paper-toolbar>
    <div> Drawer content... </div>
  </paper-header-panel>
  <paper-header-panel main>
    <paper-toolbar>
      <paper-icon-button icon="menu" paper-drawer-toggle></paper-icon-button>
      <div>Title</div>
    </paper-toolbar>
    <div> Main content... </div>
  </paper-header-panel>
</paper-drawer-panel>
```

To position the drawer to the right, add `right-drawer` attribute.

```html
<paper-drawer-panel right-drawer>
  <div drawer> Drawer panel... </div>
  <div main> Main panel... </div>
</paper-drawer-panel>
```
