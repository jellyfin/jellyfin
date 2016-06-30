
<!---

This README is automatically generated from the comments in these files:
prism-highlighter.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->


##&lt;prism-highlighter&gt;

Syntax highlighting via [Prism](http://prismjs.com/).

Place a `<prism-highlighter>` in your document, preferably as a direct child of
`<body>`. It will listen for `syntax-highlight` events on its parent element,
and annotate the code being provided via that event.

The `syntax-highlight` event's detail is expected to have a `code` property
containing the source to highlight. The event detail can optionally contain a
`lang` property, containing a string like `"html"`, `"js"`, etc.

This flow is supported by [`<marked-element>`](https://github.com/PolymerElements/marked-element).


