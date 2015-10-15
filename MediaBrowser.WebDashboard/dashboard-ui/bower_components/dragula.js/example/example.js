'use strict';

var crossvent = require('crossvent');
var sortable = $('sortable');

dragula([$('left-defaults'), $('right-defaults')]);
dragula([$('left-copy'), $('right-copy')], { copy: true });
dragula([$('left-events'), $('right-events')])
  .on('drag', function (el) {
    el.className = el.className.replace('ex-moved', '');
  })
  .on('drop', function (el) {
    el.className += ' ex-moved';
  })
  .on('over', function (el, container) {
    container.className += ' ex-over';
  })
  .on('out', function (el, container) {
    container.className = container.className.replace('ex-over', '');
  });
dragula([$('left-rollbacks'), $('right-rollbacks')], { revertOnSpill: true });
dragula([$('left-lovehandles'), $('right-lovehandles')], {
  moves: function (el, container, handle) {
    return handle.className === 'handle';
  }
});

dragula([$('left-rm-spill'), $('right-rm-spill')], { removeOnSpill: true });
dragula([$('left-copy-1tomany'), $('right-copy-1tomany')], {
  copy: function (el, source) {
    return source === $('left-copy-1tomany');
  },
  accepts: function (el, target) {
    return target !== $('left-copy-1tomany');
  }
});

dragula([sortable]);

crossvent.add(sortable, 'click', clickHandler);

function clickHandler (e) {
  var target = e.target;
  if (target === sortable) {
    return;
  }
  target.innerHTML += ' [click!]';

  setTimeout(function () {
    target.innerHTML = target.innerHTML.replace(/ \[click!\]/g, '');
  }, 500);
}

function $ (id) {
  return document.getElementById(id);
}
