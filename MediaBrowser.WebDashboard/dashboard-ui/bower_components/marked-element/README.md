
<!---

This README is automatically generated from the comments in these files:
marked-element.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build Status](https://travis-ci.org/PolymerElements/marked-element.svg?branch=master)](https://travis-ci.org/PolymerElements/marked-element)


##&lt;marked-element&gt;

Element wrapper for the [marked](https://github.com/chjj/marked) library.

`<marked-element>` accepts Markdown source, and renders it to a child
element with the class `markdown-html`. This child element can be styled
as you would a normal DOM element. If you do not provide a child element
with the `markdown-html` class, the Markdown source will still be rendered,
but to a shadow DOM child that cannot be styled.

The Markdown source can be specified either via the `markdown` attribute:

```html
<marked-element markdown="`Markdown` is _awesome_!">
  <div class="markdown-html"></div>
</marked-element>
```

Or, you can provide it via a `<script type="text/markdown">` element child:

```html
<marked-element>
  <div class="markdown-html"></div>
  <script type="text/markdown">
    Check out my markdown!

    We can even embed elements without fear of the HTML parser mucking up their
    textual representation:

    ```html
    <awesome-sauce>
      <div>Oops, I'm about to forget to close this div.
    </awesome-sauce>
    ```
  </script>
</marked-element>
```

Note that the `<script type="text/markdown">` approach is *static*. Changes to
the script content will *not* update the rendered markdown!

### Styling

If you are using a child with the `markdown-html` class, you can style it
as you would a regular DOM element:

```css
.markdown-html p {
  color: red;
}

.markdown-html td:first-child {
  padding-left: 24px;
}
```


