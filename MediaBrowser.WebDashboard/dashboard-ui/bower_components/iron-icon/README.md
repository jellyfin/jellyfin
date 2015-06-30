iron-icon
=========

The `iron-icon` element displays an icon. By default an icon renders as a 24px square.

Example using src:

```html
<iron-icon src="star.png"></iron-icon>
```

Example setting size to 32px x 32px:

```html
<iron-icon class="big" src="big_star.png"></iron-icon>

<style>
  .big {
    height: 32px;
    width: 32px;
  }
</style>
```

The iron elements include several sets of icons.
To use the default set of icons, import  `iron-icons.html` and use the `icon` attribute to specify an icon:

```html
<!-- import default iconset and iron-icon -->
<link rel="import" href="/components/iron-icons/iron-icons.html">

<iron-icon icon="menu"></iron-icon>
```

To use a different built-in set of icons, import  `iron-icons/<iconset>-icons.html`, and
specify the icon as `<iconset>:<icon>`. For example:

```html
<!-- import communication iconset and iron-icon -->
<link rel="import" href="/components/iron-icons/communication-icons.html">

<iron-icon icon="communication:email"></iron-icon>
```

You can also create custom icon sets of bitmap or SVG icons.

Example of using an icon named `cherry` from a custom iconset with the ID `fruit`:

```html
<iron-icon icon="fruit:cherry"></iron-icon>
```

See [iron-iconset](#iron-iconset) and [iron-iconset-svg](#iron-iconset-svg) for more information about
how to create a custom iconset.

See [iron-icons](http://www.polymer-project.org/components/iron-icons/demo.html) for the default set of icons.
