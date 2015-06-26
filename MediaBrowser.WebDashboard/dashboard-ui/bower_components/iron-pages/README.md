iron-pages
==========

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
