'use strict';

Tinytest.add('Sortable.is', function (test) {
  var items = document.createElement('ul');
  items.innerHTML = '<li data-id="one">item 1</li><li data-id="two">item 2</li><li data-id="three">item 3</li>';
  var sortable = new Sortable(items);
  test.instanceOf(sortable, Sortable, 'Instantiation OK');
  test.length(sortable.toArray(), 3, 'Three elements');
});
