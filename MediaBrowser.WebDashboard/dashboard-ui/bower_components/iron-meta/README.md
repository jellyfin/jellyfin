
<!---

This README is automatically generated from the comments in these files:
iron-meta.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build status](https://travis-ci.org/PolymerElements/iron-meta.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-meta)

_[Demo and API docs](https://elements.polymer-project.org/elements/iron-meta)_


##&lt;iron-meta&gt;

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
meta.byKey('info');
```

Pure imperative form would be like:

```javascript
document.createElement('iron-meta').byKey('info');
```

Or, in a Polymer element, you can include a meta in your template:

```html
<iron-meta id="meta"></iron-meta>
...
this.$.meta.byKey('info');
```



##&lt;iron-meta-query&gt;

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
meta.byKey('info');
```

Pure imperative form would be like:

```javascript
document.createElement('iron-meta').byKey('info');
```

Or, in a Polymer element, you can include a meta in your template:

```html
<iron-meta id="meta"></iron-meta>
...
this.$.meta.byKey('info');
```


