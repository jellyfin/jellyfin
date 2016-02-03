
<!---

This README is automatically generated from the comments in these files:
iron-pages.html

Edit those files, and our readme bot will duplicate them over here!
Edit this file, and the bot will squash your changes :)

The bot does some handling of markdown. Please file a bug if it does the wrong
thing! https://github.com/PolymerLabs/tedium/issues

-->

[![Build Status](https://travis-ci.org/PolymerElements/iron-pages.svg?branch=master)](https://travis-ci.org/PolymerElements/iron-pages)

_[Demo and API Docs](https://elements.polymer-project.org/elements/iron-pages)_


##&lt;iron-pages&gt;

`iron-pages` is used to select one of its children to show. One use is to cycle through a list of
children "pages".

Example:

```html
<iron-pages selected="0">
  <div>One</div>
  <div>Two</div>
  <div>Three</div>
</iron-pages>

<script>
  document.addEventListener('click', function(e) {
    var pages = document.querySelector('iron-pages');
    pages.selectNext();
  });
</script>
```


