iron-meta
=========

`iron-meta` is a generic element you can use for sharing information across the DOM tree.
It uses [monostate pattern](http://c2.com/cgi/wiki?MonostatePattern) such that any
instance of iron-meta has access to the shared
information. You can use `iron-meta` to share whatever you want (or create an extension
[like x-meta] for enhancements).

The `iron-meta` instances containing your actual data can be loaded in an import,
or constructed in any way you see fit. The only requirement is that you create them
before you try to access them.

Examples:

If I create an instance like this:

```html
<iron-meta key="info" value="foo/bar"></iron-meta>
```

Note that value="foo/bar" is the metadata I've defined. I could define more
attributes or use child nodes to define additional metadata.

Now I can access that element (and it's metadata) from any iron-meta instance
via the byKey method, e.g.

```javascript
meta.byKey('info').getAttribute('value');
```

Pure imperative form would be like:

```javascript
document.createElement('iron-meta').byKey('info').getAttribute('value');
```

Or, in a Polymer element, you can include a meta in your template:

```html
<iron-meta id="meta"></iron-meta>
```

```javascript
this.$.meta.byKey('info').getAttribute('value');
```
